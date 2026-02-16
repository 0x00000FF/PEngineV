using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Attachment
{
    [Key]
    public int Id { get; set; }

    public int PostId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string StoredPath { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Required, MaxLength(64)]
    public string Sha256Hash { get; set; } = string.Empty;

    // Encrypted attachment data (Base64), null if post not protected
    public string? EncryptedData { get; set; }

    // Base64-encoded IV/nonce for AES-256-GCM (per-attachment)
    public string? EncryptionIV { get; set; }

    // Base64-encoded GCM auth tag (per-attachment)
    public string? EncryptionTag { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
}
