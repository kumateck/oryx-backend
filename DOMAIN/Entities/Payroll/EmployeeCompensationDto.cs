namespace DOMAIN.Entities.Payroll;

public class CreateEmployeeCompensationRequest
{
    public Guid EmployeeId { get; set; }
    public Guid? PayGradeId { get; set; }
    public decimal BasicSalary { get; set; }
    public string BankName { get; set; }
    public string BankBranch { get; set; }
    public string BankAccountNumber { get; set; }
    public string TinNumber { get; set; }
    public bool Tier3VoluntaryEnrolled { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public List<CreateCompensationAllowanceRequest> Allowances { get; set; } = [];
}

public class CreateCompensationAllowanceRequest
{
    public AllowanceType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; } = true;
}

public class EmployeeCompensationDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public string StaffNumber { get; set; }
    public Guid? PayGradeId { get; set; }
    public string PayGradeName { get; set; }
    public decimal BasicSalary { get; set; }
    public string BankName { get; set; }
    public string BankBranch { get; set; }
    public string BankAccountNumber { get; set; }
    public string TinNumber { get; set; }
    public bool Tier3VoluntaryEnrolled { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal TotalAllowances { get; set; }
    public List<CompensationAllowanceDto> Allowances { get; set; } = [];
}

public class CompensationAllowanceDto
{
    public Guid Id { get; set; }
    public AllowanceType Type { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
}
