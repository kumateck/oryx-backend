using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateJobRequest
{
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
}

public class UpdateJobRequestRequest
{
    public string Location { get; set; }
    public Guid? EquipmentId { get; set; }
    public string EquipmentInstrumentNumber { get; set; }
    public string DescriptionOfWork { get; set; }
    public DateTime? PreferredCompletionDate { get; set; }
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