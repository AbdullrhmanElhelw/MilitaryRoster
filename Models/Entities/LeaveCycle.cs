using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MilitaryRoster.Models.Entities;

public class LeaveCycle
{
    public long Id { get; set; }

    public int SoldierId { get; set; }
    public Soldier Soldier { get; set; } = null!;

    public int SoldierTypeId { get; set; }
    public SoldierType SoldierType { get; set; } = null!;

    public int CycleNumber { get; set; } = 1;

    public DateOnly LastReturnDate { get; set; } // تاريخ آخر عودة / استلام
    public DateOnly LeaveStartDate { get; set; } // موعد النزول المخطط
    public DateOnly ExpectedReturnDate { get; set; } // موعد العودة المخطط

    public DateOnly? ActualLeaveDate { get; set; } // موعد النزول الفعلي
    public DateOnly? ActualReturnDate { get; set; } // موعد العودة الفعلي
    public TimeSpan? ActualReturnTime { get; set; } // وقت العودة الفعلي

    public int DutyDaysSnapshot { get; set; } = 10;
    public int LeaveDaysSnapshot { get; set; } = 8;

    public int DeductionDays { get; set; } = 0;
    public int BonusDays { get; set; } = 0;

    public bool IsCompleted { get; set; } = false;

    [MaxLength(100)]
    public string? ClauseExecutedBy { get; set; } // من قام بعمل البند
    public DateTime? ClauseExecutedAt { get; set; } // وقت عمل البند

    [MaxLength(100)]
    public string? DispatchedBy { get => ClauseExecutedBy; set => ClauseExecutedBy = value; } // توافق خلفي
    public DateTime? DispatchedAt { get => ClauseExecutedAt; set => ClauseExecutedAt = value; }

    [MaxLength(100)]
    public string? ReturnedBy { get; set; } // من قام بتسجيل العودة
    public DateTime? ReturnedAt { get; set; } // وقت تسجيل العودة

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
