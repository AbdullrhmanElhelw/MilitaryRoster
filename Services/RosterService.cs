using Microsoft.EntityFrameworkCore;
using MilitaryRoster.Data;
using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Services;

public class RosterService : IRosterService
{
    private readonly ApplicationDbContext _context;
    private readonly IRosterCalculationService _calcService;

    public RosterService(ApplicationDbContext context, IRosterCalculationService calcService)
    {
        _context = context;
        _calcService = calcService;
    }

    private DateOnly GetEffectiveToday(DateOnly? overrideToday) => overrideToday ?? DateOnly.FromDateTime(DateTime.Today);

    public async Task<List<SoldierType>> GetSoldierTypesAsync()
    {
        return await _context.SoldierTypes.AsNoTracking().Where(t => t.IsActive).ToListAsync();
    }

    public async Task<RosterDashboardViewModel> GetRosterDashboardAsync(
        int? soldierTypeId,
        SoldierDutyStatus? status,
        string? search,
        int page = 1,
        int pageSize = 10,
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var types = await GetSoldierTypesAsync();
        var kpi = await GetKpiSummaryAsync(today);
        var pagedItems = await GetFilteredPagedRosterAsync(soldierTypeId, status, search, page, pageSize, today);

        var pendingModCount = await _context.ModifierRequests.CountAsync(m => m.Status == NoteStatus.Pending);

        return new RosterDashboardViewModel
        {
            Kpi = kpi,
            PagedItems = pagedItems,
            AvailableTypes = types,
            FilterSoldierTypeId = soldierTypeId,
            FilterStatus = status,
            SearchQuery = search,
            Today = today,
            PendingModifierRequestsCount = pendingModCount
        };
    }

    public async Task<RosterItemViewModel?> ConfirmReturnAsync(int soldierId, DateOnly? actualReturnDate = null, DateOnly? overrideToday = null)
    {
        var dto = new ReturnRegistrationDto
        {
            SoldierId = soldierId,
            ActualReturnDate = actualReturnDate ?? DateOnly.FromDateTime(DateTime.Today),
            ActualReturnTime = DateTime.Now.TimeOfDay,
            ReturnedBy = "نوبتجي العمليات"
        };
        await RegisterReturnAsync(dto, overrideToday);
        return await GetRosterItemAsync(soldierId, overrideToday);
    }

    public async Task<List<Soldier>> GetAllPersonnelAsync()
    {
        return await _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .OrderBy(s => s.FullName)
            .ToListAsync();
    }

    public async Task<DashboardViewModel> GetDashboardAsync(DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var kpi = await GetKpiSummaryAsync(today);
        var types = await GetSoldierTypesAsync();
        var allItems = await GetFilteredRosterAsync(null, null, null, today);

        return new DashboardViewModel
        {
            Kpi = kpi,
            EligibleTomorrow = allItems.Where(x => x.Status == SoldierDutyStatus.EligibleForLeave).ToList(),
            LeaveToday = allItems.Where(x => x.Status == SoldierDutyStatus.LeaveToday).ToList(),
            ReturnToday = allItems.Where(x => x.Status == SoldierDutyStatus.ReturnToday).ToList(),
            OverdueReturn = allItems.Where(x => x.Status == SoldierDutyStatus.OverdueReturn).ToList(),
            AvailableTypes = types,
            Today = today
        };
    }

