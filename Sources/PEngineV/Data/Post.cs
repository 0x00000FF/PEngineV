using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public enum PostVisibility
{
    Public = 0,
    Internal = 1,
    Groups = 2,
    Private = 3
}

public class Post
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    public PostVisibility Visibility { get; set; } = PostVisibility.Public;

    public bool IsProtected { get; set; }

    // AES-256-GCM encrypted content (Base64), null if not protected
    public string? EncryptedContent { get; set; }

    // Base64-encoded salt for PBKDF2 key derivation
    public string? PasswordSalt { get; set; }

    // Base64-encoded IV/nonce for AES-256-GCM
    public string? EncryptionIV { get; set; }

    // Base64-encoded GCM auth tag
    public string? EncryptionTag { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public User Author { get; set; } = null!;
    public Category? Category { get; set; }
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostGroup> PostGroups { get; set; } = new List<PostGroup>();
}
