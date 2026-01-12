using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class Phone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EmployeeId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Type { get; set; } // e.g., "Home", "Work", "Mobile"

    public bool IsPrimary { get; set; } = false;

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }
}
