using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;

namespace MilitaryRoster.ViewModels;

public class KpiSummaryViewModel
{
    public int TotalForce { get; set; }           // إجمالي الموظفين
    public int OnDutyCount { get; set; }          // الموظفين المتواجدون
    public int OnLeaveCount { get; set; }         // الموظفين في إجازة
    public int LeaveTodayCount { get; set; }      // الموظفين الذين موعد نزولهم اليوم
    public int EligibleTomorrowCount { get; set; } // الموظفين الذين موعد نزولهم غداً
    public int ReturnTodayCount { get; set; }     // الموظفين الذين موعد عودتهم اليوم
    public int OverdueReturnCount { get; set; }   // الموظفين المتأخرون عن العودة
}

public class DashboardViewModel
{
    public KpiSummaryViewModel Kpi { get; set; } = new();
    public List<RosterItemViewModel> EligibleTomorrow { get; set; } = new();
    public List<RosterItemViewModel> LeaveToday { get; set; } = new();
    public List<RosterItemViewModel> ReturnToday { get; set; } = new();
    public List<RosterItemViewModel> OverdueReturn { get; set; } = new();
    public List<SoldierType> AvailableTypes { get; set; } = new();
    public DateOnly Today { get; set; }
}

public class ReturnRegistrationDto
{
    public int SoldierId { get; set; }
    public DateOnly ActualReturnDate { get; set; }
    public TimeSpan ActualReturnTime { get; set; }
    public string? ReturnedBy { get; set; }
    public string? Notes { get; set; }
}

public class LeaveClauseExecutionDto
{
    public int SoldierId { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class SoldierDetailsViewModel
{
    public Soldier Soldier { get; set; } = null!;
    public RosterItemViewModel CurrentStatus { get; set; } = null!;
    public List<LeaveCycle> CycleHistory { get; set; } = new();
}
