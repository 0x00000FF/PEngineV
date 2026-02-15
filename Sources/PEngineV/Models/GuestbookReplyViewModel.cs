namespace PEngineV.Models;

public record GuestbookReplyViewModel(
    int Id,
    string AuthorName,
    string Content,
    DateTime CreatedAt);
