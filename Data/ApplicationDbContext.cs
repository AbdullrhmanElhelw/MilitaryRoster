using Microsoft.EntityFrameworkCore;
using MilitaryRoster.Models.Entities;

namespace MilitaryRoster.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SoldierType> SoldierTypes => Set<SoldierType>();
    public DbSet<Soldier> Soldiers => Set<Soldier>();
    public DbSet<LeaveCycle> LeaveCycles => Set<LeaveCycle>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmployeeNote> EmployeeNotes => Set<EmployeeNote>();
    public DbSet<ModifierRequest> ModifierRequests => Set<ModifierRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SoldierType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.Soldiers)
                  .WithOne(s => s.SoldierType)
                  .HasForeignKey(s => s.SoldierTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Soldier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.MilitaryNumber).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.MilitaryNumber).IsUnique();

            entity.HasMany(e => e.LeaveCycles)
                  .WithOne(c => c.Soldier)
                  .HasForeignKey(c => c.SoldierId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.EmployeeNotes)
                  .WithOne(n => n.Soldier)
                  .HasForeignKey(n => n.SoldierId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ModifierRequests)
                  .WithOne(m => m.Soldier)
                  .HasForeignKey(m => m.SoldierId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeaveCycle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SoldierId, e.CycleNumber });
            entity.HasIndex(e => e.LeaveStartDate);
            entity.HasIndex(e => e.ExpectedReturnDate);

            entity.HasOne(e => e.SoldierType)
                  .WithMany()
                  .HasForeignKey(e => e.SoldierTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasOne(e => e.Soldier)
                  .WithMany()
                  .HasForeignKey(e => e.SoldierId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<EmployeeNote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SoldierId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PerformedAt);
        });

        modelBuilder.Entity<ModifierRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SoldierId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);

            entity.HasOne(e => e.LeaveCycle)
                  .WithMany()
                  .HasForeignKey(e => e.LeaveCycleId)
                  .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
