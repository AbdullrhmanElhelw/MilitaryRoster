using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin,Supervisor,Manager,Officer")]
public class PersonnelController : Controller
{
    private readonly IRosterService _rosterService;

    public PersonnelController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("Personnel")]
    public async Task<IActionResult> Index()
    {
        var soldiers = await _rosterService.GetAllPersonnelAsync();
        ViewBag.SoldierTypes = await _rosterService.GetSoldierTypesAsync();
        return View(soldiers);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("Personnel/Create")]
    public async Task<IActionResult> Create([FromForm] CreateSoldierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.MilitaryNumber))
        {
            TempData["ErrorMessage"] = "يرجى استكمال جميع بيانات الموظف الأساسية (الاسم ورقم الموظف).";
            return RedirectToAction("Index");
        }

        try
        {
            await _rosterService.CreateSoldierAsync(dto);
            TempData["SuccessMessage"] = "تم إضافة بيانات الموظف وبدء دورة الجدول بنجاح";
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
                TempData["ErrorMessage"] = $"رقم الموظف ({dto.MilitaryNumber?.Trim()}) مسجل بالفعل لموظف آخر في المنظومة، يرجى استخدام رقم مختلف.";
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

    [HttpGet("Personnel/History/{id:int}")]
    public async Task<IActionResult> History(int id)
    {
        var cycles = await _rosterService.GetSoldierCyclesAsync(id);
        var soldier = cycles.FirstOrDefault()?.Soldier 
                      ?? (await _rosterService.GetAllPersonnelAsync()).FirstOrDefault(s => s.Id == id);
        ViewBag.SoldierId = id;
        ViewBag.SoldierName = soldier?.FullName ?? "الموظف";
        ViewBag.MilitaryNumber = soldier?.MilitaryNumber ?? "";
        ViewBag.SoldierTypeName = soldier?.SoldierType?.Name ?? "";
        return PartialView("_HistoryModal", cycles);
    }
}
