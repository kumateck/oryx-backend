using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollElements;

namespace DOMAIN.Entities.PayrollElementAssignments;

public class PayrollElementAssignment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }
    
    public Guid PayrollElementId { get; set; }
    public PayrollElement PayrollElement { get; set; }

    public decimal? OverrideAmount { get; set; }
    public decimal? OverrideRate { get; set; }
    [StringLength(4000)] public string OverrideFormula { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public decimal? LoanBalance { get; set; }
    public decimal? LoanInstallment { get; set; }
    public int? LoanTermMonths { get; set; }

    public PayrollElementAssignmentStatus Status { get; set; } = PayrollElementAssignmentStatus.Active;
}

public enum PayrollElementAssignmentStatus
{
    Active = 0,
    Suspended = 1,
    Closed = 2
}