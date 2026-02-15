using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Tag
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
