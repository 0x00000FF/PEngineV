namespace PEngineV.Models;

public record HomePostItem(
    int Id,
    string Title,
    string? Category,
    string Author,
    DateTime PublishedAt,
    bool IsProtected,
    string? ThumbnailUrl = null,
    IEnumerable<string>? Tags = null,
    string? AuthorUsername = null);

public record HomeViewModel(
    IEnumerable<HomePostItem> Posts);
