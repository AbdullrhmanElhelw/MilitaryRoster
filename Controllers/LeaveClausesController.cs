using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin,Supervisor,Manager,Officer")]
public class LeaveClausesController : Controller
{
    private readonly IRosterService _rosterService;

    public LeaveClausesController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("LeaveClauses")]
    public async Task<IActionResult> Index()
    {
        // الموظفين المستحقون لاتخاذ إجراء النزول (نزول غداً أو نزول اليوم)
        var all = await _rosterService.GetFilteredRosterAsync(null, null, null);
        var eligible = all
            .Where(s => s.Status == SoldierDutyStatus.EligibleForLeave || s.Status == SoldierDutyStatus.LeaveToday)
            .OrderBy(s => s.LeaveStartDate)
            .ToList();

        return View(eligible);
    }

    [HttpPost("LeaveClauses/Execute")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(LeaveClauseExecutionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ExecutedBy))
        {
            dto.ExecutedBy = User.Identity?.Name ?? "نوبتجي العمليات";
        }

        bool ok = await _rosterService.ExecuteLeaveClauseAsync(dto);
        if (ok)
        {
            TempData["SuccessMessage"] = "تم اعتماد وتنفيذ البند وتوثيقه في السجل بنجاح";
        }
        return RedirectToAction("Index");
    }
}
