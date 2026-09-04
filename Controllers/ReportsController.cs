using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin,Supervisor,Manager,Officer")]
public class ReportsController : Controller
{
    private readonly IRosterService _rosterService;

    public ReportsController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("Reports")]
    public async Task<IActionResult> Index([FromQuery] string? reportType, [FromQuery] int? soldierTypeId)
    {
        var types = await _rosterService.GetSoldierTypesAsync();
        var all = await _rosterService.GetFilteredRosterAsync(soldierTypeId, null, null);

        var type = reportType ?? "on_leave";
        var filtered = type switch
        {
            "leave_today" => all.Where(x => x.Status == SoldierDutyStatus.LeaveToday).ToList(),
            "eligible_tomorrow" => all.Where(x => x.Status == SoldierDutyStatus.EligibleForLeave).ToList(),
            "return_today" => all.Where(x => x.Status == SoldierDutyStatus.ReturnToday).ToList(),
            "overdue" => all.Where(x => x.Status == SoldierDutyStatus.OverdueReturn).ToList(),
            _ => all.Where(x => x.Status == SoldierDutyStatus.OnLeave).ToList(),
        };

        ViewBag.Types = types;
        ViewBag.ReportType = type;
        ViewBag.SelectedType = soldierTypeId;

        return View(filtered);
    }
}
