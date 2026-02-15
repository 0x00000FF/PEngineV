namespace PEngineV.Models;

public record ProfilePostItem(
    int Id,
    string Title,
    DateTime PublishedAt);

public record ProfileCommentItem(
    int Id,
    int PostId,
    string PostTitle,
    string ContentPreview,
    DateTime CreatedAt);

public record ProfileGuestbookItem(
    int Id,
    string ContentPreview,
    DateTime CreatedAt);

public record ProfileViewModel(
    string Username,
    string? Nickname,
    string? ProfileImageUrl,
    string? Bio,
    string? ContactEmail,
    DateTime SignedUpAt,
    DateTime LastLoggedAt,
    IEnumerable<ProfilePostItem> Posts,
    IEnumerable<ProfileCommentItem> Comments,
    IEnumerable<ProfileGuestbookItem> GuestbookReplies);
