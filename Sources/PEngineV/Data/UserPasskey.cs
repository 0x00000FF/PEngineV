using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class UserPasskey
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string CredentialId { get; set; } = string.Empty;

    [Required]
    public string PublicKey { get; set; } = string.Empty;

    public uint SignCount { get; set; }

    public byte[]? UserHandle { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    public User User { get; set; } = null!;
}
