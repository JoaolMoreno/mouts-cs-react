using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record PhoneDto(
    [Required, MaxLength(20)] string Number,
    [MaxLength(50)] string? Type,
    bool IsPrimary = false);

public record EmployeeCreateRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(50)] string Document,
    [Required] Guid RoleId,
    [Required] DateOnly BirthDate,
    [Required, MinLength(6)] string Password,
    List<PhoneDto> Phones = null);

public record EmployeeUpdateRequest(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [EmailAddress, MaxLength(256)] string? Email,
    [MaxLength(50)] string? Document,
    Guid? RoleId,
    DateOnly? BirthDate,
    [MinLength(6)] string? Password,
    Guid? ManagerId,
    bool RemoveManager = false,
    List<PhoneDto>? Phones = null);

public record EmployeeListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? OrderBy = null,
    string? OrderDirection = null,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    string? Document = null,
    Guid? RoleId = null,
    Guid? ManagerId = null,
    string? RoleName = null);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record EmployeeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Document,
    Guid RoleId,
    string RoleName,
    string ManagerName,
    int Rank,
    Guid? ManagerId,
    DateOnly BirthDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<PhoneDto> Phones);
