using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Backend.Services;

public interface ICurrentUserAccessor
{
    AuthenticatedUserContext? GetCurrentUser();
}

public record AuthenticatedUserContext(Guid Id, string Email, string FirstName, string LastName, int Rank, Guid RoleId, string RoleName);

public class HttpContextCurrentUserAccessor(IHttpContextAccessor accessor) : ICurrentUserAccessor
{
    public AuthenticatedUserContext? GetCurrentUser()
    {
        var user = accessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue(JwtRegisteredClaimNames.Email);
        var firstName = user.FindFirstValue("firstName");
        var lastName = user.FindFirstValue("lastName");
        var rankValue = user.FindFirstValue("rank");
        var roleIdValue = user.FindFirstValue("roleId");
        var roleName = user.FindFirstValue("roleName");

        if (id == null || email == null || rankValue == null || roleIdValue == null)
        {
            return null;
        }

        return new AuthenticatedUserContext(
            Guid.Parse(id),
            email,
            firstName ?? string.Empty,
            lastName ?? string.Empty,
            int.Parse(rankValue),
            Guid.Parse(roleIdValue),
            roleName?? string.Empty);
    }
}
