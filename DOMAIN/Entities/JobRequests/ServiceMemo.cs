using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Formal service memo created after selecting a contractor
/// </summary>
public class ServiceMemo : BaseEntity, IRequireApproval
{
    [StringLength(100)]
    public string MemoNumber { get; set; }

    public Guid JobOrderId { get; set; }
    public JobOrder JobOrder { get; set; }

    public Guid ServiceQuotationId { get; set; }
    public ServiceQuotation ServiceQuotation { get; set; }

    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; }

    public DateTime IssuedDate { get; set; }

    public Guid IssuedById { get; set; }
    public User IssuedBy { get; set; }

    public decimal AgreedServiceCharge { get; set; }
    public decimal AgreedMaterialsCost { get; set; }
    public decimal TotalAgreedCost => AgreedServiceCharge + AgreedMaterialsCost;

    public DateTime ExpectedStartDate { get; set; }
    public DateTime ExpectedCompletionDate { get; set; }

    [StringLength(2000)]
    public string TermsAndConditions { get; set; }

    [StringLength(2000)]
    public string SpecialInstructions { get; set; }

    public ServiceMemoStatus Status { get; set; } = ServiceMemoStatus.Draft;

    // Approval workflow
    public List<ServiceMemoApproval> Approvals { get; set; } = [];
    public bool Approved { get; set; }

    public DateTime? ApprovedDate { get; set; }
    public bool Paid { get; set; }
}

public class ServiceMemoApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }

    public Guid ServiceMemoId { get; set; }
    public ServiceMemo ServiceMemo { get; set; }

    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum ServiceMemoStatus
{
    Draft,
    PendingApproval,
    Approved,
    Rejected,
    Issued,
    Completed,
    Cancelled
}

