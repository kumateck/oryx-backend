using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.OosInvestigations;

public class OosInvestigation : BaseEntity
{
    public Guid? AnalyticalTestRequestId { get; set; }
    public AnalyticalTestRequest AnalyticalTestRequest { get; set; }

    public Guid? MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }

    [StringLength(100)]
    public string CoaNumber { get; set; }

    [StringLength(500)]
    public string ProductOrMaterialName { get; set; }

    [StringLength(200)]
    public string BatchNumber { get; set; }

    [StringLength(2000)]
    public string RejectionReason { get; set; }

    // OOS Investigation Form Fields
    [StringLength(4000)]
    public string RootCauseAnalysis { get; set; }

    [StringLength(4000)]
    public string CorrectiveActions { get; set; }

    [StringLength(4000)]
    public string PreventiveActions { get; set; }

    [StringLength(4000)]
    public string InvestigationDetails { get; set; }

    public OosInvestigationStatus Status { get; set; }

    public DateTime? SubmittedToQaAt { get; set; }
    public Guid? SubmittedById { get; set; }
    public User SubmittedBy { get; set; }

    public DateTime? QaReviewedAt { get; set; }
    public Guid? QaReviewerId { get; set; }
    public User QaReviewer { get; set; }

    [StringLength(2000)]
    public string QaReviewComments { get; set; }
}

public enum OosInvestigationStatus
{
    Initiated = 0,
    SubmittedToQa = 1,
    QaApproved = 2,
    PermanentlyRejected = 3
}