    public async Task<PagedList<RosterItemViewModel>> GetFilteredPagedRosterAsync(
        int? soldierTypeId,
        SoldierDutyStatus? status,
        string? search,
        int page = 1,
        int pageSize = 10,
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var allFiltered = await GetFilteredRosterAsync(soldierTypeId, status, search, today);

        int totalCount = allFiltered.Count;
        int currentPage = Math.Max(1, page);
        int validPageSize = Math.Max(1, pageSize);

        var pagedData = allFiltered
            .Skip((currentPage - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        return new PagedList<RosterItemViewModel>
        {
            Items = pagedData,
            TotalCount = totalCount,
            PageNumber = currentPage,
            PageSize = validPageSize
        };
    }

    public async Task<List<RosterItemViewModel>> GetFilteredRosterAsync(
        int? soldierTypeId,
        SoldierDutyStatus? status,
        string? search,
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);

        var query = _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Include(s => s.ModifierRequests)
            .Where(s => s.IsActive);

        if (soldierTypeId.HasValue)
        {
            query = query.Where(s => s.SoldierTypeId == soldierTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim();
            query = query.Where(s => s.FullName.Contains(cleanSearch) || s.MilitaryNumber.Contains(cleanSearch));
        }

        var soldiers = await query.ToListAsync();

        var viewModels = soldiers
            .Select(s => MapToViewModel(s, today))
            .Where(vm => vm != null)
            .Select(vm => vm!)
            .ToList();

        if (status.HasValue)
        {
            viewModels = viewModels.Where(vm => vm.Status == status.Value).ToList();
        }

        return viewModels
            .OrderBy(vm => GetStatusSortOrder(vm.Status))
            .ThenBy(vm => vm.RemainingDays)
            .ToList();
    }

    public async Task<RosterItemViewModel?> GetRosterItemAsync(int soldierId, DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Include(s => s.ModifierRequests)
            .FirstOrDefaultAsync(s => s.Id == soldierId);

        if (soldier == null) return null;
        return MapToViewModel(soldier, today);
    }

    public async Task<SoldierDetailsViewModel?> GetSoldierDetailsAsync(int soldierId, DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .FirstOrDefaultAsync(s => s.Id == soldierId);

        if (soldier == null) return null;

        var currentStatus = MapToViewModel(soldier, today);
        if (currentStatus == null) return null;

        var history = soldier.LeaveCycles
            .OrderByDescending(c => c.CycleNumber)
            .ToList();

        return new SoldierDetailsViewModel
        {
            Soldier = soldier,
            CurrentStatus = currentStatus,
            CycleHistory = history
        };
    }

    public async Task<KpiSummaryViewModel> GetKpiSummaryAsync(DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldiers = await _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Where(s => s.IsActive)
            .ToListAsync();

        var items = soldiers
            .Select(s => MapToViewModel(s, today))
            .Where(vm => vm != null)
            .Select(vm => vm!)
            .ToList();

        return new KpiSummaryViewModel
        {
            TotalForce = items.Count,
            OnDutyCount = items.Count(x => x.Status == SoldierDutyStatus.OnDuty),
            OnLeaveCount = items.Count(x => x.Status == SoldierDutyStatus.OnLeave),
            LeaveTodayCount = items.Count(x => x.Status == SoldierDutyStatus.LeaveToday),
            EligibleTomorrowCount = items.Count(x => x.Status == SoldierDutyStatus.EligibleForLeave),
            ReturnTodayCount = items.Count(x => x.Status == SoldierDutyStatus.ReturnToday),
            OverdueReturnCount = items.Count(x => x.Status == SoldierDutyStatus.OverdueReturn)
        };
    }

    public async Task<bool> ExecuteLeaveClauseAsync(LeaveClauseExecutionDto dto, DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .FirstOrDefaultAsync(s => s.Id == dto.SoldierId);

        if (soldier == null) return false;
        var cycle = soldier.CurrentCycle;
        if (cycle == null) return false;

        cycle.ActualLeaveDate = today >= cycle.LeaveStartDate ? today : cycle.LeaveStartDate;
        cycle.ClauseExecutedBy = string.IsNullOrWhiteSpace(dto.ExecutedBy) ? "نوبتجي العمليات" : dto.ExecutedBy;
        cycle.ClauseExecutedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            cycle.Notes = (cycle.Notes + " | " + dto.Notes).Trim(' ', '|');
        }

        // تسجيل في سجل الـ Audit Log
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "Soldier",
            ActionType = "ExecuteLeaveClause",
            PerformedBy = cycle.ClauseExecutedBy,
            PerformedAt = DateTime.UtcNow,
            Details = $"تم تنفيذ بند النزول للموظف {soldier.FullName} (رقم الموظف: {soldier.MilitaryNumber}) موعد النزول: {cycle.LeaveStartDate:yyyy-MM-dd}"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RegisterReturnAsync(ReturnRegistrationDto dto, DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .FirstOrDefaultAsync(s => s.Id == dto.SoldierId);

        if (soldier == null) return false;
        var cycle = soldier.CurrentCycle;
        if (cycle == null) return false;

        cycle.ActualReturnDate = dto.ActualReturnDate;
        cycle.ActualReturnTime = dto.ActualReturnTime;
        cycle.ReturnedBy = string.IsNullOrWhiteSpace(dto.ReturnedBy) ? "نوبتجي العمليات" : dto.ReturnedBy;
        cycle.ReturnedAt = DateTime.UtcNow;
        cycle.IsCompleted = true;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            cycle.Notes = (cycle.Notes + " | عودة: " + dto.Notes).Trim(' ', '|');
        }

        // حساب الدورة التالية: يوم العودة لا يُحسب تواجد!
        // يبدأ التواجد من اليوم التالي:
        var returnDate = dto.ActualReturnDate;
        var nextLeave = _calcService.CalculateLeaveStartDate(returnDate, soldier.SoldierType.DutyDays, 0);
        var nextReturn = _calcService.CalculateExpectedReturnDate(nextLeave, soldier.SoldierType.LeaveDays, 0);

        var nextCycle = new LeaveCycle
        {
            SoldierId = soldier.Id,
            SoldierTypeId = soldier.SoldierTypeId,
            CycleNumber = cycle.CycleNumber + 1,
            LastReturnDate = returnDate,
            LeaveStartDate = nextLeave,
            ExpectedReturnDate = nextReturn,
            DutyDaysSnapshot = soldier.SoldierType.DutyDays,
            LeaveDaysSnapshot = soldier.SoldierType.LeaveDays,
            DeductionDays = 0,
            BonusDays = 0,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveCycles.Add(nextCycle);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "Soldier",
            ActionType = "RegisterReturn",
            PerformedBy = cycle.ReturnedBy,
            PerformedAt = DateTime.UtcNow,
            Details = $"تم تسجيل عودة الموظف {soldier.FullName} بتاريخ {dto.ActualReturnDate:yyyy-MM-dd} وبدء دورة #{nextCycle.CycleNumber} (النزول القادم: {nextLeave:yyyy-MM-dd})"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<RosterItemViewModel?> DispatchSoldierAsync(
        int soldierId,
        string dispatchedBy = "نوبتجي العمليات",
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .FirstOrDefaultAsync(s => s.Id == soldierId);

        if (soldier == null) return null;
        var cycle = soldier.CurrentCycle;
        if (cycle == null) return null;

        // توثيق النزول الفعلي وإتمام الدورة الحالية
        cycle.ActualLeaveDate = cycle.LeaveStartDate;
        cycle.ActualReturnDate = cycle.ExpectedReturnDate;
        cycle.ClauseExecutedBy = dispatchedBy;
        cycle.ClauseExecutedAt = DateTime.UtcNow;
        cycle.ReturnedBy = dispatchedBy;
        cycle.ReturnedAt = DateTime.UtcNow;
        cycle.IsCompleted = true;

        // ترحيل الدورة القادمة تلقائياً (تاريخ عودة الاستلام الجديد = ExpectedReturnDate)
        var newReturnDate = cycle.ExpectedReturnDate;
        var nextLeave = _calcService.CalculateLeaveStartDate(newReturnDate, soldier.SoldierType.DutyDays, 0);
        var nextReturn = _calcService.CalculateExpectedReturnDate(nextLeave, soldier.SoldierType.LeaveDays, 0);

        var nextCycle = new LeaveCycle
        {
            SoldierId = soldier.Id,
            SoldierTypeId = soldier.SoldierTypeId,
            CycleNumber = cycle.CycleNumber + 1,
            LastReturnDate = newReturnDate,
            LeaveStartDate = nextLeave,
            ExpectedReturnDate = nextReturn,
            DutyDaysSnapshot = soldier.SoldierType.DutyDays,
            LeaveDaysSnapshot = soldier.SoldierType.LeaveDays,
            DeductionDays = 0,
            BonusDays = 0,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveCycles.Add(nextCycle);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "Soldier",
            ActionType = "DispatchSoldier",
            PerformedBy = dispatchedBy,
            PerformedAt = DateTime.UtcNow,
            Details = $"تم تنفيذ نزول الموظف {soldier.FullName} وترحيل الدورة إلى #{nextCycle.CycleNumber} (تاريخ الاستلام: {newReturnDate:yyyy-MM-dd}، النزول القادم: {nextLeave:yyyy-MM-dd})"
        });

        await _context.SaveChangesAsync();
        return MapToViewModel(soldier, today);
    }

    public async Task<int> BatchDispatchEligibleAsync(
        string dispatchedBy = "نوبتجي العمليات",
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldiers = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Where(s => s.IsActive)
            .ToListAsync();

        int count = 0;
        foreach (var soldier in soldiers)
        {
            var cycle = soldier.CurrentCycle;
            if (cycle != null && !cycle.ActualLeaveDate.HasValue && today >= cycle.LeaveStartDate.AddDays(-1))
            {
                // إتمام الدورة الحالية
                cycle.ActualLeaveDate = cycle.LeaveStartDate;
                cycle.ActualReturnDate = cycle.ExpectedReturnDate;
                cycle.ClauseExecutedBy = dispatchedBy;
                cycle.ClauseExecutedAt = DateTime.UtcNow;
                cycle.ReturnedBy = dispatchedBy;
                cycle.ReturnedAt = DateTime.UtcNow;
                cycle.IsCompleted = true;

                // ترحيل الدورة القادمة
                var newReturnDate = cycle.ExpectedReturnDate;
                var nextLeave = _calcService.CalculateLeaveStartDate(newReturnDate, soldier.SoldierType.DutyDays, 0);
                var nextReturn = _calcService.CalculateExpectedReturnDate(nextLeave, soldier.SoldierType.LeaveDays, 0);

                var nextCycle = new LeaveCycle
                {
                    SoldierId = soldier.Id,
                    SoldierTypeId = soldier.SoldierTypeId,
                    CycleNumber = cycle.CycleNumber + 1,
                    LastReturnDate = newReturnDate,
                    LeaveStartDate = nextLeave,
                    ExpectedReturnDate = nextReturn,
                    DutyDaysSnapshot = soldier.SoldierType.DutyDays,
                    LeaveDaysSnapshot = soldier.SoldierType.LeaveDays,
                    DeductionDays = 0,
                    BonusDays = 0,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LeaveCycles.Add(nextCycle);
                count++;
            }
        }

        if (count > 0)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "LeaveCycle",
                ActionType = "BatchDispatch",
                PerformedBy = dispatchedBy,
                PerformedAt = DateTime.UtcNow,
                Details = $"تم تنفيذ النزول الجماعي وترحيل الدورات لعدد {count} موظف"
            });
            await _context.SaveChangesAsync();
        }

        return count;
    }

