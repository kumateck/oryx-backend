using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollRuns;

public class PayrollRunDto : BaseDto
{
    public string Code { get; set; }
    public PayrollRunType RunType { get; set; }
    public PayrollRunStatus RunStatus { get; set; }

    public int EmployeeCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal NetTotal { get; set; }
    public decimal EmployerCostTotal { get; set; }

    public string Notes { get; set; }
    public string Checksum { get; set; }

    public DateTime? LockedAt { get; set; }
    public UserDto LockedBy { get; set; }

    public DateTime? PrecheckCompletedAt { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaymentReleasedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public CollectionItemDto PayrollCompany { get; set; }
    public CollectionItemDto PayrollPeriod { get; set; }
    public CollectionItemDto PayGroup { get; set; }
    public CollectionItemDto ParentRun { get; set; }

    public bool Approved { get; set; }
}

public class PayrollRunSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public PayrollRunStatus RunStatus { get; set; }
    public PayrollRunType RunType { get; set; }
    public int EmployeeCount { get; set; }
    public decimal NetTotal { get; set; }
    public DateTime CreatedAt { get; set; }
}
