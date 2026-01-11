using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Backend.Controllers;

/// <summary>
/// Manage employees (CRUD) with paging, filtering and sorting.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class EmployeesController(IEmployeeService employeeService, ICurrentUserAccessor currentUserAccessor)
    : ControllerBase
{
    /// <summary>
    /// Returns a paged list of employees. Supports filtering, sorting and paging via query parameters.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<EmployeeResponse>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<EmployeeResponse>>> Get([FromQuery] EmployeeListQuery query)
    {
        var current = RequireUser();
        var employees = await employeeService.GetAsync(query, current);
        return Ok(employees);
    }

    /// <summary>
    /// Gets a single employee by id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id)
    {
        var current = RequireUser();
        var employee = await employeeService.GetByIdAsync(id, current);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    /// <summary>
    /// Creates a new employee. The current user will be assigned as manager by default.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeCreateRequest request)
    {
        var current = RequireUser();
        var created = await employeeService.CreateAsync(request, current);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Partially updates an employee. To remove the manager, pass RemoveManager=true. ManagerId and RemoveManager cannot be used together.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EmployeeResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, EmployeeUpdateRequest request)
    {
        var current = RequireUser();
        var updated = await employeeService.UpdateAsync(id, request, current);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    /// <summary>
    /// Deletes an employee. A user cannot delete their own account.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var current = RequireUser();
        if(id == current.Id)
        {
            return BadRequest(new { message = "Cannot delete your own account." });
        }
        var deleted = await employeeService.DeleteAsync(id, current);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private AuthenticatedUserContext RequireUser()
    {
        var current = currentUserAccessor.GetCurrentUser();
        if (current == null)
        {
            throw new UnauthorizedAccessException();
        }

        return current;
    }
}
