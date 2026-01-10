using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController(IEmployeeService employeeService, ICurrentUserAccessor currentUserAccessor)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> Get()
    {
        var current = RequireUser();
        var employees = await employeeService.GetAsync(current);
        return Ok(employees);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(Guid id)
    {
        var current = RequireUser();
        var employee = await employeeService.GetByIdAsync(id, current);
        if (employee == null) return NotFound();
        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(EmployeeCreateRequest request)
    {
        var current = RequireUser();
        var created = await employeeService.CreateAsync(request, current);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<EmployeeResponse>> Update(Guid id, EmployeeUpdateRequest request)
    {
        var current = RequireUser();
        var updated = await employeeService.UpdateAsync(id, request, current);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
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
