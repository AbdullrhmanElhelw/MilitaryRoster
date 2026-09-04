using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MilitaryRoster.Models.Entities;

public class Soldier
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string MilitaryNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public int SoldierTypeId { get; set; }
    public SoldierType SoldierType { get; set; } = null!;

    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly ServiceEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LeaveCycle> LeaveCycles { get; set; } = new List<LeaveCycle>();
    public ICollection<EmployeeNote> EmployeeNotes { get; set; } = new List<EmployeeNote>();
    public ICollection<ModifierRequest> ModifierRequests { get; set; } = new List<ModifierRequest>();

    [NotMapped]
    public LeaveCycle? CurrentCycle => LeaveCycles.OrderByDescending(c => c.CycleNumber).FirstOrDefault();
}
