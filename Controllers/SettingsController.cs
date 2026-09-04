using Microsoft.AspNetCore.Mvc;
using MilitaryRoster.Data;

namespace MilitaryRoster.Controllers;

public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("Settings")]
    public IActionResult Index()
    {
        return View();
    }
}
