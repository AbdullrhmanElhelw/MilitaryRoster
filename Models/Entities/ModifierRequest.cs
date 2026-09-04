using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.Models.Entities;

public class ModifierRequest
{
    public int Id { get; set; }

    public int SoldierId { get; set; }
    public Soldier Soldier { get; set; } = null!;

    public long? LeaveCycleId { get; set; }
    public LeaveCycle? LeaveCycle { get; set; }

    [Required]
    [MaxLength(20)]
    public string RequestType { get; set; } = "Bonus"; // Bonus (منحة) أو Deduction (خصم)

    [Range(1, 60)]
    public int Days { get; set; } = 1;

    [MaxLength(500)]
    public string? Reason { get; set; }

    // بيانات المشرف مقدم الطلب
    [MaxLength(50)]
    public string RequestedByUsername { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RequestedByFullName { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public NoteStatus Status { get; set; } = NoteStatus.Pending;

    // بيانات اعتماد الأدمن
    [MaxLength(100)]
    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? AdminComment { get; set; }
}