    public async Task<RosterItemViewModel?> UpdateModifiersAsync(
        int soldierId,
        int deductionDays,
        int bonusDays,
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Include(s => s.ModifierRequests)
            .FirstOrDefaultAsync(s => s.Id == soldierId);

        if (soldier == null || soldier.CurrentCycle == null) return null;

        var cycle = soldier.CurrentCycle;
        cycle.DeductionDays = Math.Max(0, deductionDays);
        cycle.BonusDays = Math.Max(0, bonusDays);

        cycle.LeaveStartDate = _calcService.CalculateLeaveStartDate(
            cycle.LastReturnDate,
            soldier.SoldierType.DutyDays,
            cycle.DeductionDays);

        cycle.ExpectedReturnDate = _calcService.CalculateExpectedReturnDate(
            cycle.LeaveStartDate,
            soldier.SoldierType.LeaveDays,
            cycle.BonusDays);

        await _context.SaveChangesAsync();

        return MapToViewModel(soldier, today);
    }

    public async Task<RosterItemViewModel?> UpdateReturnDateAsync(
        int soldierId,
        DateOnly newReturnDate,
        DateOnly? overrideToday = null)
    {
        var today = GetEffectiveToday(overrideToday);
        var soldier = await _context.Soldiers
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .Include(s => s.ModifierRequests)
            .FirstOrDefaultAsync(s => s.Id == soldierId);

        if (soldier == null || soldier.CurrentCycle == null) return null;

        var cycle = soldier.CurrentCycle;
        cycle.LastReturnDate = newReturnDate;

        cycle.LeaveStartDate = _calcService.CalculateLeaveStartDate(
            newReturnDate,
            soldier.SoldierType.DutyDays,
            cycle.DeductionDays);

        cycle.ExpectedReturnDate = _calcService.CalculateExpectedReturnDate(
            cycle.LeaveStartDate,
            soldier.SoldierType.LeaveDays,
            cycle.BonusDays);

        await _context.SaveChangesAsync();

        return MapToViewModel(soldier, today);
    }

