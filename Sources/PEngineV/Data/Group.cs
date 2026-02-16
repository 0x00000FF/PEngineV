using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class Group
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<PostGroup> PostGroups { get; set; } = new List<PostGroup>();
}
