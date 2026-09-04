namespace MilitaryRoster.Helpers;

/// <summary>
/// مساعد تنسيق التواريخ بالصيغة العربية
/// </summary>
public static class DateHelper
{
    private static readonly string[] ArabicMonths =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    };

    /// <summary>
    /// تنسيق DateOnly بالصيغة العربية: "4 سبتمبر 2026"
    /// </summary>
    public static string ToArabic(this DateOnly date)
        => $"{date.Day} {ArabicMonths[date.Month - 1]} {date.Year}";

    /// <summary>
    /// تنسيق DateTime بالصيغة العربية: "4 سبتمبر 2026"
    /// </summary>
    public static string ToArabic(this DateTime date)
        => $"{date.Day} {ArabicMonths[date.Month - 1]} {date.Year}";

    /// <summary>
    /// تنسيق nullable DateOnly بالصيغة العربية، أو نص بديل لو null
    /// </summary>
    public static string ToArabic(this DateOnly? date, string fallback = "-")
        => date.HasValue ? date.Value.ToArabic() : fallback;

    /// <summary>
    /// تنسيق nullable DateTime بالصيغة العربية، أو نص بديل لو null
    /// </summary>
    public static string ToArabic(this DateTime? date, string fallback = "-")
        => date.HasValue ? date.Value.ToArabic() : fallback;

    /// <summary>
    /// اسم الشهر العربي فقط
    /// </summary>
    public static string ArabicMonthName(int month)
        => ArabicMonths[month - 1];
}
