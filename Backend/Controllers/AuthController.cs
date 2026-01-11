using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Backend.Controllers;

/// <summary>
/// Authentication endpoints: login, refresh tokens and logout.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions, AppDbContext db)
    : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    /// <summary>
    /// Authenticate using email and password. Returns tokens as cookies.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await authService.ValidateCredentialsAsync(request.Email, request.Password);
        if (user == null)
        {
            return Unauthorized();
        }

        var accessToken = authService.GenerateAccessToken(user);
        var refreshToken = authService.GenerateRefreshToken(user);

        var refreshEntry = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryDays)
        };
        db.RefreshTokens.Add(refreshEntry);
        await db.SaveChangesAsync();

        AppendCookie(_jwtOptions.CookieName, accessToken, TimeSpan.FromMinutes(_jwtOptions.ExpiryMinutes));
        AppendCookie(_jwtOptions.RefreshCookieName, refreshToken, TimeSpan.FromDays(_jwtOptions.RefreshExpiryDays));

        return Ok(new 
        { 
            message = "Logged in",
            user = new 
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Rank,
                user.RoleName
            }
        });
    }

    /// <summary>
    /// Returns information about the current authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        if (!(User?.Identity?.IsAuthenticated ?? false))
            return Unauthorized();

        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            return Unauthorized();

        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = User.FindFirstValue("firstName") ?? string.Empty;
        var lastName = User.FindFirstValue("lastName") ?? string.Empty;
        var roleName = User.FindFirstValue("roleName") ?? User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var rankStr = User.FindFirstValue("rank") ?? "0";
        int.TryParse(rankStr, out var rank);

        return Ok(new
        {
            id,
            firstName,
            lastName,
            email,
            role = roleName,
            rank
        });
    }

    /// <summary>
    /// Refresh access token using refresh cookie.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(_jwtOptions.RefreshCookieName, out var refreshToken))
        {
            return Unauthorized();
        }

        var refresh = await db.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshToken);
        if (refresh == null || refresh.RevokedAtUtc != null || refresh.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var principal = ValidateJwt(refreshToken);
        if (principal == null)
        {
            return Unauthorized();
        }

        var user = ToAuthenticatedUser(principal);

        if (user.Id != refresh.UserId)
        {
            return Unauthorized();
        }

        var newAccess = authService.GenerateAccessToken(user);
        AppendCookie(_jwtOptions.CookieName, newAccess, TimeSpan.FromMinutes(_jwtOptions.ExpiryMinutes));
        return Ok(new { message = "Refreshed" });
    }

    /// <summary>
    /// Logs out the user by revoking the refresh token and clearing cookies.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(_jwtOptions.RefreshCookieName, out var refreshToken))
        {
            var refresh = await db.RefreshTokens.SingleOrDefaultAsync(rt => rt.Token == refreshToken);
            if (refresh != null && refresh.RevokedAtUtc == null)
            {
                refresh.RevokedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        Response.Cookies.Delete(_jwtOptions.CookieName);
        Response.Cookies.Delete(_jwtOptions.RefreshCookieName);
        return Ok(new { message = "Logged out" });
    }

    private void AppendCookie(string name, string value, TimeSpan lifetime)
    {
        Response.Cookies.Append(name, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(lifetime)
        });
    }

    private ClaimsPrincipal? ValidateJwt(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static AuthenticatedUser ToAuthenticatedUser(ClaimsPrincipal principal)
    {
        var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = principal.FindFirstValue("firstName") ?? string.Empty;
        var lastName = principal.FindFirstValue("lastName") ?? string.Empty;
        var rank = int.Parse(principal.FindFirstValue("rank") ?? "0");
        var roleId = Guid.Parse(principal.FindFirstValue("roleId") ?? Guid.Empty.ToString());
        var roleName = principal.FindFirstValue("roleName") ?? string.Empty;
        return new AuthenticatedUser(Guid.Parse(id ?? Guid.Empty.ToString()), email, firstName, lastName, rank, roleId, roleName);
    }
}
