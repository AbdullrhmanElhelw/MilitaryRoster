using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin,Supervisor,Manager,Officer")]
public class LeaveScheduleController : Controller
{
    private readonly IRosterService _rosterService;

    public LeaveScheduleController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("LeaveSchedule")]
    public async Task<IActionResult> Index([FromQuery] int? soldierTypeId, [FromQuery] string? viewMode)
    {
        var types = await _rosterService.GetSoldierTypesAsync();
        var soldiers = await _rosterService.GetFilteredRosterAsync(soldierTypeId, null, null);

        ViewBag.Types = types;
        ViewBag.SelectedType = soldierTypeId;
        ViewBag.ViewMode = viewMode ?? "list"; // calendar or list

        return View(soldiers);
    }
}
