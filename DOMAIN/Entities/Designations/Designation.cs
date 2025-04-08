using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Designations;

public class Designation : BaseEntity
{
    
    [Required] public string Name { get; set; }
    
    [StringLength(1000)] public string Description { get; set; }
    
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    
}