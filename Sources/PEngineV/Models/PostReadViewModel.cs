namespace PEngineV.Models;

public record PostReadViewModel(
    PostViewModel Post,
    IEnumerable<CommentViewModel> Comments,
    bool IsLoggedIn,
    bool IsOwner,
    IEnumerable<AttachmentItem>? Attachments = null,
    IEnumerable<CitationViewModel>? Citations = null,
    SeriesViewModel? Series = null,
    int SeriesOrder = 0);

public record CitationViewModel(
    int Id,
    string Title,
    string? Author,
    string? Url,
    DateTime? PublicationDate,
    string? Publisher,
    string? Notes);

public record SeriesViewModel(
    int Id,
    string Name,
    string? Description,
    IEnumerable<SeriesPostItem>? Posts = null);

public record SeriesPostItem(
    int Id,
    string Title,
    int OrderIndex,
    bool IsCurrent);
