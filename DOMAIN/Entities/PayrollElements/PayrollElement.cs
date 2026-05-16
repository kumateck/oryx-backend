using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollCompanies;

namespace DOMAIN.Entities.PayrollElements;


public class PayrollElement : BaseEntity
{
    [StringLength(50)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    public PayrollElementType Type { get; set; }
    public PayrollElementCategory Category { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsRecurring { get; set; }
    public bool AffectsGross { get; set; }
    public bool AffectsNet { get; set; }
    public Guid? DefaultAccountId { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public List<PayrollElementVersion> Versions { get; set; } = [];
}

public class PayrollElementVersion : BaseEntity, IRequireApproval
{
    public Guid PayrollElementId { get; set; }
    public PayrollElement PayrollElement { get; set; }

    public int VersionNumber { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(4000)] public string FormulaExpression { get; set; }
    public decimal? FlatAmount { get; set; }
    public decimal? Rate { get; set; }
    [StringLength(int.MaxValue)] public string BandsJson { get; set; }

    public PayrollElementVersionStatus Status { get; set; } = PayrollElementVersionStatus.Draft;
    public bool Approved { get; set; }
    public List<PayrollElementVersionApproval> Approvals { get; set; } = [];
}

public class PayrollElementVersionApproval : ResponsibleApprovalStage
{
    public Guid Id  { get; set; }
    public Guid PayrollElementVersionId { get; set; }
    public PayrollElementVersion PayrollElementVersion { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PayrollElementType
{
    Earning,
    PreTaxDeduction,
    Tax,
    PostTaxDeduction,
    EmployerContribution,
    Reimbursement,
    Provision
}

public enum PayrollElementCategory
{
    Basic,
    Allowance,
    Bonus,
    Overtime,
    Statutory,
    Loan,
    Garnishment,
    Custom
}

public enum PayrollElementVersionStatus
{
    Draft,
    PendingApproval,
    Approved,
    Superseded,
    Retired
}