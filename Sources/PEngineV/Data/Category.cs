using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Slug { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
