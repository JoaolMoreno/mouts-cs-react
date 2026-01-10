﻿using Backend.Data;
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
}

