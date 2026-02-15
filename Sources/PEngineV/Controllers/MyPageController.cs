using System.Security.Claims;
using System.Security.Cryptography;
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

    public MyPageController(IUserService userService, IAuditLogService auditLogService, ITotpService totpService)
    {
        _userService = userService;
        _auditLogService = auditLogService;
        _totpService = totpService;
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
        var auditLogs = await _auditLogService.GetLogsForUserAsync(userId);

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

        return RedirectToAction("Index");
    }

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp"
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

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

        if (profileImage.Length > 2 * 1024 * 1024)
        {
            TempData["Error"] = "ProfileImageTooLarge";
            return RedirectToAction("Index");
        }

        var contentType = profileImage.ContentType;
        var extension = Path.GetExtension(profileImage.FileName);

        if (!AllowedImageTypes.Contains(contentType) || !AllowedImageExtensions.Contains(extension))
        {
            TempData["Error"] = "ProfileImageInvalidType";
            return RedirectToAction("Index");
        }

        var fileName = $"{userId}_{Guid.NewGuid():N}{extension}";
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await profileImage.CopyToAsync(stream);
        }

        if (!string.IsNullOrEmpty(user.ProfileImageUrl))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot",
                user.ProfileImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        var imageUrl = $"/uploads/profiles/{fileName}";
        await _userService.UpdateProfileAsync(userId, user.Nickname, user.Bio, user.ContactEmail, imageUrl);
        await _auditLogService.LogAsync(userId, "Profile_Image_Updated", GetIp(), GetUserAgent(), "Profile image updated");

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
    {
        var userId = GetUserId();

        if (newPassword != confirmNewPassword)
        {
            TempData["Error"] = "PasswordMismatch";
            return RedirectToAction("Index");
        }

        var success = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
        if (!success)
        {
            TempData["Error"] = "InvalidCurrentPassword";
            await _auditLogService.LogAsync(userId, "Password_Change_Failed", GetIp(), GetUserAgent(), "Invalid current password");
        }
        else
        {
            await _auditLogService.LogAsync(userId, "Password_Changed", GetIp(), GetUserAgent(), "Password changed successfully");
        }

        return RedirectToAction("Index");
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
        return RedirectToAction("Index");
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
                TempData["Error"] = "Invalid2FACode";
                await _auditLogService.LogAsync(userId, "2FA_Disable_Failed", GetIp(), GetUserAgent(), "Invalid 2FA code on disable attempt");
                return RedirectToAction("Index");
            }
        }

        await _userService.DisableTwoFactorAsync(userId);
        await _auditLogService.LogAsync(userId, "2FA_Disabled", GetIp(), GetUserAgent(), "2FA disabled");
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> AddPasskey(string name)
    {
        var userId = GetUserId();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null) return RedirectToAction("Login", "Account");

        var credentialId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var publicKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await _userService.AddPasskeyAsync(userId, name, credentialId, publicKey);
        await _auditLogService.LogAsync(userId, "Passkey_Added", GetIp(), GetUserAgent(), $"Passkey '{name}' added");

        return RedirectToAction("Index");
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
        }
        else
        {
            await _auditLogService.LogAsync(userId, "Passkey_Remove_Failed", GetIp(), GetUserAgent(), $"Passkey (ID: {passkeyId}) not found");
        }

        return RedirectToAction("Index");
    }
}
