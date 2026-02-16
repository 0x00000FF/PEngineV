using System.Security.Claims;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly ITotpService _totpService;
    private readonly IFido2 _fido2;

    public AccountController(IUserService userService, IAuditLogService auditLogService, ITotpService totpService, IFido2 fido2)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _totpService = totpService;
        _fido2 = fido2;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel("", ""));
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? twoFactorCode)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        int userId;

        if (TempData.ContainsKey("Pending2FAUserId") && !string.IsNullOrWhiteSpace(twoFactorCode))
        {
            userId = (int)TempData["Pending2FAUserId"]!;
            var pendingUser = await _userService.GetByIdAsync(userId);
            if (pendingUser is null)
            {
                return View(new LoginViewModel("", "", ErrorMessage: "InvalidCredentials"));
            }

            if (pendingUser.TwoFactorSecret is null || !_totpService.ValidateCode(pendingUser.TwoFactorSecret, twoFactorCode))
            {
                TempData["Pending2FAUserId"] = userId;
                await _auditLogService.LogAsync(userId, "Login_2FA_Failed", ip, ua, "Invalid 2FA code");
                return View(new LoginViewModel(pendingUser.Username, "", RequiresTwoFactor: true, ErrorMessage: "Invalid2FACode"));
            }

            return await SignInUserAsync(pendingUser, ip, ua);
        }

        var user = await _userService.AuthenticateAsync(username, password);
        if (user is null)
        {
            return View(new LoginViewModel(username, "", ErrorMessage: "InvalidCredentials"));
        }

        if (user.TwoFactorEnabled)
        {
            TempData["Pending2FAUserId"] = user.Id;
            return View(new LoginViewModel(username, "", RequiresTwoFactor: true));
        }

        return await SignInUserAsync(user, ip, ua);
    }

    private async Task<IActionResult> SignInUserAsync(Data.User user, string? ip, string? ua)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("Nickname", user.Nickname)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        await _auditLogService.LogAsync(user.Id, "Login", ip, ua, "Login successful");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel("", "", "", ""));
    }

    [HttpPost]
    public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ViewData["Error"] = "PasswordMismatch";
            return View(new RegisterViewModel(username, email, "", ""));
        }

        var existing = await _userService.GetByUsernameAsync(username);
        if (existing is not null)
        {
            ViewData["Error"] = "UsernameTaken";
            return View(new RegisterViewModel(username, email, "", ""));
        }

        var user = await _userService.CreateUserAsync(username, email, password, username);

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        await _auditLogService.LogAsync(user.Id, "Register", ip, ua, "Account created");

        return RedirectToAction("Login");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult BeginPasskeyLogin()
    {
        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Preferred
        });

        HttpContext.Session.SetString("fido2.assertionOptions", JsonSerializer.Serialize(options));

        return new JsonResult(options);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CompletePasskeyLogin([FromBody] AuthenticatorAssertionRawResponse assertionResponse)
    {
        ArgumentNullException.ThrowIfNull(assertionResponse);
        var optionsJson = HttpContext.Session.GetString("fido2.assertionOptions");
        if (optionsJson is null) return BadRequest("Session expired");

        var options = JsonSerializer.Deserialize<AssertionOptions>(optionsJson);
        if (options is null) return BadRequest("Invalid options");

        var passkey = await _userService.GetPasskeyByCredentialIdAsync(assertionResponse.Id);
        if (passkey is null) return BadRequest("Unknown credential");

        var storedPublicKey = Convert.FromBase64String(passkey.PublicKey);

        var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            OriginalOptions = options,
            StoredPublicKey = storedPublicKey,
            StoredSignatureCounter = passkey.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
            {
                var pk = await _userService.GetPasskeyByCredentialIdAsync(args.CredentialId);
                return pk is not null;
            }
        });

        await _userService.UpdatePasskeySignCountAsync(passkey.Id, result.SignCount);

        var user = passkey.User;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        HttpContext.Session.Remove("fido2.assertionOptions");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("Nickname", user.Nickname)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        await _auditLogService.LogAsync(user.Id, "Login_Passkey", ip, ua, "Login via passkey successful");

        return Ok(new { success = true, redirect = Url.Action("Index", "Home") });
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var ua = Request.Headers.UserAgent.ToString();
                await _auditLogService.LogAsync(userId, "Logout", ip, ua, "User logged out");
            }
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
