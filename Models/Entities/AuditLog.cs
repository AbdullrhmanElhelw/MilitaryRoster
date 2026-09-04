using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.Models.Entities;

public class AuditLog
{
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EntityName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }
}
