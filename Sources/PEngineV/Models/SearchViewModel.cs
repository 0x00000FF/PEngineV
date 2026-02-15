namespace PEngineV.Models;

public record SearchPostResult(
    int Id,
    string Title,
    string Author,
    string ContentPreview,
    DateTime PublishedAt);

public record SearchCommentResult(
    int Id,
    int PostId,
    string PostTitle,
    string Author,
    string ContentPreview,
    DateTime CreatedAt);

public record SearchViewModel(
    string Query,
    IEnumerable<SearchPostResult> Posts,
    IEnumerable<SearchCommentResult> Comments);
