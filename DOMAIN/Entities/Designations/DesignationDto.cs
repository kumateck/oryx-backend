using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Designations;

public class DesignationDto: BaseDto
{
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public int MaximumLeaveDays { get; set; }

    public List<DepartmentDto> Departments { get; set; } = [];
    
}