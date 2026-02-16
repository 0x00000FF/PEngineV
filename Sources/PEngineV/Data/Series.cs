using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Series
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int AuthorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Author { get; set; } = null!;
    public ICollection<PostSeries> PostSeries { get; set; } = new List<PostSeries>();
}
