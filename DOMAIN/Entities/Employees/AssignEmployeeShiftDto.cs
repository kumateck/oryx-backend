using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Employees;

public class AssignEmployeeShiftDto
{
    [Required] public List<Guid> EmployeeIds { get; set; }
}