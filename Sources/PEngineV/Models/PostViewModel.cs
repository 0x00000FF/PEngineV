namespace PEngineV.Models;

public record PostViewModel(
    int Id,
    string Title,
    string Content,
    string Author,
    DateTime PublishedAt,
    bool IsProtected,
    string? Category = null,
    string? ThumbnailUrl = null,
    IEnumerable<string>? Tags = null,
    string? Visibility = null,
    string? AuthorUsername = null);
