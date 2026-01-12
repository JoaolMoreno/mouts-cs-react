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
    public async Task CreateAsync_should_create_employee_with_phones()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var phones = new List<PhoneDto>
        {
            new("123456789", "Home", true),
            new("987654321", "Work", false)
        };
        var req = new EmployeeCreateRequest("John", "Doe", "john@test.com", "123", role.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "secret123", phones);

        var response = await sut.CreateAsync(req, DirectorContext());

        response.Should().NotBeNull();
        response.Phones.Should().HaveCount(2);
        response.Phones.Should().Contain(p => p.Number == "123456789" && p.Type == "Home" && p.IsPrimary);
        response.Phones.Should().Contain(p => p.Number == "987654321" && p.Type == "Work" && !p.IsPrimary);

        var created = await db.Employees.Include(e => e.Phones).FirstAsync(e => e.Id == response.Id);
        created.Phones.Should().HaveCount(2);
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

        var list = await sut.GetAsync(new EmployeeListQuery(), current);

        list.Items.Should().HaveCount(1);
        list.Items.Single().ManagerId.Should().Be(managerId);
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
        var update = new EmployeeUpdateRequest("  New ", " Name  ", "NEW@TEST.COM  ", " doc-new  ", null, null, "newpass", null);

        var response = await sut.UpdateAsync(employee.Id, update, current);

        response.Should().NotBeNull();
        var updated = await db.Employees.FirstAsync(e => e.Id == employee.Id);
        updated.FirstName.Should().Be("NEW");
        updated.LastName.Should().Be("NAME");
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
        var update = new EmployeeUpdateRequest(null, null, null, null, leaderRole.Id, null, null, null);

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
        var update = new EmployeeUpdateRequest(null, null, null, null, null, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)), null, null);

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

    [Fact]
    public async Task GetAsync_should_page_filter_and_sort()
    {
        using var db = CreateDb();
        var directorRole = new Role { Id = Guid.NewGuid(), Name = "Diretor", Rank = 1 };
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        db.Roles.AddRange(directorRole, leaderRole);

        var managerId = Guid.NewGuid();
        db.Employees.AddRange(
            new Employee
            {
                Id = managerId,
                FirstName = "Alice",
                LastName = "Manager",
                Email = "alice@test.com",
                Document = "doc-a",
                RoleId = directorRole.Id,
                BirthDate = new DateOnly(1980, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5),
                Role = directorRole
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Bob",
                LastName = "Smith",
                Email = "bob@test.com",
                Document = "doc-b",
                RoleId = leaderRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1990, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-3),
                Role = leaderRole
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Charlie",
                LastName = "Brown",
                Email = "charlie@test.com",
                Document = "doc-c",
                RoleId = leaderRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1992, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                Role = leaderRole
            });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var query = new EmployeeListQuery(Page: 1, PageSize: 1, Search: "b", OrderBy: nameof(Employee.CreatedAtUtc), OrderDirection: "desc");
        var result = await sut.GetAsync(query, current);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(1);
        result.Items.Single().FirstName.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetAsync_should_filter_and_sort_by_role_name()
    {
        using var db = CreateDb();
        var directorRole = new Role { Id = Guid.NewGuid(), Name = "Diretor", Rank = 1 };
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(directorRole, leaderRole, staffRole);

        var managerId = Guid.NewGuid();
        db.Employees.AddRange(
            new Employee
            {
                Id = managerId,
                FirstName = "Alice",
                LastName = "Manager",
                Email = "alice@test.com",
                Document = "doc-a",
                RoleId = directorRole.Id,
                BirthDate = new DateOnly(1980, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-5),
                Role = directorRole
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Bob",
                LastName = "Smith",
                Email = "bob@test.com",
                Document = "doc-b",
                RoleId = leaderRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1990, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-3),
                Role = leaderRole
            },
            new Employee
            {
                Id = Guid.NewGuid(),
                FirstName = "Carol",
                LastName = "Jones",
                Email = "carol@test.com",
                Document = "doc-c",
                RoleId = staffRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1992, 1, 1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
                Role = staffRole
            });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var query = new EmployeeListQuery(Page: 1, PageSize: 5, RoleName: "li", OrderBy: "RoleName", OrderDirection: "desc");
        var result = await sut.GetAsync(query, current);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.Single().RoleName.Should().Be("Lider");
    }

    [Fact]
    public async Task UpdateAsync_should_change_manager_when_valid()
    {
        using var db = CreateDb();
        var directorRole = new Role { Id = Guid.NewGuid(), Name = "Diretor", Rank = 1 };
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(directorRole, leaderRole, staffRole);

        var oldManagerId = Guid.NewGuid();
        var newManagerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        db.Employees.AddRange(
            new Employee
            {
                Id = oldManagerId,
                FirstName = "Old",
                LastName = "Manager",
                Email = "old.manager@test.com",
                Document = "m-old",
                RoleId = leaderRole.Id,
                BirthDate = new DateOnly(1980,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = leaderRole
            },
            new Employee
            {
                Id = newManagerId,
                FirstName = "New",
                LastName = "Manager",
                Email = "new.manager@test.com",
                Document = "m-new",
                RoleId = leaderRole.Id,
                BirthDate = new DateOnly(1982,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = leaderRole
            },
            new Employee
            {
                Id = employeeId,
                FirstName = "Staff",
                LastName = "Member",
                Email = "staff@test.com",
                Document = "doc-staff",
                RoleId = staffRole.Id,
                ManagerId = oldManagerId,
                BirthDate = new DateOnly(1990,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = staffRole
            });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, newManagerId);
        var res = await sut.UpdateAsync(employeeId, update, current);

        res.Should().NotBeNull();
        var updated = await db.Employees.FirstAsync(e => e.Id == employeeId);
        updated.ManagerId.Should().Be(newManagerId);
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_setting_self_as_manager()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(leaderRole, staffRole);

        var employeeId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            FirstName = "Staff",
            LastName = "Self",
            Email = "self@test.com",
            Document = "doc-self",
            RoleId = staffRole.Id,
            ManagerId = Guid.NewGuid(),
            BirthDate = new DateOnly(1990,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, employeeId);
        await FluentActions.Invoking(() => sut.UpdateAsync(employeeId, update, current)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_new_manager_rank_not_higher()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        db.Roles.Add(leaderRole);

        var manager1 = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "M1",
            LastName = "Mgr",
            Email = "m1@test.com",
            Document = "m1",
            RoleId = leaderRole.Id,
            BirthDate = new DateOnly(1980,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = leaderRole
        };
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "LeaderLike",
            LastName = "User",
            Email = "leaduser@test.com",
            Document = "doc-lead",
            RoleId = leaderRole.Id, // same rank as manager1
            ManagerId = manager1.Id,
            BirthDate = new DateOnly(1990,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = leaderRole
        };
        db.Employees.AddRange(manager1, employee);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, manager1.Id);
        await FluentActions.Invoking(() => sut.UpdateAsync(employee.Id, update, current)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_manager_not_found()
    {
        using var db = CreateDb();
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(staffRole);

        var employeeId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            FirstName = "Staff",
            LastName = "NoMgr",
            Email = "nomgr@test.com",
            Document = "doc-nomgr",
            RoleId = staffRole.Id,
            ManagerId = Guid.NewGuid(),
            BirthDate = new DateOnly(1990,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, Guid.NewGuid()); // manager id that doesn't exist
        await FluentActions.Invoking(() => sut.UpdateAsync(employeeId, update, current)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Manager not found");
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_manager_role_missing()
    {
        using var db = CreateDb();
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(staffRole);

        // Manager that references a non-existent role (Role navigation will be null)
        var manager = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Ghost",
            LastName = "Manager",
            Email = "ghost@test.com",
            Document = "mgr-ghost",
            RoleId = Guid.NewGuid(), // no Role with this id is added to db.Roles
            BirthDate = new DateOnly(1980,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = null
        };

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Staff",
            LastName = "Receiver",
            Email = "receiver@test.com",
            Document = "doc-recv",
            RoleId = staffRole.Id,
            ManagerId = Guid.NewGuid(),
            BirthDate = new DateOnly(1990,1,1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = staffRole
        };

        db.Employees.Add(manager);
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, manager.Id);
        await FluentActions.Invoking(() => sut.UpdateAsync(employee.Id, update, current)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Manager not found");
    }

    [Fact]
    public async Task UpdateAsync_should_remove_manager_when_requested()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(leaderRole, staffRole);

        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        db.Employees.AddRange(
            new Employee
            {
                Id = managerId,
                FirstName = "Manager",
                LastName = "One",
                Email = "mgr@test.com",
                Document = "mgr-1",
                RoleId = leaderRole.Id,
                BirthDate = new DateOnly(1980,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = leaderRole
            },
            new Employee
            {
                Id = employeeId,
                FirstName = "Staff",
                LastName = "Member",
                Email = "staff@test.com",
                Document = "doc-staff",
                RoleId = staffRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1990,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = staffRole
            });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, null, true);
        var res = await sut.UpdateAsync(employeeId, update, current);

        res.Should().NotBeNull();
        var updated = await db.Employees.FirstAsync(e => e.Id == employeeId);
        updated.ManagerId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_should_throw_when_remove_and_managerid_provided()
    {
        using var db = CreateDb();
        var leaderRole = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.AddRange(leaderRole, staffRole);

        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        db.Employees.AddRange(
            new Employee
            {
                Id = managerId,
                FirstName = "Manager",
                LastName = "One",
                Email = "mgr@test.com",
                Document = "mgr-1",
                RoleId = leaderRole.Id,
                BirthDate = new DateOnly(1980,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = leaderRole
            },
            new Employee
            {
                Id = employeeId,
                FirstName = "Staff",
                LastName = "Member",
                Email = "staff@test.com",
                Document = "doc-staff",
                RoleId = staffRole.Id,
                ManagerId = managerId,
                BirthDate = new DateOnly(1990,1,1),
                PasswordHash = "hash",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Role = staffRole
            });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var current = DirectorContext();

        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, managerId, true);
        await FluentActions.Invoking(() => sut.UpdateAsync(employeeId, update, current)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Cannot specify ManagerId when RemoveManager is true");
    }

    [Fact]
    public async Task UpdateAsync_should_update_phones()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Document = "123",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee);
        // Add initial phones
        db.Phones.AddRange(
            new Phone { EmployeeId = employee.Id, Number = "111111111", Type = "Old", IsPrimary = true },
            new Phone { EmployeeId = employee.Id, Number = "222222222", Type = "Old2", IsPrimary = false }
        );
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var newPhones = new List<PhoneDto>
        {
            new("333333333", "New", true),
            new("444444444", "New2", false),
            new("555555555", null, false)
        };
        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, null, false, newPhones);

        var response = await sut.UpdateAsync(employee.Id, update, current);

        response.Should().NotBeNull();
        response.Phones.Should().HaveCount(3);
        response.Phones.Should().Contain(p => p.Number == "333333333" && p.Type == "New" && p.IsPrimary);
        response.Phones.Should().Contain(p => p.Number == "444444444" && p.Type == "New2" && !p.IsPrimary);
        response.Phones.Should().Contain(p => p.Number == "555555555" && p.Type == null && !p.IsPrimary);

        var updatedPhones = await db.Phones.Where(p => p.EmployeeId == employee.Id).ToListAsync();
        updatedPhones.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_should_fail_when_phone_number_already_in_use()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        // Create first employee with a phone
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@test.com",
            Document = "doc1",
            RoleId = role.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee1);
        db.Phones.Add(new Phone { EmployeeId = employee1.Id, Number = "123456789", Type = "Home", IsPrimary = true });
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var phones = new List<PhoneDto>
        {
            new("123456789", "Work", false) // Same number
        };
        var req = new EmployeeCreateRequest("John", "Doe", "john@test.com", "doc2", role.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "secret123", phones);

        await FluentActions.Invoking(() => sut.CreateAsync(req, DirectorContext())).Should().ThrowAsync<InvalidOperationException>().WithMessage("Phone number(s) already in use: 123456789");
    }

    [Fact]
    public async Task CreateAsync_should_fail_when_multiple_primary_phones()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var phones = new List<PhoneDto>
        {
            new("123456789", "Home", true),
            new("987654321", "Work", true) // Another primary
        };
        var req = new EmployeeCreateRequest("John", "Doe", "john@test.com", "123", role.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "secret123", phones);

        await FluentActions.Invoking(() => sut.CreateAsync(req, DirectorContext())).Should().ThrowAsync<InvalidOperationException>().WithMessage("Only one phone can be primary");
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_phone_number_already_in_use_by_another_employee()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        // Create first employee with a phone
        var employee1 = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            Email = "existing@test.com",
            Document = "doc1",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee1);
        db.Phones.Add(new Phone { EmployeeId = employee1.Id, Number = "123456789", Type = "Home", IsPrimary = true });
        await db.SaveChangesAsync();

        // Create second employee
        var employee2 = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "Second",
            LastName = "User",
            Email = "second@test.com",
            Document = "doc2",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee2);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var newPhones = new List<PhoneDto>
        {
            new("123456789", "Work", false) // Same number as employee1
        };
        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, null, false, newPhones);

        await FluentActions.Invoking(() => sut.UpdateAsync(employee2.Id, update, current)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Phone number(s) already in use: 123456789");
    }

    [Fact]
    public async Task UpdateAsync_should_fail_when_multiple_primary_phones()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Document = "123",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var newPhones = new List<PhoneDto>
        {
            new("123456789", "Home", true),
            new("987654321", "Work", true) // Another primary
        };
        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, null, false, newPhones);

        await FluentActions.Invoking(() => sut.UpdateAsync(employee.Id, update, current)).Should().ThrowAsync<InvalidOperationException>().WithMessage("Only one phone can be primary");
    }

    [Fact]
    public async Task UpdateAsync_should_succeed_when_reusing_own_phone_numbers()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var current = DirectorContext();
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            Document = "123",
            RoleId = role.Id,
            ManagerId = current.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = "hash",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        };
        db.Employees.Add(employee);
        // Add initial phones
        db.Phones.AddRange(
            new Phone { EmployeeId = employee.Id, Number = "111111111", Type = "Old", IsPrimary = true },
            new Phone { EmployeeId = employee.Id, Number = "222222222", Type = "Old2", IsPrimary = false }
        );
        await db.SaveChangesAsync();

        var sut = new EmployeeService(db, new BCryptPasswordHasher());
        var newPhones = new List<PhoneDto>
        {
            new("111111111", "Updated", true), // Reuse own number
            new("333333333", "New", false)
        };
        var update = new EmployeeUpdateRequest(null, null, null, null, null, null, null, null, false, newPhones);

        var response = await sut.UpdateAsync(employee.Id, update, current);

        response.Should().NotBeNull();
        response.Phones.Should().HaveCount(2);
        response.Phones.Should().Contain(p => p.Number == "111111111" && p.Type == "Updated" && p.IsPrimary);
        response.Phones.Should().Contain(p => p.Number == "333333333" && p.Type == "New" && !p.IsPrimary);
    }

}
