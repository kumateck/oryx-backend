using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

public class PayGrade : BaseEntity
{
    [StringLength(200)] public string Name { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public EmployeeLevel? Level { get; set; }
}
