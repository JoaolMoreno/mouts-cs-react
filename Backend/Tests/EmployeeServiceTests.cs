using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests;

public class EmployeeServiceTests
{
    private static DbContextOptions<AppDbContext> InMemoryOptions() => new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private static AppDbContext CreateDb() => new AppDbContext(InMemoryOptions());

    private static AuthenticatedUserContext DirectorContext() => new(Guid.NewGuid(), "director@test.com", "Dir", "Boss", 1, Guid.NewGuid(), "Director");
    private static AuthenticatedUserContext LeaderContext(Guid id, Guid roleId) => new(id, "leader@test.com", "Lead", "Boss", 2, roleId, "Leader");

    [Fact]
    public async Task CreateAsync_should_fail_when_age_under_18()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());

        var req = new EmployeeCreateRequest("John", "Doe", "john@test.com", "123", role.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)), "secret123");

        await FluentActions.Invoking(() => sut.CreateAsync(req, DirectorContext())).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_should_fail_when_role_rank_not_lower()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        db.Roles.Add(leaderRole);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());

        var leader = LeaderContext(Guid.NewGuid(), leaderRole.Id);
        var req = new EmployeeCreateRequest("John", "Doe", "john@test.com", "123", leaderRole.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "secret123");

        await FluentActions.Invoking(() => sut.CreateAsync(req, leader)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_should_fail_when_email_or_document_in_use()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);

        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            Email = "john@test.com",
            Document = "123",
            RoleId = role.Id,
            ManagerId = DirectorContext().Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var req = new EmployeeCreateRequest("John", "Doe", " john@test.com ", " 123 ", role.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "secret123");

        await FluentActions.Invoking(() => sut.CreateAsync(req, DirectorContext())).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAsync_non_director_should_filter_by_manager()
    {
        using var db = CreateDb();
        var directorRole = new Role { Id = Guid.NewGuid(), Name = "Diretor", Rank = 1 };
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        db.Roles.AddRange(directorRole, leaderRole);

        var managerId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Sub",
            LastName = "One",
            Email = "sub1@test.com",
            Document = "doc1",
            RoleId = leaderRole.Id,
            ManagerId = managerId,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = leaderRole
        });
        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Sub",
            LastName = "Two",
            Email = "sub2@test.com",
            Document = "doc2",
            RoleId = leaderRole.Id,
            ManagerId = Guid.NewGuid(),
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = leaderRole
        });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = LeaderContext(managerId, leaderRole.Id);

        var list = await sut.GetAsync(current);

        list.Should().HaveCount(1);
        list.Single().ManagerId.Should().Be(managerId);
    }

    [Fact]
    public async Task UpdateAsync_should_apply_changes_and_hash_password()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Old",
            LastName = "Name",
            Email = "old@test.com",
            Document = "doc-old",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = new BCryptPasswordHasher().Hash("oldpass"),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            Role = role
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();
        var previousHash = employee.PasswordHash;

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var update = new EmployeeUpdateRequest("  New ", " Name  ", "NEW@TEST.COM  ", " doc-new  ", null, null, "newpass");

        var response = await sut.UpdateAsync(employee.Id, update, current);

        response.Should().NotBeNull();
        var updated = await db.Employees.FirstAsync(e => e.Id == employee.Id);
        updated.FirstName.Should().Be("New");
        updated.LastName.Should().Be("Name");
        updated.Email.Should().Be("new@test.com");
        updated.Document.Should().Be("doc-new");
        updated.PasswordHash.Should().NotBe("newpass");
        updated.PasswordHash.Should().NotBe(previousHash);
        updated.UpdatedAtUtc.Should().BeOnOrAfter(employee.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_setting_higher_or_equal_rank()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(leaderRole, staffRole);
        await db.SaveChangesAsync();

        var current = LeaderContext(Guid.NewGuid(), leaderRole.Id);
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Staff",
            LastName = "Member",
            Email = "staff@test.com",
            Document = "doc-staff",
            RoleId = staffRole.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var update = new EmployeeUpdateRequest(null, null, null, null, leaderRole.Id, null, null);

        await FluentActions.Invoking(() => sut.UpdateAsync(employee.Id, update, current)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_age_under_18()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Young",
            LastName = "One",
            Email = "young@test.com",
            Document = "doc-young",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(2000, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var update = new EmployeeUpdateRequest(null, null, null, null, null, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)), null);

        await FluentActions.Invoking(() => sut.UpdateAsync(employee.Id, update, current)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_should_respect_scope()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(leaderRole, staffRole);
        await db.SaveChangesAsync();

        var current = LeaderContext(Guid.NewGuid(), leaderRole.Id);
        var managed = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Managed",
            LastName = "User",
            Email = "managed@test.com",
            Document = "doc-managed",
            RoleId = staffRole.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        };
        var other = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "User",
            Email = "other@test.com",
            Document = "doc-other",
            RoleId = staffRole.Id,
            ManagerId = Guid.NewGuid(),
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        };
        db.Employees.AddRange(managed, other);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());

        (await sut.DeleteAsync(managed.Id, current)).Should().BeTrue();
        (await sut.DeleteAsync(other.Id, current)).Should().BeFalse();
    }
}
