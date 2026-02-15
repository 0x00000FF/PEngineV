namespace PEngineV.Models;

public record PostViewModel(
    int Id,
    string Title,
    string Content,
    string Author,
    DateTime PublishedAt,
    bool IsProtected,
    string? Category = null);
