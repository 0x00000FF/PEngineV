using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Comment
{
    [Key]
    public int Id { get; set; }

    public int PostId { get; set; }

    public int? AuthorId { get; set; }

    public int? ParentCommentId { get; set; }

    [MaxLength(100)]
    public string? GuestName { get; set; }

    [MaxLength(255)]
    public string? GuestEmail { get; set; }

    // Password hash for anonymous comments (for private comment access)
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public User? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
