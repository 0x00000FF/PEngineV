namespace PEngineV.Models;

public record SearchPostResult(
    int Id,
    string Title,
    string Author,
    string ContentPreview,
    DateTime PublishedAt,
    string? AuthorUsername = null);

public record SearchCommentResult(
    int Id,
    int PostId,
    string PostTitle,
    string Author,
    string ContentPreview,
    DateTime CreatedAt,
    string? AuthorUsername = null);

public record SearchViewModel(
    string Query,
    IEnumerable<SearchPostResult> Posts,
    IEnumerable<SearchCommentResult> Comments);
