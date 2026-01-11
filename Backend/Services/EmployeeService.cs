using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface IEmployeeService
{
    Task<EmployeeResponse?> GetByIdAsync(Guid id, AuthenticatedUserContext current);
    Task<PagedResult<EmployeeResponse>> GetAsync(EmployeeListQuery query, AuthenticatedUserContext current);
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

    public async Task<PagedResult<EmployeeResponse>> GetAsync(EmployeeListQuery query, AuthenticatedUserContext current)
    {
        var validatedQuery = NormalizeQuery(query);
        var scoped = QueryByScope(current);

        // Include Role when filtering/sorting by role name
        if (!string.IsNullOrWhiteSpace(validatedQuery.RoleName) || (validatedQuery.OrderBy != null && validatedQuery.OrderBy.Equals("RoleName", StringComparison.OrdinalIgnoreCase)))
        {
            scoped = scoped.Include(e => e.Role);
        }

        scoped = ApplyFilters(scoped, validatedQuery);
        scoped = ApplySorting(scoped, validatedQuery);

        var total = await scoped.CountAsync();
        var items = await scoped
            .Skip((validatedQuery.Page - 1) * validatedQuery.PageSize)
            .Take(validatedQuery.PageSize)
            .ToListAsync();

        var responses = items.Select(ToResponse).ToList();
        return new PagedResult<EmployeeResponse>(responses, validatedQuery.Page, validatedQuery.PageSize, total);
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
            FirstName = request.FirstName.Trim().ToUpperInvariant(),
            LastName = request.LastName.Trim().ToUpperInvariant(),
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

        if (request.RemoveManager && request.ManagerId.HasValue)
        {
            throw new InvalidOperationException("Cannot specify ManagerId when RemoveManager is true");
        }

        if (request.RemoveManager)
        {
            entity.ManagerId = null;
        }
        else if (request.ManagerId.HasValue)
        {
            var newManagerId = request.ManagerId.Value;
            if (newManagerId == entity.Id)
            {
                throw new InvalidOperationException("Employee cannot be their own manager");
            }

            var newManager = await db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == newManagerId)
                             ?? throw new InvalidOperationException("Manager not found");

            if (newManager.Role == null)
            {
                throw new InvalidOperationException("Manager role not found");
            }

            if (entity.Role == null)
            {
                entity.Role = await db.Roles.FirstOrDefaultAsync(r => r.Id == entity.RoleId);
            }

            var currentRank = entity.Role?.Rank ?? 0;
            if (newManager.Role.Rank >= currentRank)
            {
                throw new InvalidOperationException("Manager must have a higher rank");
            }

            entity.ManagerId = newManagerId;
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

        if (!string.IsNullOrWhiteSpace(request.FirstName)) entity.FirstName = request.FirstName.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(request.LastName)) entity.LastName = request.LastName.Trim().ToUpperInvariant();
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

    private static readonly HashSet<string> SortableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Employee.FirstName),
        nameof(Employee.LastName),
        nameof(Employee.Email),
        nameof(Employee.Document),
        nameof(Employee.RoleId),
        nameof(Employee.BirthDate),
        nameof(Employee.CreatedAtUtc),
        nameof(Employee.UpdatedAtUtc),
        "RoleName"
    };

    private static EmployeeListQuery NormalizeQuery(EmployeeListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is <= 0 or > 100 ? 20 : query.PageSize;
        var orderBy = string.IsNullOrWhiteSpace(query.OrderBy) ? nameof(Employee.CreatedAtUtc) : query.OrderBy.Trim();
        var orderDirection = string.IsNullOrWhiteSpace(query.OrderDirection) ? "desc" : query.OrderDirection.Trim();
        return query with
        {
            Page = page,
            PageSize = pageSize,
            OrderBy = orderBy,
            OrderDirection = orderDirection,
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            FirstName = string.IsNullOrWhiteSpace(query.FirstName) ? null : query.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(query.LastName) ? null : query.LastName.Trim(),
            Email = string.IsNullOrWhiteSpace(query.Email) ? null : query.Email.Trim(),
            Document = string.IsNullOrWhiteSpace(query.Document) ? null : query.Document.Trim(),
            RoleName = string.IsNullOrWhiteSpace(query.RoleName) ? null : query.RoleName.Trim()
        };
    }

    private IQueryable<Employee> ApplyFilters(IQueryable<Employee> query, EmployeeListQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.ToLowerInvariant();
            query = query.Where(e =>
                e.FirstName.ToLower()!.Contains(term) ||
                e.LastName.ToLower()!.Contains(term) ||
                e.Email.ToLower()!.Contains(term) ||
                e.Document.ToLower()!.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.FirstName))
        {
            var first = filter.FirstName.ToLowerInvariant();
            query = query.Where(e => e.FirstName.ToLower()!.Contains(first));
        }

        if (!string.IsNullOrWhiteSpace(filter.LastName))
        {
            var last = filter.LastName.ToLowerInvariant();
            query = query.Where(e => e.LastName.ToLower()!.Contains(last));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            var email = filter.Email.ToLowerInvariant();
            query = query.Where(e => e.Email.ToLower()!.Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(filter.Document))
        {
            var document = filter.Document.ToLowerInvariant();
            query = query.Where(e => e.Document.ToLower()!.Contains(document));
        }

        if (filter.RoleId.HasValue)
        {
            query = query.Where(e => e.RoleId == filter.RoleId.Value);
        }

        if (filter.ManagerId.HasValue)
        {
            query = query.Where(e => e.ManagerId == filter.ManagerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RoleName))
        {
            var rn = filter.RoleName.ToLowerInvariant();
            query = query.Where(e => e.Role != null && e.Role.Name.ToLower()!.Contains(rn));
        }

        return query;
    }

    private IQueryable<Employee> ApplySorting(IQueryable<Employee> query, EmployeeListQuery request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderBy) || !SortableFields.Contains(request.OrderBy))
        {
            return query.OrderByDescending(e => e.CreatedAtUtc);
        }

        var orderDir = request.OrderDirection?.ToLowerInvariant();
        var orderBy = request.OrderBy;

        return (orderBy.ToLowerInvariant()) switch
        {
            var f when f == nameof(Employee.FirstName).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.FirstName) : query.OrderByDescending(e => e.FirstName),
            var f when f == nameof(Employee.LastName).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.LastName) : query.OrderByDescending(e => e.LastName),
            var f when f == nameof(Employee.Email).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.Email) : query.OrderByDescending(e => e.Email),
            var f when f == nameof(Employee.Document).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.Document) : query.OrderByDescending(e => e.Document),
            var f when f == nameof(Employee.RoleId).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.RoleId) : query.OrderByDescending(e => e.RoleId),
            var f when f == nameof(Employee.BirthDate).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.BirthDate) : query.OrderByDescending(e => e.BirthDate),
            var f when f == nameof(Employee.CreatedAtUtc).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.CreatedAtUtc) : query.OrderByDescending(e => e.CreatedAtUtc),
            var f when f == nameof(Employee.UpdatedAtUtc).ToLowerInvariant() => orderDir == "asc" ? query.OrderBy(e => e.UpdatedAtUtc) : query.OrderByDescending(e => e.UpdatedAtUtc),
            var f when f == "rolename" => orderDir == "asc" ? query.OrderBy(e => e.Role!.Name) : query.OrderByDescending(e => e.Role!.Name),
            _ => query.OrderByDescending(e => e.CreatedAtUtc)
        };
    }
}
