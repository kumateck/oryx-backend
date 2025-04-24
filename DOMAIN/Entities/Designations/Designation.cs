using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.Designations;

public class Designation : BaseEntity
{
    
    [Required] public string Name { get; set; }
    
    [StringLength(1000)] public string Description { get; set; }
    
    [Range(1, 366)] public int MaximumLeaveDays { get; set; }

    public List<Department> Departments { get; set; }

    public List<LeaveType> LeaveTypes { get; set; }
    
}