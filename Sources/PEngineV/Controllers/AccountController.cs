using System.Security.Claims;
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

    public AccountController(IUserService userService, IAuditLogService auditLogService, ITotpService totpService)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _totpService = totpService;
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
