using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize]
public class SoldiersController : Controller
{
    private readonly IRosterService _rosterService;

    public SoldiersController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("Soldiers")]
    public async Task<IActionResult> Index(
        [FromQuery] int? soldierTypeId,
        [FromQuery] SoldierDutyStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var types = await _rosterService.GetSoldierTypesAsync();
        var paged = await _rosterService.GetFilteredPagedRosterAsync(soldierTypeId, status, search, page, pageSize);

        ViewBag.Types = types;
        ViewBag.SelectedType = soldierTypeId;
        ViewBag.SelectedStatus = status;
        ViewBag.Search = search;

        return View(paged);
    }

    [HttpGet("Soldiers/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _rosterService.GetSoldierDetailsAsync(id);
        if (model == null) return NotFound();

        var isStaff = User.IsInRole("Admin") || User.IsInRole("Supervisor") || User.IsInRole("Manager") || User.IsInRole("Officer");
        ViewBag.Notes = await _rosterService.GetSoldierNotesAsync(id, approvedOnly: !isStaff);

        return View(model);
    }

    [HttpPost("Soldiers/AddNote/{soldierId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(int soldierId, [FromForm] string noteText)
    {
        if (string.IsNullOrWhiteSpace(noteText))
        {
            TempData["ErrorMessage"] = "يرجى كتابة نص الملاحظة قبل الإرسال.";
            return RedirectToAction("Details", new { id = soldierId });
        }

        try
        {
            var username = User.Identity?.Name ?? "supervisor";
            var fullName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value ?? username;
            var role = User.IsInRole("Admin") ? "Admin" : "Supervisor";
            var isAdmin = User.IsInRole("Admin");

            await _rosterService.AddEmployeeNoteAsync(soldierId, noteText, username, fullName, role, isAdmin);

            if (isAdmin)
            {
                TempData["SuccessMessage"] = "تمت إضافة الملاحظة واعتمدت بنجاح.";
            }
            else
            {
                TempData["SuccessMessage"] = "تم تسجيل الملاحظة بنجاح، وهي الآن قيد مراجعة واعتماد مدير النظام (الأدمن).";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"تعذر إضافة الملاحظة: {ex.Message}";
        }

        return RedirectToAction("Details", new { id = soldierId });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Soldiers/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSoldierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.MilitaryNumber))
        {
            TempData["ErrorMessage"] = "يرجى استكمال جميع بيانات الموظف الأساسية (الاسم ورقم الموظف).";
            return RedirectToAction("Index");
        }

        try
        {
            await _rosterService.CreateSoldierAsync(dto);
            TempData["SuccessMessage"] = $"تم إضافة الموظف {dto.FullName.Trim()} وبدء دورته الأولى بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            if (dbEx.InnerException?.Message.Contains("IX_Soldiers_MilitaryNumber") == true ||
                dbEx.Message.Contains("IX_Soldiers_MilitaryNumber"))
            {
                TempData["ErrorMessage"] = $"رقم الموظف ({dto.MilitaryNumber?.Trim()}) مسجل بالفعل لموظف آخر في المنظومة.";
            }
            else
            {
                TempData["ErrorMessage"] = "تعذر حفظ بيانات الموظف بسبب تعارض في البيانات بقاعدة البيانات.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"حدث خطأ أثناء إضافة الموظف: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Soldiers/Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string fullName, string militaryNumber, int soldierTypeId, DateOnly serviceEndDate, string? notes)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(militaryNumber))
        {
            TempData["ErrorMessage"] = "يرجى استكمال جميع بيانات الموظف الأساسية.";
            return RedirectToAction("Index");
        }

        try
        {
            await _rosterService.UpdateSoldierAsync(id, fullName, militaryNumber, soldierTypeId, serviceEndDate, notes);
            TempData["SuccessMessage"] = $"تم تحديث بيانات الموظف {fullName.Trim()} بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            if (dbEx.InnerException?.Message.Contains("IX_Soldiers_MilitaryNumber") == true ||
                dbEx.Message.Contains("IX_Soldiers_MilitaryNumber"))
            {
                TempData["ErrorMessage"] = $"رقم الموظف ({militaryNumber.Trim()}) مسجل بالفعل لموظف آخر.";
            }
            else
            {
                TempData["ErrorMessage"] = "تعذر تحديث بيانات الموظف بسبب تعارض في البيانات.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"حدث خطأ أثناء تعديل بيانات الموظف: {ex.Message}";
        }

        return RedirectToAction("Index");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Soldiers/Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _rosterService.DeleteSoldierAsync(id);
            TempData["SuccessMessage"] = "تم حذف الموظف من المنظومة بنجاح";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"تعذر حذف الموظف: {ex.Message}";
        }

        return RedirectToAction("Index");
    }
}
