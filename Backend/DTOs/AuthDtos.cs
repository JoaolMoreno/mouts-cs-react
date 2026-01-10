using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password);

public record AuthenticatedUser(Guid Id, string Email, string FirstName, string LastName, int Rank, Guid RoleId, string RoleName);
