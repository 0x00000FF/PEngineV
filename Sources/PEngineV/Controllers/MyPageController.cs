using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

[Authorize]
public class MyPageController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly ITotpService _totpService;
    private readonly IFido2 _fido2;
    private readonly IFileUploadService _fileUploadService;

    public MyPageController(IUserService userService, IAuditLogService auditLogService, ITotpService totpService, IFido2 fido2, IFileUploadService fileUploadService)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _totpService = totpService;
        _fido2 = fido2;
        _fileUploadService = fileUploadService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string? GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string GetUserAgent() =>
        Request.Headers.UserAgent.ToString();

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        var passkeys = await _userService.GetPasskeysAsync(userId);
        var auditLogs = await _auditLogService.GetLogsForUserAsync(userId, 1, 200);

        var model = new MyPageViewModel(
            user.Username,
            user.Nickname,
            user.Bio,
            user.ContactEmail,
            user.ProfileImageUrl,
            user.TwoFactorEnabled,
            passkeys.Select(p => new MyPagePasskeyItem(p.Id, p.Name, p.CreatedAt, p.LastUsedAt)),
            auditLogs.Select(a => new MyPageAuditLogItem(a.Timestamp, a.ActionType, a.IpAddress, a.UserAgent, a.Details)));

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string nickname, string? bio, string? contactEmail)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        await _userService.UpdateProfileAsync(userId, nickname, bio, contactEmail, user.ProfileImageUrl);
        await _auditLogService.LogAsync(userId, "Profile_Updated", GetIp(), GetUserAgent(), "Profile information updated");

        TempData["ToastMessage"] = "Toast_ProfileUpdated";
        TempData["ToastType"] = "success";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UploadProfileImage(IFormFile? profileImage)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        if (profileImage is null || profileImage.Length == 0)
        {
            return RedirectToAction("Index");
        }

        try
        {
            var oldProfileImage = await _fileUploadService.GetProfileImageByUserIdAsync(userId);
            if (oldProfileImage is not null)
            {
                await _fileUploadService.DeleteFileAsync(oldProfileImage.Id);
            }

            var uploadedFile = await _fileUploadService.UploadProfileImageAsync(userId, profileImage);
            var imageUrl = $"/file/view/{uploadedFile.FileGuid}";

            await _userService.UpdateProfileAsync(userId, user.Nickname, user.Bio, user.ContactEmail, imageUrl);
            await _auditLogService.LogAsync(userId, "Profile_Image_Updated", GetIp(), GetUserAgent(), "Profile image updated");

            TempData["ToastMessage"] = "Toast_ProfileImageUpdated";
            TempData["ToastType"] = "success";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ToastMessage"] = ex.Message.Contains("2MB") ? "Toast_Error_ProfileImageTooLarge" : "Toast_Error_ProfileImageInvalidType";
            TempData["ToastType"] = "error";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
    {
        var userId = GetUserId();

        if (newPassword != confirmNewPassword)
        {
            TempData["ToastMessage"] = "Toast_Error_PasswordMismatch";
            TempData["ToastType"] = "error";
            return RedirectToAction("Index", null, "security");
        }

        var success = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
        if (!success)
        {
            TempData["ToastMessage"] = "Toast_Error_InvalidPassword";
            TempData["ToastType"] = "error";
            await _auditLogService.LogAsync(userId, "Password_Change_Failed", GetIp(), GetUserAgent(), "Invalid current password");
            return RedirectToAction("Index", null, "security");
        }

        await _auditLogService.LogAsync(userId, "Password_Changed", GetIp(), GetUserAgent(), "Password changed successfully");
        TempData["ToastMessage"] = "Toast_PasswordChanged";
        TempData["ToastType"] = "success";
        return RedirectToAction("Index", null, "security");
    }

    [HttpPost]
    public async Task<IActionResult> EnableTwoFactor()
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        var secret = await _userService.EnableTwoFactorAsync(userId);
        var qrCodeUri = _totpService.GenerateQrCodeUri(secret, user.Username);

        await _auditLogService.LogAsync(userId, "2FA_Setup_Started", GetIp(), GetUserAgent(), "2FA setup initiated");

        var model = new TwoFactorSetupViewModel(qrCodeUri, secret, "");
        return View("TwoFactorSetup", model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmTwoFactor(string secret, string verificationCode)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        var success = await _userService.ConfirmTwoFactorAsync(userId, verificationCode);
        if (!success)
        {
            var qrCodeUri = _totpService.GenerateQrCodeUri(secret, user.Username);
            await _auditLogService.LogAsync(userId, "2FA_Setup_Failed", GetIp(), GetUserAgent(), "Invalid verification code");
            return View("TwoFactorSetup", new TwoFactorSetupViewModel(qrCodeUri, secret, ""));
        }

        await _auditLogService.LogAsync(userId, "2FA_Enabled", GetIp(), GetUserAgent(), "2FA enabled successfully");
        TempData["ToastMessage"] = "Toast_2FAEnabled";
        TempData["ToastType"] = "success";
        return RedirectToAction("Index", null, "security");
    }

    [HttpPost]
    public async Task<IActionResult> DisableTwoFactor(string confirmationCode)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        if (user.TwoFactorEnabled && user.TwoFactorSecret is not null)
        {
            if (!_totpService.ValidateCode(user.TwoFactorSecret, confirmationCode))
            {
                TempData["ToastMessage"] = "Toast_Error_Invalid2FACode";
                TempData["ToastType"] = "error";
                await _auditLogService.LogAsync(userId, "2FA_Disable_Failed", GetIp(), GetUserAgent(), "Invalid 2FA code on disable attempt");
                return RedirectToAction("Index", null, "security");
            }
        }

        await _userService.DisableTwoFactorAsync(userId);
        await _auditLogService.LogAsync(userId, "2FA_Disabled", GetIp(), GetUserAgent(), "2FA disabled");
        TempData["ToastMessage"] = "Toast_2FADisabled";
        TempData["ToastType"] = "success";
        return RedirectToAction("Index", null, "security");
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BeginPasskeyRegistration([FromBody] JsonElement body)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        var name = body.GetProperty("name").GetString() ?? "My Passkey";
        var existingPasskeys = await _userService.GetPasskeysAsync(userId);
        var excludeCredentials = existingPasskeys
            .Select(p => new PublicKeyCredentialDescriptor(Convert.FromBase64String(p.CredentialId)))
            .ToList();

        var fidoUser = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(userId.ToString()),
            Name = user.Username,
            DisplayName = user.Nickname
        };

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = AuthenticatorSelection.Default,
            AttestationPreference = AttestationConveyancePreference.None
        });

        HttpContext.Session.SetString("fido2.attestationOptions", JsonSerializer.Serialize(options));
        HttpContext.Session.SetString("fido2.passkeyName", name);

        return new JsonResult(options);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CompletePasskeyRegistration([FromBody] AuthenticatorAttestationRawResponse attestationResponse)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return Unauthorized();

        var optionsJson = HttpContext.Session.GetString("fido2.attestationOptions");
        if (optionsJson is null) return BadRequest("Session expired");

        var options = JsonSerializer.Deserialize<CredentialCreateOptions>(optionsJson);
        if (options is null) return BadRequest("Invalid options");
        var passkeyName = HttpContext.Session.GetString("fido2.passkeyName") ?? "Passkey";

        var credential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, ct) =>
            {
                var existing = await _userService.GetPasskeyByCredentialIdAsync(args.CredentialId);
                return existing is null;
            }
        });

        await _userService.AddPasskeyAsync(
            userId, passkeyName,
            credential.Id,
            credential.PublicKey,
            credential.SignCount,
            credential.User.Id);

        await _auditLogService.LogAsync(userId, "Passkey_Added", GetIp(), GetUserAgent(), $"Passkey '{passkeyName}' added");

        HttpContext.Session.Remove("fido2.attestationOptions");
        HttpContext.Session.Remove("fido2.passkeyName");

        return Ok(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AddPasskey(string name)
    {
        // Fallback for non-JS form submission
        TempData["ToastMessage"] = "Toast_PasskeyAdded";
        TempData["ToastType"] = "error";
        return RedirectToAction("Index", null, "security");
    }

    [HttpPost]
    public async Task<IActionResult> RemovePasskey(int passkeyId)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        var success = await _userService.RemovePasskeyAsync(userId, passkeyId);
        if (success)
        {
            await _auditLogService.LogAsync(userId, "Passkey_Removed", GetIp(), GetUserAgent(), $"Passkey (ID: {passkeyId}) removed");
            TempData["ToastMessage"] = "Toast_PasskeyRemoved";
            TempData["ToastType"] = "success";
        }
        else
        {
            await _auditLogService.LogAsync(userId, "Passkey_Remove_Failed", GetIp(), GetUserAgent(), $"Passkey (ID: {passkeyId}) not found");
        }

        return RedirectToAction("Index", null, "security");
    }
}
