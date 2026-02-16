using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Citation
{
    [Key]
    public int Id { get; set; }

    public int PostId { get; set; }

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Author { get; set; }

    [MaxLength(1000)]
    public string? Url { get; set; }

    public DateTime? PublicationDate { get; set; }

    [MaxLength(200)]
    public string? Publisher { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int OrderIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Post Post { get; set; } = null!;
}
