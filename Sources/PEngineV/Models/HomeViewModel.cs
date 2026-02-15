namespace PEngineV.Models;

public record HomePostItem(
    int Id,
    string Title,
    string? Category,
    string Author,
    DateTime PublishedAt,
    bool IsProtected);

public record HomeViewModel(
    IEnumerable<HomePostItem> Posts);
