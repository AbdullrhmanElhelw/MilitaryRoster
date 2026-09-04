using MilitaryRoster.Models.Entities;

namespace MilitaryRoster.ViewModels;

public class SettingsViewModel
{
    // سجل الحركات والتدقيق
    public PagedList<AuditLog> Logs { get; set; } = new();

    // بطاقات المؤشرات الإحصائية (KPIs)
    public int TotalLogsCount { get; set; }
    public int TodayLogsCount { get; set; }
    public int UniqueActorsCount { get; set; }
    public DateTime? LatestActionTime { get; set; }

    // خيارات الفلترة الحالية
    public string? Search { get; set; }
    public string? SelectedActionType { get; set; }
    public string? SelectedEntity { get; set; }
    public string? SelectedActor { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string ActiveTab { get; set; } = "logs"; // "logs" or "database"

    // القوائم المنسدلة للفلاتر
    public List<string> AvailableActionTypes { get; set; } = new();
    public List<string> AvailableEntities { get; set; } = new();
    public List<string> AvailableActors { get; set; } = new();

    // إحصائيات ومعلومات النظام وقاعدة البيانات
    public int SoldiersCount { get; set; }
    public int ActiveSoldiersCount { get; set; }
    public int LeaveCyclesCount { get; set; }
    public int NotesCount { get; set; }
    public int ModifierRequestsCount { get; set; }
    public int UsersCount { get; set; }
    public string DatabaseServer { get; set; } = "db66536.public.databaseasp.net";
    public string DatabaseName { get; set; } = "db66536";
    public string DatabaseProvider { get; set; } = "Microsoft SQL Server";
    public bool IsDatabaseConnected { get; set; } = true;
    public string SystemVersion { get; set; } = "v2.5 (نسخة التشغيل الرسمية)";
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}
