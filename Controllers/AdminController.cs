using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilitaryRoster.Data;
using MilitaryRoster.Models.Entities;
using MilitaryRoster.Services;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRosterService _rosterService;

    public AdminController(ApplicationDbContext context, IRosterService rosterService)
    {
        _context = context;
        _rosterService = rosterService;
    }

    [HttpGet("Admin")]
    public async Task<IActionResult> Index(
        [FromQuery] string? soldierSearch,
        [FromQuery] int? soldierTypeId,
        [FromQuery] int soldierPage = 1,
        [FromQuery] int soldierPageSize = 10,
        [FromQuery] string tab = "analytics")
    {
        var types = await _context.SoldierTypes.Include(t => t.Soldiers).ToListAsync();
        var users = await _context.AppUsers.ToListAsync();
        var pagedSoldiers = await _rosterService.GetPagedPersonnelAsync(soldierSearch, soldierTypeId, soldierPage, soldierPageSize);
        var kpi = await _rosterService.GetKpiSummaryAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var redifSoldiers = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Where(s => s.IsActive && s.ServiceEndDate <= today.AddDays(90))
            .OrderBy(s => s.ServiceEndDate)
            .Take(10)
            .ToListAsync();

        var pendingNotes = await _rosterService.GetPendingNotesAsync();
        var pendingModifierRequests = await _rosterService.GetPendingModifierRequestsAsync();

        ViewBag.Types = types;
        ViewBag.Users = users;
        ViewBag.PagedSoldiers = pagedSoldiers;
        ViewBag.Kpi = kpi;
        ViewBag.RedifSoldiers = redifSoldiers;
        ViewBag.CurrentSearch = soldierSearch;
        ViewBag.CurrentTypeId = soldierTypeId;
        ViewBag.ActiveTab = tab;
        ViewBag.PendingNotes = pendingNotes;
        ViewBag.PendingModifierRequests = pendingModifierRequests;

        return View();
    }

    [HttpGet("Admin/SoldiersTable")]
    public async Task<IActionResult> SoldiersTable(
        [FromQuery] string? soldierSearch,
        [FromQuery] int? soldierTypeId,
        [FromQuery] int soldierPage = 1,
        [FromQuery] int soldierPageSize = 10)
    {
        var pagedSoldiers = await _rosterService.GetPagedPersonnelAsync(soldierSearch, soldierTypeId, soldierPage, soldierPageSize);
        ViewBag.Types = await _context.SoldierTypes.ToListAsync();
        return PartialView("_AdminSoldiersTable", pagedSoldiers);
    }

    [HttpPost("Admin/CreateSoldier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSoldier(CreateSoldierDto dto, [FromForm] string returnTab = "soldiers")
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.MilitaryNumber))
        {
            TempData["AdminError"] = "يرجى استكمال جميع بيانات الموظف الأساسية (الاسم ورقم الموظف).";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        try
        {
            await _rosterService.CreateSoldierAsync(dto);
            TempData["AdminSuccess"] = $"تم إضافة الموظف ({dto.FullName.Trim()}) وبدء دورته بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["AdminError"] = ex.Message;
        }
        catch (DbUpdateException dbEx)
        {
            if (dbEx.InnerException?.Message.Contains("IX_Soldiers_MilitaryNumber") == true ||
                dbEx.Message.Contains("IX_Soldiers_MilitaryNumber"))
            {
                TempData["AdminError"] = $"رقم الموظف ({dto.MilitaryNumber.Trim()}) مسجل بالفعل لموظف آخر، يرجى استخدام رقم مختلف.";
            }
            else
            {
                TempData["AdminError"] = "تعذر حفظ بيانات الموظف بسبب تعارض في البيانات بقاعدة البيانات.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"حدث خطأ غير متوقع: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/UpdateSoldier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSoldier(int id, string fullName, string militaryNumber, int soldierTypeId, DateOnly serviceEndDate, [FromForm] string returnTab = "soldiers")
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(militaryNumber))
        {
            TempData["AdminError"] = "يرجى استكمال جميع بيانات الموظف الأساسية.";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        try
        {
            await _rosterService.UpdateSoldierAsync(id, fullName, militaryNumber, soldierTypeId, serviceEndDate);
            TempData["AdminSuccess"] = $"تم تحديث بيانات الموظف ({fullName.Trim()}) بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["AdminError"] = ex.Message;
        }
        catch (DbUpdateException dbEx)
        {
            if (dbEx.InnerException?.Message.Contains("IX_Soldiers_MilitaryNumber") == true ||
                dbEx.Message.Contains("IX_Soldiers_MilitaryNumber"))
            {
                TempData["AdminError"] = $"رقم الموظف ({militaryNumber.Trim()}) مسجل بالفعل لموظف آخر، يرجى اختيار رقم آخر.";
            }
            else
            {
                TempData["AdminError"] = "تعذر تحديث بيانات الموظف بسبب تعارض في قاعدة البيانات.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"حدث خطأ أثناء تعديل بيانات الموظف: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/DeleteSoldier/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSoldier(int id, [FromForm] string returnTab = "soldiers")
    {
        try
        {
            await _rosterService.DeleteSoldierAsync(id);
            TempData["AdminSuccess"] = "تم حذف الموظف من المنظومة بنجاح";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر حذف الموظف: {ex.Message}";
        }
        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/UpdateType")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateType(int id, string name, int dutyDays, int leaveDays, string? description, [FromForm] string returnTab = "categories")
    {
        var cleanName = name?.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            TempData["AdminError"] = "اسم تصنيف الخدمة مطلوب ولا يمكن تركه فارغاً.";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        try
        {
            var type = await _context.SoldierTypes.FindAsync(id);
            if (type != null)
            {
                var duplicate = await _context.SoldierTypes.AnyAsync(t => t.Id != id && t.Name.ToLower() == cleanName.ToLower());
                if (duplicate)
                {
                    TempData["AdminError"] = $"اسم التصنيف ({cleanName}) مسجل بالفعل لتصنيف آخر.";
                    return RedirectToAction("Index", new { tab = returnTab });
                }

                type.Name = cleanName;
                type.DutyDays = Math.Max(1, dutyDays);
                type.LeaveDays = Math.Max(1, leaveDays);
                type.Description = description?.Trim();
                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"تم تحديث إعدادات تصنيف ({type.Name}) بنجاح";
            }
            else
            {
                TempData["AdminError"] = "تصنيف الخدمة غير موجود.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر تحديث تصنيف الخدمة: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/DeleteType/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteType(int id, [FromForm] string returnTab = "categories")
    {
        try
        {
            var type = await _context.SoldierTypes.Include(t => t.Soldiers).FirstOrDefaultAsync(t => t.Id == id);
            if (type == null)
            {
                TempData["AdminError"] = "تصنيف الخدمة غير موجود.";
                return RedirectToAction("Index", new { tab = returnTab });
            }

            var activeSoldiers = type.Soldiers.Count;
            if (activeSoldiers > 0)
            {
                TempData["AdminError"] = $"لا يمكن حذف تصنيف ({type.Name}) لأنه مرتبط حالياً بـ ({activeSoldiers}) موظف. يرجى تعديل فئة هؤلاء الموظفين أولاً قبل الحذف.";
                return RedirectToAction("Index", new { tab = returnTab });
            }

            var cyclesCount = await _context.LeaveCycles.CountAsync(c => c.SoldierTypeId == id);
            if (cyclesCount > 0)
            {
                type.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"تم أرشفة وإلغاء تفعيل تصنيف ({type.Name}) بنجاح لوجود دورات تاريخية سابقة مسجلة عليه.";
            }
            else
            {
                _context.SoldierTypes.Remove(type);
                await _context.SaveChangesAsync();
                TempData["AdminSuccess"] = $"تم حذف تصنيف الخدمة ({type.Name}) نهائياً بنجاح.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر حذف تصنيف الخدمة: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/CreateType")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(string name, int dutyDays, int leaveDays, string? description, [FromForm] string returnTab = "categories")
    {
        var cleanName = name?.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            TempData["AdminError"] = "يرجى كتابة اسم تصنيف الخدمة الجديد.";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        try
        {
            var exists = await _context.SoldierTypes.AnyAsync(t => t.Name.ToLower() == cleanName.ToLower());
            if (exists)
            {
                TempData["AdminError"] = $"تصنيف الخدمة ({cleanName}) مسجل بالفعل مسبقاً.";
                return RedirectToAction("Index", new { tab = returnTab });
            }

            var type = new SoldierType
            {
                Name = cleanName,
                DutyDays = Math.Max(1, dutyDays),
                LeaveDays = Math.Max(1, leaveDays),
                Description = description,
                IsActive = true
            };
            _context.SoldierTypes.Add(type);
            await _context.SaveChangesAsync();
            TempData["AdminSuccess"] = $"تم إضافة تصنيف الخدمة الجديد ({type.Name}) بنجاح";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر إضافة تصنيف الخدمة: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/CreateUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string username, string password, string fullName, string role, int? soldierId, [FromForm] string returnTab = "users")
    {
        var cleanUser = username?.Trim();
        var cleanPass = password?.Trim();
        var cleanName = fullName?.Trim();

        if (string.IsNullOrWhiteSpace(cleanUser) || string.IsNullOrWhiteSpace(cleanPass) || string.IsNullOrWhiteSpace(cleanName))
        {
            TempData["AdminError"] = "يرجى استكمال جميع بيانات المستخدم الجديد (اسم المستخدم، كلمة المرور، الاسم الكامل).";
            return RedirectToAction("Index", new { tab = returnTab });
        }

        try
        {
            var exists = await _context.AppUsers.AnyAsync(u => u.Username.ToLower() == cleanUser.ToLower());
            if (exists)
            {
                TempData["AdminError"] = $"اسم المستخدم ({cleanUser}) مسجل بالفعل، يرجى اختيار اسم مستخدم آخر.";
                return RedirectToAction("Index", new { tab = returnTab });
            }

            var user = new AppUser
            {
                Username = cleanUser,
                PasswordHash = PasswordHelper.HashPassword(cleanPass),
                FullName = cleanName,
                Role = role ?? "Supervisor",
                SoldierId = (role == "Employee") ? soldierId : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();
            TempData["AdminSuccess"] = $"تم إضافة المستخدم ({user.FullName}) بنجاح بصلاحية: {user.Role}";
        }
        catch (DbUpdateException)
        {
            TempData["AdminError"] = $"اسم المستخدم ({cleanUser}) مسجل مسبقاً في النظام.";
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر إضافة المستخدم: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/ApproveNote/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveNote(int id, [FromForm] string returnTab = "notes")
    {
        try
        {
            var adminName = User.Identity?.Name ?? "مدير النظام";
            var success = await _rosterService.ApproveNoteAsync(id, adminName);
            if (success)
            {
                TempData["AdminSuccess"] = "تم اعتماد الملاحظة بنجاح، وتظهر الآن في ملف الموظف وبوابته الشخصية.";
            }
            else
            {
                TempData["AdminError"] = "الملاحظة المطلوبة غير موجودة.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر اعتماد الملاحظة: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/RejectNote/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectNote(int id, string? reason, [FromForm] string returnTab = "notes")
    {
        try
        {
            var adminName = User.Identity?.Name ?? "مدير النظام";
            var success = await _rosterService.RejectNoteAsync(id, adminName, reason);
            if (success)
            {
                TempData["AdminSuccess"] = "تم رفض الملاحظة بنجاح.";
            }
            else
            {
                TempData["AdminError"] = "الملاحظة المطلوبة غير موجودة.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر رفض الملاحظة: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/ApproveModifierRequest/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveModifierRequest(int id, [FromForm] string returnTab = "modifiers")
    {
        try
        {
            var adminName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value ?? User.Identity?.Name ?? "مدير النظام";
            var success = await _rosterService.ApproveModifierRequestAsync(id, adminName);
            if (success)
            {
                TempData["AdminSuccess"] = "تم اعتماد وتطبيق طلب الخصم/المنحة بنجاح على دورة الموظف في الجدول.";
            }
            else
            {
                TempData["AdminError"] = "طلب الخصم/المنحة المطلوب غير موجود أو تم البت فيه مسبقاً.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر اعتماد الطلب: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }

    [HttpPost("Admin/RejectModifierRequest/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectModifierRequest(int id, [FromForm] string? comment, [FromForm] string returnTab = "modifiers")
    {
        try
        {
            var adminName = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.GivenName)?.Value ?? User.Identity?.Name ?? "مدير النظام";
            var success = await _rosterService.RejectModifierRequestAsync(id, adminName, comment);
            if (success)
            {
                TempData["AdminSuccess"] = "تم رفض طلب الخصم/المنحة بنجاح.";
            }
            else
            {
                TempData["AdminError"] = "طلب الخصم/المنحة المطلوب غير موجود أو تم البت فيه مسبقاً.";
            }
        }
        catch (Exception ex)
        {
            TempData["AdminError"] = $"تعذر رفض الطلب: {ex.Message}";
        }

        return RedirectToAction("Index", new { tab = returnTab });
    }
}
