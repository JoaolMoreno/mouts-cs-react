using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class SeedData
{
    public static async Task ApplyAsync(AppDbContext db)
    {
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Id = Guid.NewGuid(), Name = "Diretor", Rank = 1 },
                new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 },
                new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 }
            );

            await db.SaveChangesAsync();
        }

        if (!await db.Employees.AnyAsync())
        {
            var directorRole = await db.Roles.FirstAsync(r => r.Rank == 1);
            db.Employees.Add(new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com",
                Document = "00000000000",
                RoleId = directorRole.Id,
                ManagerId = null,
                BirthDate = new DateOnly(1980, 1, 1),
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("Admin@123"),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
