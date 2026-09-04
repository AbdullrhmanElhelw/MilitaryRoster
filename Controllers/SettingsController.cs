using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MilitaryRoster.Data;
using MilitaryRoster.Models.Entities;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("Settings")]
    public async Task<IActionResult> Index(
        string? search = null,
        string? actionType = null,
        string? entityName = null,
        string? performedBy = null,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        int page = 1,
        int pageSize = 15,
        string tab = "logs")
    {
        if (page < 1) page = 1;
        if (pageSize < 5) pageSize = 15;
        if (pageSize > 100) pageSize = 100;

        var baseQuery = _context.AuditLogs.AsNoTracking();

        // قوائم الفلترة المتوفرة في قاعدة البيانات
        var availableActionTypes = await baseQuery
            .Select(a => a.ActionType)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var availableEntities = await baseQuery
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        var availableActors = await baseQuery
            .Select(a => a.PerformedBy)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        // إحصائيات عامة لسجل التدقيق
        var totalLogsCount = await baseQuery.CountAsync();
        var todayStart = DateTime.UtcNow.Date;
        var todayLogsCount = await baseQuery.CountAsync(a => a.PerformedAt >= todayStart);
        var uniqueActorsCount = availableActors.Count;
        var latestActionTime = await baseQuery.MaxAsync(a => (DateTime?)a.PerformedAt);

        // تطبيق الفلاتر
        var filteredQuery = baseQuery;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filteredQuery = filteredQuery.Where(a =>
                (a.Details != null && a.Details.Contains(s)) ||
                a.PerformedBy.Contains(s) ||
                a.ActionType.Contains(s) ||
                a.EntityName.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            filteredQuery = filteredQuery.Where(a => a.ActionType == actionType);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            filteredQuery = filteredQuery.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(performedBy))
        {
            filteredQuery = filteredQuery.Where(a => a.PerformedBy == performedBy);
        }

        if (dateFrom.HasValue)
        {
            var fromDt = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(a => a.PerformedAt >= fromDt);
        }

        if (dateTo.HasValue)
        {
            var toDt = dateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            filteredQuery = filteredQuery.Where(a => a.PerformedAt <= toDt);
        }

        var filteredCount = await filteredQuery.CountAsync();

        var items = await filteredQuery
            .OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // إحصائيات النظام وقاعدة البيانات
        var soldiersCount = await _context.Soldiers.CountAsync();
        var activeSoldiersCount = await _context.Soldiers.CountAsync(s => s.IsActive);
        var leaveCyclesCount = await _context.LeaveCycles.CountAsync();
        var notesCount = await _context.EmployeeNotes.CountAsync();
        var modifierRequestsCount = await _context.ModifierRequests.CountAsync();
        var usersCount = await _context.AppUsers.CountAsync();

        var vm = new SettingsViewModel
        {
            Logs = new PagedList<AuditLog>
            {
                Items = items,
                TotalCount = filteredCount,
                PageNumber = page,
                PageSize = pageSize
            },
            TotalLogsCount = totalLogsCount,
            TodayLogsCount = todayLogsCount,
            UniqueActorsCount = uniqueActorsCount,
            LatestActionTime = latestActionTime,

            Search = search,
            SelectedActionType = actionType,
            SelectedEntity = entityName,
            SelectedActor = performedBy,
            DateFrom = dateFrom,
            DateTo = dateTo,
            ActiveTab = tab,

            AvailableActionTypes = availableActionTypes,
            AvailableEntities = availableEntities,
            AvailableActors = availableActors,

            SoldiersCount = soldiersCount,
            ActiveSoldiersCount = activeSoldiersCount,
            LeaveCyclesCount = leaveCyclesCount,
            NotesCount = notesCount,
            ModifierRequestsCount = modifierRequestsCount,
            UsersCount = usersCount,
            IsDatabaseConnected = true,
            ServerTime = DateTime.UtcNow
        };

        return View(vm);
    }
}
