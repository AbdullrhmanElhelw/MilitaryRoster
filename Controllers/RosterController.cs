using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize]
[IgnoreAntiforgeryToken]
public class RosterController : Controller
{
    private readonly IRosterService _rosterService;
    private readonly ILogger<RosterController> _logger;

    public RosterController(IRosterService rosterService, ILogger<RosterController> logger)
    {
        _rosterService = rosterService;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("Roster")]
    public async Task<IActionResult> Index(
        [FromQuery] int? soldierTypeId,
        [FromQuery] SoldierDutyStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] DateOnly? today = null)
    {
        if (User.IsInRole("Employee"))
        {
            return RedirectToAction("Index", "MyLeave");
        }

        var model = await _rosterService.GetRosterDashboardAsync(soldierTypeId, status, search, page, pageSize);
        return View(model);
    }

    [HttpGet("Roster/Filter")]
    public async Task<IActionResult> Filter(
        [FromQuery] int? soldierTypeId,
        [FromQuery] SoldierDutyStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var pagedItems = await _rosterService.GetFilteredPagedRosterAsync(soldierTypeId, status, search, page, pageSize);
        return PartialView("_RosterTableContent", pagedItems);
    }

    [HttpGet("Roster/KpiCards")]
    public async Task<IActionResult> KpiCards()
    {
        var kpi = await _rosterService.GetKpiSummaryAsync();
        return PartialView("_KpiCards", kpi);
    }

    [HttpGet("Roster/Row/{id:int}")]
    public async Task<IActionResult> Row(int id)
    {
        var item = await _rosterService.GetRosterItemAsync(id);
        if (item == null) return NotFound();
        return PartialView("_SoldierRow", item);
    }

    [HttpPost("Roster/Dispatch/{id:int}")]
    public async Task<IActionResult> Dispatch(int id, [FromForm] string? dispatchedBy)
    {
        var officerName = string.IsNullOrWhiteSpace(dispatchedBy) ? "نوبتجي العمليات" : dispatchedBy.Trim();
        var item = await _rosterService.DispatchSoldierAsync(id, officerName);
        if (item == null) return NotFound();

        Response.Headers["HX-Trigger"] = "rosterUpdated";
        return PartialView("_SoldierRow", item);
    }

    [HttpPost("Roster/BatchDispatch")]
    public async Task<IActionResult> BatchDispatch(
        [FromForm] string? dispatchedBy,
        [FromQuery] int? soldierTypeId,
        [FromQuery] SoldierDutyStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var officerName = string.IsNullOrWhiteSpace(dispatchedBy) ? "نوبتجي العمليات" : dispatchedBy.Trim();
        int count = await _rosterService.BatchDispatchEligibleAsync(officerName);

        var pagedItems = await _rosterService.GetFilteredPagedRosterAsync(soldierTypeId, status, search, page, pageSize);
        Response.Headers["HX-Trigger"] = "rosterUpdated";
        return PartialView("_RosterTableContent", pagedItems);
    }

    [HttpPost("Roster/ConfirmReturn/{id:int}")]
    public async Task<IActionResult> ConfirmReturn(int id)
    {
        var item = await _rosterService.ConfirmReturnAsync(id);
        if (item == null) return NotFound();

        Response.Headers["HX-Trigger"] = "rosterUpdated";
        return PartialView("_SoldierRow", item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Roster/UpdateModifiers/{id:int}")]
    public async Task<IActionResult> UpdateModifiers(int id, [FromForm] int deductionDays, [FromForm] int bonusDays)
    {
        var item = await _rosterService.UpdateModifiersAsync(id, deductionDays, bonusDays);
        if (item == null) return NotFound();

        Response.Headers["HX-Trigger"] = "rosterUpdated";
        return PartialView("_SoldierRow", item);
    }

    [HttpPost("Roster/RequestModifier")]
    public async Task<IActionResult> RequestModifier(
        [FromForm] int soldierId,
        [FromForm] string requestType,
        [FromForm] int days,
        [FromForm] string? reason)
    {
        var username = User.Identity?.Name ?? "supervisor";
        var fullName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value ?? username;

        try
        {
            await _rosterService.CreateModifierRequestAsync(soldierId, requestType, days, reason, username, fullName);

            var item = await _rosterService.GetRosterItemAsync(soldierId);
            if (Request.Headers.ContainsKey("HX-Request") && item != null)
            {
                Response.Headers["HX-Trigger"] = "rosterUpdated";
                return PartialView("_SoldierRow", item);
            }

            return Json(new { success = true, message = "تم إرسال طلب الخصم/المنحة بنجاح، وهو قيد مراجعة واعتماد مدير النظام." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("Roster/UpdateReturnDate/{id:int}")]
    public async Task<IActionResult> UpdateReturnDate(int id, [FromForm] DateOnly currentReturnDate)
    {
        var item = await _rosterService.UpdateReturnDateAsync(id, currentReturnDate);
        if (item == null) return NotFound();

        Response.Headers["HX-Trigger"] = "rosterUpdated";
        return PartialView("_SoldierRow", item);
    }
}
