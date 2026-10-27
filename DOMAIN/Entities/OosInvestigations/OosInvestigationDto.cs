using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.OosInvestigations;

public class OosInvestigationDto : BaseDto
{
    public Guid? AnalyticalTestRequestId { get; set; }
    public Guid? MaterialBatchId { get; set; }
    public string CoaNumber { get; set; }
    public string ProductOrMaterialName { get; set; }
    public string BatchNumber { get; set; }
    public string RejectionReason { get; set; }
    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }
    public string InvestigationDetails { get; set; }
    public OosInvestigationStatus Status { get; set; }
    public DateTime? SubmittedToQaAt { get; set; }
    public UserDto SubmittedBy { get; set; }
    public DateTime? QaReviewedAt { get; set; }
    public UserDto QaReviewer { get; set; }
    public string QaReviewComments { get; set; }
}

public class InitiateOosInvestigationRequest
{
    public Guid? ResponseId { get; set; }
    public Guid? AnalyticalTestRequestId { get; set; }
    public Guid? MaterialBatchId { get; set; }

    [Required, StringLength(100)]
    public string CoaNumber { get; set; }

    [StringLength(500)] public string ProductOrMaterialName { get; set; }
    [StringLength(200)] public string BatchNumber { get; set; }

    [Required, StringLength(2000)]
    public string RejectionReason { get; set; }

    [StringLength(4000)] public string RootCauseAnalysis { get; set; }
    [StringLength(4000)] public string CorrectiveActions { get; set; }
    [StringLength(4000)] public string PreventiveActions { get; set; }
    [StringLength(4000)] public string InvestigationDetails { get; set; }
}

public class ReviewOosInvestigationRequest
{
    [Required]
    public bool Approve { get; set; }

    public string Comments { get; set; }
}

public class UpdateOosInvestigationRequest
{
    [StringLength(500)] public string ProductOrMaterialName { get; set; }
    [StringLength(200)] public string BatchNumber { get; set; }
    [StringLength(4000)] public string RootCauseAnalysis { get; set; }
    [StringLength(4000)] public string CorrectiveActions { get; set; }
    [StringLength(4000)] public string PreventiveActions { get; set; }
    [StringLength(4000)] public string InvestigationDetails { get; set; }
}
