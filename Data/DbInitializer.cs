using Microsoft.EntityFrameworkCore;
using MilitaryRoster.Models.Entities;
using MilitaryRoster.Services;

namespace MilitaryRoster.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        // 0. ضمان وجود جدول الملاحظات وحقل ربط الموظف
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeNotes')
BEGIN
    CREATE TABLE [EmployeeNotes] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SoldierId] int NOT NULL,
        [NoteText] nvarchar(1000) NOT NULL,
        [AuthorUsername] nvarchar(50) NOT NULL,
        [AuthorFullName] nvarchar(100) NOT NULL,
        [AuthorRole] nvarchar(30) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [ApprovedBy] nvarchar(100) NULL,
        [ReviewedAt] datetime2 NULL,
        [AdminComment] nvarchar(500) NULL,
        CONSTRAINT [FK_EmployeeNotes_Soldiers_SoldierId] FOREIGN KEY ([SoldierId]) REFERENCES [Soldiers] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_EmployeeNotes_SoldierId] ON [EmployeeNotes] ([SoldierId]);
    CREATE INDEX [IX_EmployeeNotes_Status] ON [EmployeeNotes] ([Status]);
    CREATE INDEX [IX_EmployeeNotes_CreatedAt] ON [EmployeeNotes] ([CreatedAt]);
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppUsers') AND name = 'SoldierId')
BEGIN
    ALTER TABLE [AppUsers] ADD [SoldierId] int NULL;
    ALTER TABLE [AppUsers] ADD CONSTRAINT [FK_AppUsers_Soldiers_SoldierId] FOREIGN KEY ([SoldierId]) REFERENCES [Soldiers] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ModifierRequests')
BEGIN
    CREATE TABLE [ModifierRequests] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SoldierId] int NOT NULL,
        [LeaveCycleId] bigint NULL,
        [RequestType] nvarchar(20) NOT NULL,
        [Days] int NOT NULL DEFAULT 1,
        [Reason] nvarchar(500) NULL,
        [RequestedByUsername] nvarchar(50) NOT NULL,
        [RequestedByFullName] nvarchar(100) NOT NULL,
        [RequestedAt] datetime2 NOT NULL,
        [Status] int NOT NULL DEFAULT 0,
        [ReviewedBy] nvarchar(100) NULL,
        [ReviewedAt] datetime2 NULL,
        [AdminComment] nvarchar(500) NULL,
        CONSTRAINT [FK_ModifierRequests_Soldiers_SoldierId] FOREIGN KEY ([SoldierId]) REFERENCES [Soldiers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ModifierRequests_LeaveCycles_LeaveCycleId] FOREIGN KEY ([LeaveCycleId]) REFERENCES [LeaveCycles] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_ModifierRequests_SoldierId] ON [ModifierRequests] ([SoldierId]);
    CREATE INDEX [IX_ModifierRequests_Status] ON [ModifierRequests] ([Status]);
    CREATE INDEX [IX_ModifierRequests_RequestedAt] ON [ModifierRequests] ([RequestedAt]);
