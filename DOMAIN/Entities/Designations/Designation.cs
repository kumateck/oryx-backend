using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Designation;

public class Designation : BaseEntity
{
    
    [Required] public string Name { get; set; }
    
    [StringLength(1000)] public string Description { get; set; }
    
    [Required] public Guid DepartmentId { get; set; }
    
    public Department Department { get; set; }
    
}