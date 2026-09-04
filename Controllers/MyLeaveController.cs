using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Services;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Employee")]
public class MyLeaveController : Controller
{
    private readonly IRosterService _rosterService;

    public MyLeaveController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("MyLeave")]
    public async Task<IActionResult> Index()
    {
        int userId = 0;
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var soldier = await _rosterService.GetSoldierByUserIdAsync(userId);
        if (soldier == null)
        {
            ViewBag.ErrorMessage = "حسابك غير مرتبط بملف موظف حتى الآن. يرجى التواصل مع إدارة المنظومة.";
            return View(null);
        }

        var details = await _rosterService.GetSoldierDetailsAsync(soldier.Id);
        var approvedNotes = await _rosterService.GetSoldierNotesAsync(soldier.Id, approvedOnly: true);
        ViewBag.ApprovedNotes = approvedNotes;

        return View(details);
    }
}