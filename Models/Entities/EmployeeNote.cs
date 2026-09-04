using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.Models.Entities;

public enum NoteStatus
{
    Pending = 0,    // قيد الانتظار / بانتظار موافقة الأدمن
    Approved = 1,   // معتمدة من الأدمن
    Rejected = 2    // مرفوضة
}

public class EmployeeNote
{
    public int Id { get; set; }

    public int SoldierId { get; set; }
    public Soldier Soldier { get; set; } = null!;

    [Required]
    [MaxLength(1000)]
    public string NoteText { get; set; } = string.Empty;

    // معلومات محرر الملاحظة (المشرف / صف الضابط)
    [MaxLength(50)]
    public string AuthorUsername { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AuthorFullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string AuthorRole { get; set; } = "Supervisor"; // Admin, Supervisor

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public NoteStatus Status { get; set; } = NoteStatus.Pending;

    // بيانات المراجعة والاعتماد من قبل الأدمن
    [MaxLength(100)]
    public string? ApprovedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? AdminComment { get; set; }
}