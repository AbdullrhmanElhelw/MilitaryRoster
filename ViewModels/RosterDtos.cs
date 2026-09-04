using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.ViewModels;

public class UpdateModifiersDto
{
    [Range(0, 30)]
    public int DeductionDays { get; set; }

    [Range(0, 30)]
    public int BonusDays { get; set; }
}

public class UpdateReturnDateDto
{
    [Required]
    public DateOnly LastReturnDate { get; set; }
}

public class CreateSoldierDto
{
    [Required(ErrorMessage = "الاسم بالكامل مطلوب")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "رقم الموظف مطلوب")]
    [MaxLength(30)]
    public string MilitaryNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "نوع العسكري مطلوب")]
    public int SoldierTypeId { get; set; }

    [Required(ErrorMessage = "تاريخ إنهاء الخدمة مطلوب")]
    public DateOnly ServiceEndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(1));

    [Required(ErrorMessage = "تاريخ آخر عودة مطلوب")]
    public DateOnly InitialReturnDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));

    public string? Notes { get; set; }
}
