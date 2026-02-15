namespace PEngineV.Models;

public record CommentViewModel(
    int Id,
    string Name,
    string? Email,
    string Content,
    DateTime CreatedAt,
    IEnumerable<CommentViewModel> Replies);
