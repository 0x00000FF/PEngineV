namespace PEngineV.Models;

public record CommentWriteViewModel(
    int PostId,
    int? ParentCommentId,
    string? Name,
    string? Email,
    string? Password,
    string Content,
    bool IsPrivate = false);
