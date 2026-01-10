using Backend.Data;
using Backend.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public interface IAuthService
{
    Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password);

    // Added token helpers to the auth service contract
    string GenerateAccessToken(AuthenticatedUser user);
    string GenerateRefreshToken(AuthenticatedUser user);
}

public class AuthService(AppDbContext db, IPasswordHasher hasher, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await db.Employees.Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Email == normalizedEmail);

        if (user == null)
        {
            return null;
        }

        if (!hasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.Email, user.FirstName, user.LastName, user.Role?.Rank ?? 0, user.RoleId, user.Role?.Name ?? string.Empty);
    }

    public string GenerateAccessToken(AuthenticatedUser user)
    {
        return GenerateJwt(user, TimeSpan.FromMinutes(_jwtOptions.ExpiryMinutes));
    }

    public string GenerateRefreshToken(AuthenticatedUser user)
    {
        return GenerateJwt(user, TimeSpan.FromDays(_jwtOptions.RefreshExpiryDays));
    }

    private string GenerateJwt(AuthenticatedUser user, TimeSpan lifetime)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("rank", user.Rank.ToString()),
            new("roleId", user.RoleId.ToString())
        };

        if (!string.IsNullOrEmpty(user.RoleName))
        {
            claims.Add(new Claim("roleName", user.RoleName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
