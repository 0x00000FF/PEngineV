using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public enum FileCategory
{
    ProfileImage = 0,
    PostAttachment = 1,
    PostThumbnail = 2
}

public class UploadedFile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid FileGuid { get; set; } = Guid.NewGuid();

    public FileCategory Category { get; set; }

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string StoredPath { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required, MaxLength(64)]
    public string Sha256Hash { get; set; } = string.Empty;

    public int? UploadedByUserId { get; set; }

    public int? RelatedPostId { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public User? UploadedBy { get; set; }
    public Post? RelatedPost { get; set; }
}
