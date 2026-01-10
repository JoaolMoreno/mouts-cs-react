using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record EmployeeCreateRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(50)] string Document,
    [Required] Guid RoleId,
    [Required] DateOnly BirthDate,
    [Required, MinLength(6)] string Password);

public record EmployeeUpdateRequest(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [EmailAddress, MaxLength(256)] string? Email,
    [MaxLength(50)] string? Document,
    Guid? RoleId,
    DateOnly? BirthDate,
    [MinLength(6)] string? Password);

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
    DateTime UpdatedAtUtc);

