using PEngineV.Data;

namespace PEngineV.Models;

public record PostWriteViewModel(
    int? Id,
    string Title,
    string Content,
    string? Password,
    string? CategoryName,
    string? Tags,
    string Visibility,
    DateTime? PublishAt,
    IEnumerable<CategoryOption>? Categories,
    IEnumerable<AttachmentItem>? Attachments);

public record CategoryOption(int Id, string Name);

public record AttachmentItem(int Id, string FileName, long FileSize, string Sha256Hash);
