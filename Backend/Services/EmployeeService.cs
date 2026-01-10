using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface IEmployeeService
{
    Task<EmployeeResponse?> GetByIdAsync(Guid id, AuthenticatedUserContext current);
    Task<List<EmployeeResponse>> GetAsync(AuthenticatedUserContext current);
    Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, AuthenticatedUserContext current);
    Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, AuthenticatedUserContext current);
    Task<bool> DeleteAsync(Guid id, AuthenticatedUserContext current);
}

public class EmployeeService(AppDbContext db, IPasswordHasher hasher) : IEmployeeService
{
    public async Task<EmployeeResponse?> GetByIdAsync(Guid id, AuthenticatedUserContext current)
    {
        var employee = await QueryByScope(current).FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
        {
            return null;
        }
        var manager = await db.Employees.FirstOrDefaultAsync(e => e.Id == employee.ManagerId);
        employee.Manager = manager;
        return ToResponse(employee);
    }

    public async Task<List<EmployeeResponse>> GetAsync(AuthenticatedUserContext current)
    {
        var employees = await QueryByScope(current)
            .ToListAsync();
        return employees.Select(ToResponse).ToList();
    }

    public async Task<EmployeeResponse> CreateAsync(EmployeeCreateRequest request, AuthenticatedUserContext current)
    {
        await ValidateRankAsync(request.RoleId, current);
        ValidateAge(request.BirthDate);
        
        var existingEmail = await db.Employees.AnyAsync(e => e.Email.ToLower() == request.Email.Trim().ToLowerInvariant());
        if (existingEmail) throw new InvalidOperationException("Email already in use");
        var existingDocument = await db.Employees.AnyAsync(e => e.Document == request.Document.Trim());
        if (existingDocument) throw new InvalidOperationException("Document already in use");

        var now = DateTime.UtcNow;
        var entity = new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Document = request.Document.Trim(),
            RoleId = request.RoleId,
            ManagerId = current.Id,
            BirthDate = request.BirthDate,
            PasswordHash = hasher.Hash(request.Password),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Employees.Add(entity);
        await db.SaveChangesAsync();

        return ToResponse(await LoadWithRole(entity.Id));
    }

    public async Task<EmployeeResponse?> UpdateAsync(Guid id, EmployeeUpdateRequest request, AuthenticatedUserContext current)
    {
        var entity = await QueryByScope(current).FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
        {
            return null;
        }

        if (request.RoleId.HasValue)
        {
            await ValidateRankAsync(request.RoleId.Value, current);
            entity.RoleId = request.RoleId.Value;
        }

        if (request.BirthDate.HasValue)
        {
            ValidateAge(request.BirthDate.Value);
            entity.BirthDate = request.BirthDate.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName)) entity.FirstName = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName)) entity.LastName = request.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(request.Email)) entity.Email = request.Email.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.Document)) entity.Document = request.Document.Trim();
        if (!string.IsNullOrWhiteSpace(request.Password)) entity.PasswordHash = hasher.Hash(request.Password);

        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return ToResponse(await LoadWithRole(entity.Id));
    }

    public async Task<bool> DeleteAsync(Guid id, AuthenticatedUserContext current)
    {
        var entity = await QueryByScope(current).FirstOrDefaultAsync(e => e.Id == id);
        if (entity == null)
        {
            return false;
        }

        db.Employees.Remove(entity);
        await db.SaveChangesAsync();
        return true;
    }

    private IQueryable<Employee> QueryByScope(AuthenticatedUserContext current)
    {
        var query = db.Employees.Include(e => e.Role).AsQueryable();

        if (current.Rank == 1)
        {
            return query;
        }

        return query.Where(e => e.ManagerId == current.Id);
    }

    private async Task ValidateRankAsync(Guid roleId, AuthenticatedUserContext current)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId) ?? throw new InvalidOperationException("Role not found");
        if (role.Rank <= current.Rank)
        {
            throw new InvalidOperationException("Cannot manage role with equal or higher rank");
        }
    }

    private static void ValidateAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var age = today.Year - birthDate.Year - (today.DayOfYear < birthDate.DayOfYear ? 1 : 0);
        if (age < 18)
        {
            throw new InvalidOperationException("Employee must be at least 18 years old");
        }
    }

    private async Task<Employee> LoadWithRole(Guid id)
    {
        var entity = await db.Employees.Include(e => e.Role)
            .FirstAsync(e => e.Id == id);
        return entity;
    }

    private static EmployeeResponse ToResponse(Employee e)
    {
        return new EmployeeResponse(
            e.Id,
            e.FirstName,
            e.LastName,
            e.Email,
            e.Document,
            e.RoleId,
            e.Role?.Name ?? string.Empty,
            // ManagerName
            (e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : string.Empty),
            // Rank
            e.Role?.Rank ?? 0,
            // ManagerId
            e.ManagerId,
            e.BirthDate,
            e.CreatedAtUtc,
            e.UpdatedAtUtc);
    }
}
