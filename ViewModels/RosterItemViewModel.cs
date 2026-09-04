using MilitaryRoster.Models.Enums;

namespace MilitaryRoster.ViewModels;

public class RosterItemViewModel
{
    public int SoldierId { get; set; }
    public long CycleId { get; set; }
    public int CycleNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MilitaryNumber { get; set; } = string.Empty;
    public int SoldierTypeId { get; set; }
    public string SoldierTypeName { get; set; } = string.Empty;
    public int DutyDays { get; set; }
    public int LeaveDays { get; set; }

    public DateOnly LastReturnDate { get; set; }
    public DateOnly LeaveStartDate { get; set; }
    public DateOnly ExpectedReturnDate { get; set; }
    public DateOnly? ActualLeaveDate { get; set; }
    public DateOnly? ActualReturnDate { get; set; }

    public int DeductionDays { get; set; }
    public int BonusDays { get; set; }

    public SoldierDutyStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public int? CurrentDutyDayNumber { get; set; }
    public int RemainingDays { get; set; }

    public bool CanDispatch => Status == SoldierDutyStatus.LeaveToday || Status == SoldierDutyStatus.EligibleForLeave;
    public bool CanConfirmReturn => Status == SoldierDutyStatus.OnLeave || Status == SoldierDutyStatus.ReturnToday;

    public DateOnly ServiceEndDate { get; set; }
    public bool IsDemobSoon { get; set; }

    public string StatusBadgeClass => Status switch
    {
        SoldierDutyStatus.EligibleForLeave => "bg-amber-500/20 text-amber-300 border-amber-500/40 animate-pulse",
        SoldierDutyStatus.LeaveToday => "bg-emerald-500/20 text-emerald-300 border-emerald-500/40 font-bold",
        SoldierDutyStatus.ReturnToday => "bg-blue-500/20 text-blue-300 border-blue-500/40",
        SoldierDutyStatus.OnLeave => "bg-purple-500/20 text-purple-300 border-purple-500/40",
        SoldierDutyStatus.OnDuty => "bg-slate-800 text-slate-300 border-slate-700",
        _ => "bg-slate-800 text-slate-300 border-slate-700"
    };

    public string DutyDayDisplay => CurrentDutyDayNumber.HasValue
        ? $"اليوم {CurrentDutyDayNumber} من {DutyDays}"
        : Status == SoldierDutyStatus.OnLeave ? "في راحة" : "استلام خدمة";

    public bool HasPendingModifierRequest { get; set; }
    public string? PendingModifierSummary { get; set; }
    public int? PendingModifierId { get; set; }
}
