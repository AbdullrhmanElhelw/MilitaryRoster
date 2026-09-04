using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;

namespace MilitaryRoster.ViewModels;

public class RosterDashboardViewModel
{
    public KpiSummaryViewModel Kpi { get; set; } = new();
    public PagedList<RosterItemViewModel> PagedItems { get; set; } = new();
    public List<SoldierType> AvailableTypes { get; set; } = new();
    public int? FilterSoldierTypeId { get; set; }
    public SoldierDutyStatus? FilterStatus { get; set; }
    public string? SearchQuery { get; set; }
    public DateOnly Today { get; set; }
    public int PendingModifierRequestsCount { get; set; }
}
