using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IRosterService _rosterService;

    public DashboardController(IRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    [HttpGet("Dashboard")]
    public async Task<IActionResult> Index()
    {
        var model = await _rosterService.GetDashboardAsync();
        return View(model);
    }
}
