namespace PEngineV.Models;

public record MyPageViewModel(
    string Username,
    string Nickname,
    string? Bio,
    string? ContactEmail,
    string? ProfileImageUrl,
    bool TwoFactorEnabled,
    IEnumerable<MyPagePasskeyItem> Passkeys,
    IEnumerable<MyPageAuditLogItem> AuditLogs);

public record MyPagePasskeyItem(
    int Id,
    string Name,
    DateTime CreatedAt,
    DateTime? LastUsedAt);

public record MyPageAuditLogItem(
    DateTime Timestamp,
    string ActionType,
    string? IpAddress,
    string? UserAgent,
    string? Details);

public record MyPageProfileEditViewModel(
    string Nickname,
    string? Bio,
    string? ContactEmail);

public record MyPageChangePasswordViewModel(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public record MyPageAddPasskeyViewModel(
    string Name,
    string ConfirmationCode);

public record MyPageRemovePasskeyViewModel(
    int PasskeyId,
    string ConfirmationCode);
