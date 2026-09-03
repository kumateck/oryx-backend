using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Payroll;

public class CreatePayGradeRequest
{
    public string Name { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public EmployeeLevel? Level { get; set; }
}

public class PayGradeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }
    public EmployeeLevel? Level { get; set; }
}
