using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollElementAssignments;

public class CreatePayrollElementAssignment
{
    [Required] public Guid EmployeeId { get; set; }
    [Required] public Guid PayrollElementId { get; set; }

    public decimal? OverrideAmount { get; set; }
    public decimal? OverrideRate { get; set; }
    [StringLength(4000)] public string OverrideFormula { get; set; }

    [Required] public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public decimal? LoanBalance { get; set; }
    public decimal? LoanInstallment { get; set; }
    public int? LoanTermMonths { get; set; }
}