    public async Task<Soldier> CreateSoldierAsync(CreateSoldierDto dto)
    {
        var cleanNumber = dto.MilitaryNumber?.Trim();
        if (string.IsNullOrWhiteSpace(cleanNumber))
        {
            throw new InvalidOperationException("رقم الموظف مطلوب ولا يمكن تركه فارغاً.");
        }

        var cleanName = dto.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            throw new InvalidOperationException("اسم الموظف مطلوب ولا يمكن تركه فارغاً.");
        }

        var exists = await _context.Soldiers.AnyAsync(s => s.MilitaryNumber == cleanNumber);
        if (exists)
        {
            throw new InvalidOperationException($"رقم الموظف ({cleanNumber}) مسجل بالفعل لموظف آخر في المنظومة، يرجى إدخال رقم مختلف.");
        }

        var soldierType = await _context.SoldierTypes.FindAsync(dto.SoldierTypeId)
            ?? throw new InvalidOperationException("نوع أو تصنيف الموظف غير موجود في المنظومة.");

        var leaveStart = _calcService.CalculateLeaveStartDate(dto.InitialReturnDate, soldierType.DutyDays, 0);
        var expectedReturn = _calcService.CalculateExpectedReturnDate(leaveStart, soldierType.LeaveDays, 0);

        var soldier = new Soldier
        {
            FullName = cleanName,
            MilitaryNumber = cleanNumber,
            SoldierTypeId = dto.SoldierTypeId,
            JoinDate = dto.InitialReturnDate,
            ServiceEndDate = dto.ServiceEndDate,
            IsActive = true,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            LeaveCycles = new List<LeaveCycle>
            {
                new()
                {
                    CycleNumber = 1,
                    SoldierTypeId = dto.SoldierTypeId,
                    LastReturnDate = dto.InitialReturnDate,
                    LeaveStartDate = leaveStart,
                    ExpectedReturnDate = expectedReturn,
                    DutyDaysSnapshot = soldierType.DutyDays,
                    LeaveDaysSnapshot = soldierType.LeaveDays,
                    DeductionDays = 0,
                    BonusDays = 0,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        _context.Soldiers.Add(soldier);
        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "Soldier",
            ActionType = "CreateSoldier",
            PerformedBy = "المشرف",
            PerformedAt = DateTime.UtcNow,
            Details = $"إضافة موظف جديد: {soldier.FullName} ({soldier.MilitaryNumber})"
        });

        await _context.SaveChangesAsync();
        return soldier;
    }

