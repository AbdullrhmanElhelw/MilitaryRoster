using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin,Supervisor,Manager,Officer")]
public class ReturnManagementController : Controller
{
    private readonly IRosterService _rosterService;

    public ReturnManagementController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("ReturnManagement")]
    public async Task<IActionResult> Index(string? search, int? soldierTypeId, string? statusFilter)
    {
        var all = await _rosterService.GetFilteredRosterAsync(soldierTypeId, null, search);

        // تصفية الموظفين المعنيين بالعودة: في إجازة، عودة اليوم، أو متأخرين عن العودة
        var query = all.AsEnumerable();

        if (statusFilter == "returnToday")
        {
            query = query.Where(s => s.Status == SoldierDutyStatus.ReturnToday);
        }
        else if (statusFilter == "overdue")
        {
            query = query.Where(s => s.Status == SoldierDutyStatus.OverdueReturn);
        }
        else if (statusFilter == "onLeave")
        {
            query = query.Where(s => s.Status == SoldierDutyStatus.OnLeave);
        }
        else
        {
            // الافتراضي: كل من هو في الإجازة أو عودة اليوم أو متأخر
            query = query.Where(s => s.Status == SoldierDutyStatus.ReturnToday || 
                                     s.Status == SoldierDutyStatus.OverdueReturn || 
                                     s.Status == SoldierDutyStatus.OnLeave);
        }

        var returningSoldiers = query
            .OrderBy(s => s.Status == SoldierDutyStatus.OverdueReturn ? 0 : 
                          s.Status == SoldierDutyStatus.ReturnToday ? 1 : 2)
            .ThenBy(s => s.ExpectedReturnDate)
            .ToList();

        ViewBag.Search = search;
        ViewBag.SoldierTypeId = soldierTypeId;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.AvailableTypes = await _rosterService.GetSoldierTypesAsync();

        return View(returningSoldiers);
    }

    [HttpPost("ReturnManagement/Register")]
    public async Task<IActionResult> Register([FromForm] ReturnRegistrationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReturnedBy))
        {
            dto.ReturnedBy = User.Identity?.Name ?? "نوبتجي العمليات";
        }

        bool ok = await _rosterService.RegisterReturnAsync(dto);
        if (ok)
        {
            TempData["SuccessMessage"] = "تم تسجيل تمام الوصول وبدء دورة التواجد الجديدة للموظف بنجاح.";
        }
        else
        {
            TempData["ErrorMessage"] = "تعذر تسجيل العودة. تأكد من صحة بيانات الموظف.";
        }
        return RedirectToAction("Index");
    }

    // تسجيل سريع فوري بنقرة واحدة (One-click Quick Confirm)
    [HttpPost("ReturnManagement/QuickConfirm/{id:int}")]
    public async Task<IActionResult> QuickConfirm(int id)
    {
        var item = await _rosterService.ConfirmReturnAsync(id);
        if (item != null)
        {
            TempData["SuccessMessage"] = $"تم إثبات تمام عودة واستلام الموظف {item.FullName} وبدء دورته الجديدة.";
        }
        return RedirectToAction("Index");
    }
}
