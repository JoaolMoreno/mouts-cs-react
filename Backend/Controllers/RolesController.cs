using Backend.Data;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(AppDbContext db, ICurrentUserAccessor currentUserAccessor) : ControllerBase
{
    [HttpGet]
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