    public async Task UpdateSoldierAsync(int soldierId, string fullName, string militaryNumber, int soldierTypeId, DateOnly serviceEndDate, string? notes = null)
    {
        var cleanNumber = militaryNumber?.Trim();
        if (string.IsNullOrWhiteSpace(cleanNumber))
        {
            throw new InvalidOperationException("رقم الموظف مطلوب ولا يمكن تركه فارغاً.");
        }

        var cleanName = fullName?.Trim();
        if (string.IsNullOrWhiteSpace(cleanName))
        {
            throw new InvalidOperationException("اسم الموظف مطلوب ولا يمكن تركه فارغاً.");
        }

        var duplicate = await _context.Soldiers.AnyAsync(s => s.Id != soldierId && s.MilitaryNumber == cleanNumber);
        if (duplicate)
        {
            throw new InvalidOperationException($"رقم الموظف ({cleanNumber}) مسجل بالفعل لموظف آخر في المنظومة، يرجى اختيار رقم آخر.");
        }

        var soldier = await _context.Soldiers.FindAsync(soldierId)
            ?? throw new InvalidOperationException("الموظف المراد تعديل بياناته غير موجود.");

        soldier.FullName = cleanName;
        soldier.MilitaryNumber = cleanNumber;
        soldier.SoldierTypeId = soldierTypeId;
        soldier.ServiceEndDate = serviceEndDate;
        soldier.Notes = notes;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSoldierAsync(int soldierId)
    {
        var soldier = await _context.Soldiers.FindAsync(soldierId);
        if (soldier != null)
        {
            _context.Soldiers.Remove(soldier);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PagedList<Soldier>> GetPagedPersonnelAsync(string? search, int? typeId, int page = 1, int pageSize = 10)
    {
        var query = _context.Soldiers
            .AsNoTracking()
            .Include(s => s.SoldierType)
            .Include(s => s.LeaveCycles)
            .AsQueryable();

        if (typeId.HasValue)
        {
            query = query.Where(s => s.SoldierTypeId == typeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var clean = search.Trim();
            query = query.Where(s => s.FullName.Contains(clean) || s.MilitaryNumber.Contains(clean));
        }

        int totalCount = await query.CountAsync();
        int currentPage = Math.Max(1, page);
        int validPageSize = Math.Max(1, pageSize);

        var soldiers = await query
            .OrderBy(s => s.FullName)
            .Skip((currentPage - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync();

        return new PagedList<Soldier>
        {
            Items = soldiers,
            TotalCount = totalCount,
            PageNumber = currentPage,
            PageSize = validPageSize
        };
    }

    public async Task<List<LeaveCycle>> GetSoldierCyclesAsync(int soldierId)
    {
        return await _context.LeaveCycles
            .AsNoTracking()
            .Include(c => c.Soldier)
            .Include(c => c.SoldierType)
            .Where(c => c.SoldierId == soldierId)
            .OrderByDescending(c => c.CycleNumber)
            .ToListAsync();
    }

    private RosterItemViewModel? MapToViewModel(Soldier soldier, DateOnly today)
    {
        var cycle = soldier.CurrentCycle;
        if (cycle == null) return null;

        var type = soldier.SoldierType;
        var eval = _calcService.EvaluateStatus(cycle, type, today);
        bool isDemobSoon = (soldier.ServiceEndDate.DayNumber - today.DayNumber) <= 60;

        var pendingMod = soldier.ModifierRequests?.FirstOrDefault(m => m.Status == NoteStatus.Pending);

        return new RosterItemViewModel
        {
            SoldierId = soldier.Id,
            CycleId = cycle.Id,
            CycleNumber = cycle.CycleNumber,
            FullName = soldier.FullName,
            MilitaryNumber = soldier.MilitaryNumber,
            SoldierTypeId = soldier.SoldierTypeId,
            SoldierTypeName = type.Name,
            DutyDays = type.DutyDays,
            LeaveDays = type.LeaveDays,
            LastReturnDate = cycle.LastReturnDate,
            LeaveStartDate = cycle.LeaveStartDate,
            ExpectedReturnDate = cycle.ExpectedReturnDate,
            ActualLeaveDate = cycle.ActualLeaveDate,
            ActualReturnDate = cycle.ActualReturnDate,
            DeductionDays = cycle.DeductionDays,
            BonusDays = cycle.BonusDays,
            Status = eval.Status,
            StatusName = eval.StatusDisplayName,
            StatusDescription = eval.StatusDescription,
            CurrentDutyDayNumber = eval.CurrentDutyDayNumber,
            RemainingDays = eval.RemainingDaysToLeave,
            ServiceEndDate = soldier.ServiceEndDate,
            IsDemobSoon = isDemobSoon,
            HasPendingModifierRequest = pendingMod != null,
            PendingModifierSummary = pendingMod != null
                ? $"طلب {(pendingMod.RequestType == "Bonus" ? "منحة" : "خصم")} ({pendingMod.Days} يوم) قيد الاعتماد"
                : null,
            PendingModifierId = pendingMod?.Id
        };
    }

    private static int GetStatusSortOrder(SoldierDutyStatus status) => status switch
    {
        SoldierDutyStatus.OverdueReturn => 0,     // الأعلى أولوية: المتأخر عن العودة
        SoldierDutyStatus.EligibleForLeave => 1,  // مستحق النزول غداً
        SoldierDutyStatus.LeaveToday => 2,        // نزول اليوم
        SoldierDutyStatus.ReturnToday => 3,       // عودة اليوم
        SoldierDutyStatus.OnLeave => 4,           // في إجازة
        SoldierDutyStatus.OnDuty => 5,            // في الخدمة
        _ => 6
    };

    public async Task<EmployeeNote> AddEmployeeNoteAsync(int soldierId, string noteText, string authorUsername, string authorFullName, string authorRole, bool isAdmin)
    {
        var cleanText = noteText?.Trim();
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            throw new InvalidOperationException("نص الملاحظة مطلوب ولا يمكن تركه فارغاً.");
        }

        var soldierExists = await _context.Soldiers.AnyAsync(s => s.Id == soldierId);
        if (!soldierExists)
        {
            throw new InvalidOperationException("الموظف المحدد غير موجود.");
        }

        var note = new EmployeeNote
        {
            SoldierId = soldierId,
            NoteText = cleanText,
            AuthorUsername = authorUsername,
            AuthorFullName = authorFullName,
            AuthorRole = authorRole,
            CreatedAt = DateTime.UtcNow,
            Status = isAdmin ? NoteStatus.Approved : NoteStatus.Pending,
            ApprovedBy = isAdmin ? authorFullName : null,
            ReviewedAt = isAdmin ? DateTime.UtcNow : null
        };

        _context.EmployeeNotes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<List<EmployeeNote>> GetPendingNotesAsync()
    {
        return await _context.EmployeeNotes
            .Include(n => n.Soldier)
            .Where(n => n.Status == NoteStatus.Pending)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<EmployeeNote>> GetSoldierNotesAsync(int soldierId, bool approvedOnly = false)
    {
        var query = _context.EmployeeNotes
            .Where(n => n.SoldierId == soldierId);

        if (approvedOnly)
        {
            query = query.Where(n => n.Status == NoteStatus.Approved);
        }

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<bool> ApproveNoteAsync(int noteId, string approvedBy)
    {
        var note = await _context.EmployeeNotes.FindAsync(noteId);
        if (note == null) return false;

        note.Status = NoteStatus.Approved;
        note.ApprovedBy = approvedBy;
        note.ReviewedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectNoteAsync(int noteId, string rejectedBy, string? comment = null)
    {
        var note = await _context.EmployeeNotes.FindAsync(noteId);
        if (note == null) return false;

        note.Status = NoteStatus.Rejected;
        note.ApprovedBy = rejectedBy;
        note.ReviewedAt = DateTime.UtcNow;
        note.AdminComment = comment;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Soldier?> GetSoldierByUserIdAsync(int userId)
    {
        var user = await _context.AppUsers
            .Include(u => u.Soldier)
                .ThenInclude(s => s!.SoldierType)
            .Include(u => u.Soldier)
                .ThenInclude(s => s!.LeaveCycles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Soldier != null)
        {
            return user.Soldier;
        }

        if (user != null)
        {
            return await _context.Soldiers
                .Include(s => s.SoldierType)
                .Include(s => s.LeaveCycles)
                .FirstOrDefaultAsync(s => s.MilitaryNumber == user.Username || s.FullName == user.FullName);
        }

        return null;
    }

    public async Task<ModifierRequest> CreateModifierRequestAsync(
        int soldierId,
        string requestType,
        int days,
        string? reason,
        string requestedByUsername,
        string requestedByFullName)
    {
        if (days <= 0)
        {
            throw new InvalidOperationException("عدد الأيام يجب أن يكون يوماً واحداً على الأقل.");
        }

        var cleanType = requestType?.Trim() == "Deduction" ? "Deduction" : "Bonus";

        var soldier = await _context.Soldiers
            .Include(s => s.LeaveCycles)
            .FirstOrDefaultAsync(s => s.Id == soldierId)
            ?? throw new InvalidOperationException("الموظف المحدد غير موجود.");

        var currentCycle = soldier.CurrentCycle;

        var hasPending = await _context.ModifierRequests
            .AnyAsync(m => m.SoldierId == soldierId && m.Status == NoteStatus.Pending);

        if (hasPending)
        {
            throw new InvalidOperationException("يوجد بالفعل طلب خصم/منحة معلق لهذا الموظف بانتظار اعتماد الإدارة.");
        }

        var request = new ModifierRequest
        {
            SoldierId = soldierId,
            LeaveCycleId = currentCycle?.Id,
            RequestType = cleanType,
            Days = days,
            Reason = reason?.Trim(),
            RequestedByUsername = requestedByUsername,
            RequestedByFullName = requestedByFullName,
            RequestedAt = DateTime.UtcNow,
            Status = NoteStatus.Pending
        };

        _context.ModifierRequests.Add(request);

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ModifierRequest",
            ActionType = "CreateRequest",
            PerformedBy = requestedByFullName,
            PerformedAt = DateTime.UtcNow,
            Details = $"طلب {(cleanType == "Bonus" ? "منحة" : "خصم")} ({days} يوم) للموظف: {soldier.FullName} بواسطة {requestedByFullName}"
        });

        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<List<ModifierRequest>> GetPendingModifierRequestsAsync()
    {
        return await _context.ModifierRequests
            .AsNoTracking()
            .Include(m => m.Soldier)
                .ThenInclude(s => s.SoldierType)
            .Include(m => m.LeaveCycle)
            .Where(m => m.Status == NoteStatus.Pending)
            .OrderByDescending(m => m.RequestedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveModifierRequestAsync(int requestId, string approvedBy)
    {
        var request = await _context.ModifierRequests
            .Include(m => m.Soldier)
                .ThenInclude(s => s.SoldierType)
            .Include(m => m.Soldier)
                .ThenInclude(s => s.LeaveCycles)
            .FirstOrDefaultAsync(m => m.Id == requestId);

        if (request == null || request.Status != NoteStatus.Pending)
        {
            return false;
        }

        request.Status = NoteStatus.Approved;
        request.ReviewedBy = approvedBy;
        request.ReviewedAt = DateTime.UtcNow;

        var soldier = request.Soldier;
        var cycle = soldier.CurrentCycle;

        if (cycle != null)
        {
            if (request.RequestType == "Bonus")
            {
                cycle.BonusDays += request.Days;
            }
            else
            {
                cycle.DeductionDays += request.Days;
            }

            cycle.LeaveStartDate = _calcService.CalculateLeaveStartDate(
                cycle.LastReturnDate,
                soldier.SoldierType.DutyDays,
                cycle.DeductionDays);

            cycle.ExpectedReturnDate = _calcService.CalculateExpectedReturnDate(
                cycle.LeaveStartDate,
                soldier.SoldierType.LeaveDays,
                cycle.BonusDays);
        }

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ModifierRequest",
            ActionType = "ApproveRequest",
            PerformedBy = approvedBy,
            PerformedAt = DateTime.UtcNow,
            Details = $"اعتماد وتطبيق طلب {(request.RequestType == "Bonus" ? "منحة" : "خصم")} ({request.Days} يوم) للموظف {soldier.FullName}"
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectModifierRequestAsync(int requestId, string rejectedBy, string? comment = null)
    {
        var request = await _context.ModifierRequests
            .Include(m => m.Soldier)
            .FirstOrDefaultAsync(m => m.Id == requestId);

        if (request == null || request.Status != NoteStatus.Pending)
        {
            return false;
        }

        request.Status = NoteStatus.Rejected;
        request.ReviewedBy = rejectedBy;
        request.ReviewedAt = DateTime.UtcNow;
        request.AdminComment = comment?.Trim();

        _context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ModifierRequest",
            ActionType = "RejectRequest",
            PerformedBy = rejectedBy,
            PerformedAt = DateTime.UtcNow,
            Details = $"رفض طلب {(request.RequestType == "Bonus" ? "منحة" : "خصم")} للموظف {request.Soldier?.FullName}. السبب: {comment}"
        });

        await _context.SaveChangesAsync();
        return true;
    }
}
