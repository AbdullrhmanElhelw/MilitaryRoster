using MilitaryRoster.Models.Entities;
using MilitaryRoster.Models.Enums;
using MilitaryRoster.ViewModels;

namespace MilitaryRoster.Services;

public interface IRosterService
{
    Task<DashboardViewModel> GetDashboardAsync(DateOnly? overrideToday = null);
    Task<RosterDashboardViewModel> GetRosterDashboardAsync(int? soldierTypeId, SoldierDutyStatus? status, string? search, int page = 1, int pageSize = 10, DateOnly? overrideToday = null);
    Task<PagedList<RosterItemViewModel>> GetFilteredPagedRosterAsync(int? soldierTypeId, SoldierDutyStatus? status, string? search, int page = 1, int pageSize = 10, DateOnly? overrideToday = null);
    Task<List<RosterItemViewModel>> GetFilteredRosterAsync(int? soldierTypeId, SoldierDutyStatus? status, string? search, DateOnly? overrideToday = null);
    Task<RosterItemViewModel?> GetRosterItemAsync(int soldierId, DateOnly? overrideToday = null);
    Task<SoldierDetailsViewModel?> GetSoldierDetailsAsync(int soldierId, DateOnly? overrideToday = null);
    Task<KpiSummaryViewModel> GetKpiSummaryAsync(DateOnly? overrideToday = null);
    
    // تنفيذ البند
    Task<bool> ExecuteLeaveClauseAsync(LeaveClauseExecutionDto dto, DateOnly? overrideToday = null);
    
    // تسجيل العودة
    Task<bool> RegisterReturnAsync(ReturnRegistrationDto dto, DateOnly? overrideToday = null);
    Task<RosterItemViewModel?> ConfirmReturnAsync(int soldierId, DateOnly? actualReturnDate = null, DateOnly? overrideToday = null);
    
    Task<RosterItemViewModel?> DispatchSoldierAsync(int soldierId, string dispatchedBy = "نوبتجي العمليات", DateOnly? overrideToday = null);
    Task<int> BatchDispatchEligibleAsync(string dispatchedBy = "نوبتجي العمليات", DateOnly? overrideToday = null);
    Task<RosterItemViewModel?> UpdateModifiersAsync(int soldierId, int deductionDays, int bonusDays, DateOnly? overrideToday = null);
    Task<RosterItemViewModel?> UpdateReturnDateAsync(int soldierId, DateOnly newReturnDate, DateOnly? overrideToday = null);
    
    // إدارة الموظفين
    Task<Soldier> CreateSoldierAsync(CreateSoldierDto dto);
    Task UpdateSoldierAsync(int soldierId, string fullName, string militaryNumber, int soldierTypeId, DateOnly serviceEndDate, string? notes = null);
    Task DeleteSoldierAsync(int soldierId);
    Task<List<Soldier>> GetAllPersonnelAsync();
    Task<PagedList<Soldier>> GetPagedPersonnelAsync(string? search, int? typeId, int page = 1, int pageSize = 10);
    Task<List<LeaveCycle>> GetSoldierCyclesAsync(int soldierId);
    Task<List<SoldierType>> GetSoldierTypesAsync();

    // إدارة ملاحظات وتوجيهات الأداء (Notes & Approvals Workflow)
    Task<EmployeeNote> AddEmployeeNoteAsync(int soldierId, string noteText, string authorUsername, string authorFullName, string authorRole, bool isAdmin);
    Task<List<EmployeeNote>> GetPendingNotesAsync();
    Task<List<EmployeeNote>> GetSoldierNotesAsync(int soldierId, bool approvedOnly = false);
    Task<bool> ApproveNoteAsync(int noteId, string approvedBy);
    Task<bool> RejectNoteAsync(int noteId, string rejectedBy, string? comment = null);
    Task<Soldier?> GetSoldierByUserIdAsync(int userId);

    // إدارة طلبات الخصم والمنح (Modifiers Workflow)
    Task<ModifierRequest> CreateModifierRequestAsync(int soldierId, string requestType, int days, string? reason, string requestedByUsername, string requestedByFullName);
    Task<List<ModifierRequest>> GetPendingModifierRequestsAsync();
    Task<bool> ApproveModifierRequestAsync(int requestId, string approvedBy);
    Task<bool> RejectModifierRequestAsync(int requestId, string rejectedBy, string? comment = null);
}
