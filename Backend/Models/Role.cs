using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int Rank { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