END;
");
        }
        catch
        {
            // SQLite fallback or already created
        }

        // 1. زرع وتحديث الحسابات الافتراضية
        if (!await context.AppUsers.AnyAsync(u => u.Username == "admin"))
        {
            context.AppUsers.Add(new AppUser
            {
                Username = "admin",
                PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                FullName = "مدير النظام العام",
                Role = "Admin",
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        if (!await context.AppUsers.AnyAsync(u => u.Username == "supervisor"))
        {
            context.AppUsers.Add(new AppUser
            {
                Username = "supervisor",
                PasswordHash = PasswordHelper.HashPassword("Supervisor@123"),
                FullName = "مشرف الوحدة (صف ضابط)",
                Role = "Supervisor",
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        var firstSoldier = await context.Soldiers.FirstOrDefaultAsync(s => s.IsActive);
        if (firstSoldier != null && !await context.AppUsers.AnyAsync(u => u.Username == "employee"))
        {
            context.AppUsers.Add(new AppUser
            {
                Username = "employee",
                PasswordHash = PasswordHelper.HashPassword("Employee@123"),
                FullName = firstSoldier.FullName,
                Role = "Employee",
                SoldierId = firstSoldier.Id,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        // 2. زرع أنواع الموظفين
        if (!await context.SoldierTypes.AnyAsync())
        {
            var typeManager = new SoldierType
            {
                Name = "متابعة المدير",
                DutyDays = 10,
                LeaveDays = 8,
                Description = "10 أيام تواجد + 8 أيام إجازة",
                IsActive = true
            };

            var typeSecretary = new SoldierType
            {
                Name = "السكرتارية",
                DutyDays = 10,
                LeaveDays = 7,
                Description = "10 أيام تواجد + 7 أيام إجازة",
                IsActive = true
            };

            context.SoldierTypes.AddRange(typeManager, typeSecretary);
            await context.SaveChangesAsync();

            // زرع موظفين أولية
            var today = DateOnly.FromDateTime(DateTime.Today);
            var soldiers = new List<Soldier>
            {
                new Soldier
                {
                    FullName = "سامي السيد أحمد",
                    MilitaryNumber = "2023101",
                    SoldierType = typeManager,
                    JoinDate = today.AddMonths(-6),
                    ServiceEndDate = today.AddMonths(12),
                    IsActive = true,
                    LeaveCycles = new List<LeaveCycle>
                    {
                        new()
                        {
                            CycleNumber = 1,
                            SoldierType = typeManager,
                            LastReturnDate = today.AddDays(-11),
                            LeaveStartDate = today.AddDays(0), // نزول اليوم
                            ExpectedReturnDate = today.AddDays(8),
                            DutyDaysSnapshot = 10,
                            LeaveDaysSnapshot = 8,
                            IsCompleted = false
                        }
                    }
                },
                new Soldier
                {
                    FullName = "حازم محمود إبراهيم",
                    MilitaryNumber = "2023102",
                    SoldierType = typeSecretary,
                    JoinDate = today.AddMonths(-8),
                    ServiceEndDate = today.AddMonths(8),
                    IsActive = true,
                    LeaveCycles = new List<LeaveCycle>
                    {
                        new()
                        {
                            CycleNumber = 1,
                            SoldierType = typeSecretary,
                            LastReturnDate = today.AddDays(-10),
                            LeaveStartDate = today.AddDays(1), // نزول غداً (عمل البند)
                            ExpectedReturnDate = today.AddDays(8),
                            DutyDaysSnapshot = 10,
                            LeaveDaysSnapshot = 7,
                            IsCompleted = false
                        }
                    }
                },
                new Soldier
                {
                    FullName = "علاء محمد حسن",
                    MilitaryNumber = "2023103",
                    SoldierType = typeSecretary,
                    JoinDate = today.AddMonths(-4),
                    ServiceEndDate = today.AddMonths(14),
                    IsActive = true,
                    LeaveCycles = new List<LeaveCycle>
                    {
                        new()
                        {
                            CycleNumber = 1,
                            SoldierType = typeSecretary,
                            LastReturnDate = today.AddDays(-5),
                            LeaveStartDate = today.AddDays(6),
                            ExpectedReturnDate = today.AddDays(13),
                            DutyDaysSnapshot = 10,
                            LeaveDaysSnapshot = 7,
                            IsCompleted = false
                        }
                    }
                },
                new Soldier
                {
                    FullName = "عمرو علي الرويني",
                    MilitaryNumber = "2023104",
                    SoldierType = typeManager,
                    JoinDate = today.AddMonths(-10),
                    ServiceEndDate = today.AddMonths(2), // رديف قريب
                    IsActive = true,
                    LeaveCycles = new List<LeaveCycle>
                    {
                        new()
                        {
                            CycleNumber = 1,
                            SoldierType = typeManager,
                            LastReturnDate = today.AddDays(-15),
                            LeaveStartDate = today.AddDays(-4),
                            ActualLeaveDate = today.AddDays(-4),
                            ExpectedReturnDate = today, // عودة اليوم
                            DutyDaysSnapshot = 10,
                            LeaveDaysSnapshot = 8,
                            IsCompleted = false
                        }
                    }
                },
                new Soldier
                {
                    FullName = "محمود عادل صلاح",
                    MilitaryNumber = "2023105",
                    SoldierType = typeSecretary,
                    JoinDate = today.AddMonths(-7),
                    ServiceEndDate = today.AddMonths(10),
                    IsActive = true,
                    LeaveCycles = new List<LeaveCycle>
                    {
                        new()
                        {
                            CycleNumber = 1,
                            SoldierType = typeSecretary,
                            LastReturnDate = today.AddDays(-18),
                            LeaveStartDate = today.AddDays(-7),
                            ActualLeaveDate = today.AddDays(-7),
                            ExpectedReturnDate = today.AddDays(-1), // متأخر عن العودة بمقدار 1 يوم
                            DutyDaysSnapshot = 10,
                            LeaveDaysSnapshot = 7,
                            IsCompleted = false
                        }
                    }
                }
            };

            context.Soldiers.AddRange(soldiers);
            await context.SaveChangesAsync();
        }
    }
}
