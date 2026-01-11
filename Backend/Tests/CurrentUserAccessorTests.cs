using System.Security.Claims;
using Backend.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests;

public class CurrentUserAccessorTests
{
    private static HttpContext CreateContext(bool authenticated, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticated ? "test" : null);
        var principal = new ClaimsPrincipal(identity);
        return new DefaultHttpContext { User = principal };
    }

    [Fact]
    public void GetCurrentUser_should_return_null_when_not_authenticated()
    {
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = CreateContext(false) });

        accessor.GetCurrentUser().Should().BeNull();
    }

    [Fact]
    public void GetCurrentUser_should_return_null_when_missing_required_claims()
    {
        var claims = new[]
        {
            new Claim("firstName", "Test"),
            new Claim("lastName", "User")
        };
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = CreateContext(true, claims) });

        accessor.GetCurrentUser().Should().BeNull();
    }

    [Fact]
    public void GetCurrentUser_should_map_claims_when_present()
    {
        var id = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Email, "user@test.com"),
            new Claim("firstName", "First"),
            new Claim("lastName", "Last"),
            new Claim("rank", "2"),
            new Claim("roleId", roleId.ToString()),
            new Claim("roleName", "Leader")
        };
        var accessor = new HttpContextCurrentUserAccessor(new HttpContextAccessor { HttpContext = CreateContext(true, claims) });

        var user = accessor.GetCurrentUser();

        user.Should().NotBeNull();
        user!.Id.Should().Be(id);
        user.Email.Should().Be("user@test.com");
        user.FirstName.Should().Be("First");
        user.LastName.Should().Be("Last");
        user.Rank.Should().Be(2);
        user.RoleId.Should().Be(roleId);
        user.RoleName.Should().Be("Leader");
    }
}

