using System.ComponentModel.DataAnnotations;

namespace PEngineV.Data;

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    public User User { get; set; } = null!;
}
