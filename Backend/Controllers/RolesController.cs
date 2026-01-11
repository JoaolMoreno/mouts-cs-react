using Backend.Data;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Backend.Controllers;

/// <summary>
/// Manage roles available in the application.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RolesController(AppDbContext db, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    /// <summary>
    /// Gets the list of roles that the current user can assign or view (roles with rank >= current user's rank).
    /// </summary>
    /// <returns>List of roles</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<RoleResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> Get()
    {
        var current = currentUserAccessor.GetCurrentUser();
        if (current == null) return Unauthorized();

        var roles = await db.Roles
            .Where(r => r.Rank >= current.Rank)
            .OrderBy(r => r.Rank)
            .ThenBy(r => r.Name)
            .Select(r => new RoleResponse(r.Id, r.Name, r.Rank))
            .ToListAsync();

        return Ok(roles);
    }
}
