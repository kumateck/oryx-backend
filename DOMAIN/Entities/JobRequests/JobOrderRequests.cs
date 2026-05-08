using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateJobOrderRequest
{
    [Required]
    public Guid JobRequestId { get; set; }

    public Guid? ServiceId { get; set; }

    [Required]
    public DateTime IssuedDate { get; set; }

    [Required]
    public Guid IssuedById { get; set; }

    [StringLength(2000)]
    public string Description { get; set; }

    [StringLength(100)]
    public string IssuedBySignature { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one service provider must be selected")]
    public List<Guid> ServiceProviderIds { get; set; }
}

public class SendJobOrderToProvidersRequest
{
    [Required]
    public Guid JobOrderId { get; set; }

    [Required, MinLength(1)]
    public List<Guid> ServiceProviderIds { get; set; }
}

public class StartJobOrderExecutionRequest
{
    [Required]
    public Guid JobOrderId { get; set; }

    [Required]
    public Guid ServiceProviderId { get; set; }

    public string Notes { get; set; }
}

public class CompleteJobOrderExecutionRequest
{
    [Required]
    public Guid JobOrderExecutionId { get; set; }

    public string Notes { get; set; }

    public List<RecordJobActivityRequest> Activities { get; set; } = [];

    public List<RecordConsumedItemRequest> ConsumedItems { get; set; } = [];
}

public class VerifyJobOrderExecutionRequest
{
    [Required]
    public Guid JobOrderExecutionId { get; set; }

    [Required]
    public Guid VerifiedById { get; set; }

    [StringLength(1000)]
    public string VerificationComments { get; set; }
}

public class ApproveJobOrderExecutionRequest
{
    [Required]
    public Guid JobOrderExecutionId { get; set; }

    [Required]
    public Guid ApprovedById { get; set; }

    [StringLength(1000)]
    public string ApprovalComments { get; set; }

    public bool RequesterSatisfied { get; set; }

    [StringLength(1000)]
    public string RequesterComments { get; set; }
}

