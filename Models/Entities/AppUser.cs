using System.ComponentModel.DataAnnotations;

namespace MilitaryRoster.Models.Entities;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Admin"; // Admin, Supervisor, Employee

    // في حال كان المستخدم موظفاً (Employee) يتم ربطه بملفه الوظيفي
    public int? SoldierId { get; set; }
    public Soldier? Soldier { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
