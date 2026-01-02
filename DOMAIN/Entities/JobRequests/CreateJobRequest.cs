using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateJobRequest
{
    [StringLength(255)] public string Code { get; set; }
    [Required, StringLength(500)]
    public string Location { get; set; }
    public Guid? EquipmentId { get; set; }
    [StringLength(1000)]
    public string EquipmentInstrumentNumber { get; set; }
    [Required]
    public DateTime DateOfIssue { get; set; }
    [Required, StringLength(2000)]
    public string DescriptionOfWork { get; set; }
    [Required]
    public DateTime PreferredCompletionDate { get; set; }
    [StringLength(500)]
    public string Item { get; set; }
    [StringLength(500)]
    public string ItemNumber { get; set; }
    
    public List<Guid> ServiceIds { get; set; } = [];
}

public class UpdateJobRequestRequest
{
    public string Location { get; set; }
    public Guid? EquipmentId { get; set; }
    public string EquipmentInstrumentNumber { get; set; }
    public string DescriptionOfWork { get; set; }
    public DateTime? PreferredCompletionDate { get; set; }
    public string Item { get; set; }
    public string ItemNumber { get; set; }
    public Guid? ServiceId { get; set; }
    public List<Guid> ServiceIds { get; set; } = [];
}

public class AssignInternalJobRequest
{
    [Required] public Guid JobRequestId { get; set; }
    [Required] public Guid AssignedToEmployeeId { get; set; }
    [Required] public Guid AssignedById { get; set; }
    public string Notes { get; set; }
}

public class ReassignJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }
    [Required] public Guid NewEmployeeId { get; set; }

    [Required, StringLength(2000)]
    public string Reason { get; set; }

    public string Notes { get; set; }
}

public class AcknowledgeJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }
}

public class StartJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }

    [Required, StringLength(2000)]
    public string ActivityDescription { get; set; }

    public string Notes { get; set; }
}

public class RecordJobActivityRequest
{
    [Required, StringLength(2000)]
    public string ActivityDescription { get; set; }
    [Required]
    public DateTime PerformedAt { get; set; }
    [Required]
    public Guid PerformedById { get; set; }
    public string Notes { get; set; }
}

public class RecordConsumedItemRequest
{
    [Required]
    public Guid ItemId { get; set; }
    [Required]
    public decimal QuantityConsumed { get; set; }
    [Required]
    public Guid UnitOfMeasureId { get; set; }
    public string Notes { get; set; }
    public ItemSource Source { get; set; } = ItemSource.FromStock;
}

public class CompleteJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }
    [Required, StringLength(2000)]
    public string ActivityDescription { get; set; }
    public string Notes { get; set; }
    public List<RecordJobActivityRequest> AdditionalActivities { get; set; } = [];
    public List<RecordConsumedItemRequest> ConsumedItems { get; set; } = [];
}

public class VerifyJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }
    [Required] public Guid VerifiedById { get; set; }
    [StringLength(1000)]
    public string VerificationComments { get; set; }
}

public class ApproveJobExecutionRequest
{
    [Required] public Guid JobExecutionId { get; set; }
    [Required] public Guid ApprovedById { get; set; }
    [StringLength(1000)]
    public string ApprovalComments { get; set; }
}

public class UpdateJobRequestStatusRequest
{
    [Required(ErrorMessage = "Status is required. Valid values: 0 (Pending), 1 (Acknowledged), 2 (Assigned), 3 (JobStarted), 4 (Completed), 5 (SentToExternal), 6 (QuotationReceived), 7 (ContractorSelected), 8 (Approved), 9 (Cancelled)")]
    public JobRequestStatus Status { get; set; }
}

public class CompleteJobRequestRequest
{
    [Required]
    public Guid JobRequestId { get; set; }
    
    [Required, StringLength(2000)]
    public string ActivityPerformedNote { get; set; }
    
    public string Notes { get; set; }
}