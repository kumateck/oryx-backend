using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Base;

public class Resource : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string Type { get; set; } // e.g., Machine, Labor, etc.
    public bool IsAvailable { get; set; }
}
