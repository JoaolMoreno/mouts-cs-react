using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Backend.Tests;

public class AuthServiceTests
{
    private static DbContextOptions<AppDbContext> InMemoryOptions() => new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private static AppDbContext CreateDb() => new AppDbContext(InMemoryOptions());

    private static JwtOptions TestJwtOptions() => new JwtOptions
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SigningKey = new string('k', 32),
        ExpiryMinutes = 60,
        RefreshExpiryDays = 7
    };

    [Fact]
    public async Task ValidateCredentialsAsync_should_return_null_when_user_not_found_or_password_invalid()
    {
        using var db = CreateDb();
        var sut = new AuthService(db, new BCryptPasswordHasher(), Options.Create(TestJwtOptions()));

        (await sut.ValidateCredentialsAsync("missing@test.com", "pw")).Should().BeNull();

        var role = new Role { Id = Guid.NewGuid(), Name = "Colaborador", Rank = 3 };
        db.Roles.Add(role);
        var hash = new BCryptPasswordHasher().Hash("secret");
        db.Employees.Add(new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = "User",
            LastName = "One",
            Email = "user@test.com",
            Document = "doc",
            RoleId = role.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = hash,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        });
        await db.SaveChangesAsync();

        (await sut.ValidateCredentialsAsync("user@test.com", "wrong")).Should().BeNull();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_should_return_user_when_password_matches()
    {
        using var db = CreateDb();
        var role = new Role { Id = Guid.NewGuid(), Name = "Lider", Rank = 2 };
        db.Roles.Add(role);
        var hash = new BCryptPasswordHasher().Hash("secret");
        var employeeId = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = employeeId,
            FirstName = "User",
            LastName = "One",
            Email = "user@test.com",
            Document = "doc",
            RoleId = role.Id,
            BirthDate = new DateOnly(1990, 1, 1),
            PasswordHash = hash,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Role = role
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db, new BCryptPasswordHasher(), Options.Create(TestJwtOptions()));

        var result = await sut.ValidateCredentialsAsync(" User@test.com ", "secret");

        result.Should().NotBeNull();
        result!.Id.Should().Be(employeeId);
        result.RoleId.Should().Be(role.Id);
        result.RoleName.Should().Be(role.Name);
        result.Rank.Should().Be(role.Rank);
    }

    [Fact]
    public void GenerateAccessToken_should_include_expected_claims()
    {
        var options = Options.Create(TestJwtOptions());
        var sut = new AuthService(CreateDb(), new BCryptPasswordHasher(), options);
        var user = new AuthenticatedUser(Guid.NewGuid(), "user@test.com", "User", "Test", 2, Guid.NewGuid(), "Leader");

        var token = sut.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "firstName" && c.Value == user.FirstName);
        jwt.Claims.Should().Contain(c => c.Type == "lastName" && c.Value == user.LastName);
        jwt.Claims.Should().Contain(c => c.Type == "rank" && c.Value == user.Rank.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "roleId" && c.Value == user.RoleId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "roleName" && c.Value == user.RoleName);
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateRefreshToken_should_use_refresh_lifetime()
    {
        var options = Options.Create(TestJwtOptions());
        var sut = new AuthService(CreateDb(), new BCryptPasswordHasher(), options);
        var user = new AuthenticatedUser(Guid.NewGuid(), "user@test.com", "User", "Test", 2, Guid.NewGuid(), "Leader");

        var access = sut.GenerateAccessToken(user);
        var refresh = sut.GenerateRefreshToken(user);

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.ReadJwtToken(access);
        var refreshToken = handler.ReadJwtToken(refresh);

        refreshToken.ValidTo.Should().BeAfter(accessToken.ValidTo);
    }
}

