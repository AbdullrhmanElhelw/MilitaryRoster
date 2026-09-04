using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.Models.Entities;

public class SoldierType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// عدد أيام التواجد المطلوبة بالخدمة (افتراضي 10)
    /// </summary>
    [Range(1, 60)]
    public int DutyDays { get; set; } = 10;

    /// <summary>
    /// عدد أيام الإجازة المقررة (8 للمدير، 7 للسكرتارية)
    /// </summary>
    [Range(1, 30)]
    public int LeaveDays { get; set; } = 7;

    [MaxLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Soldier> Soldiers { get; set; } = new List<Soldier>();
}
