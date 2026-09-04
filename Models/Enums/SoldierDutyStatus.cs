namespace MilitaryRoster.Models.Enums;

public enum SoldierDutyStatus
{
    OnDuty = 0,              // متواجد بالخدمة (في فترة الـ 10 أيام)
    EligibleForLeave = 1,    // مستحق النزول غداً (عمل البند) - Today = LeaveStartDate - 1
    LeaveToday = 2,          // موعد النزول اليوم - Today = LeaveStartDate
    OnLeave = 3,             // حالياً في فترة الإجازة
    ReturnToday = 4,         // موعد العودة اليوم (استلام - لا يُحسب تواجد)
    OverdueReturn = 5        // متأخر عن العودة - Today > ExpectedReturnDate ولم تسجل عودته
}

public enum UserRole
{
    Admin,
    Manager,
    Employee,
    Viewer
}
