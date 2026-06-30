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
    public Guid? AnalyticalTestRequestId { get; set; }
    public Guid? MaterialBatchId { get; set; }

    [Required]
    public string CoaNumber { get; set; }

    public string ProductOrMaterialName { get; set; }
    public string BatchNumber { get; set; }

    [Required]
    public string RejectionReason { get; set; }

    public string RootCauseAnalysis { get; set; }
    public string CorrectiveActions { get; set; }
    public string PreventiveActions { get; set; }
    public string InvestigationDetails { get; set; }
}

public class ReviewOosInvestigationRequest
{
    [Required]
    public bool Approve { get; set; }

    public string Comments { get; set; }
}
