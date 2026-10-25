using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.QcWorksheets;

namespace DOMAIN.Entities.Materials.Batch;

public enum MaterialRetestStatus
{
    PendingApproval = 0,
    Authorized = 1,
    SampleTaken = 2,
    Acknowledged = 3,
    Testing = 4,
    Completed = 5,
    Rejected = 6
}

public enum MaterialRetestOrigin
{
    Scheduled = 0,
    Manual = 1
}

/// <summary>One immutable request cycle. The material's scheduled RetestDate is never rewritten.</summary>
public class MaterialRetestRequest : BaseEntity, IRequireApproval
{
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    [StringLength(160)] public string SourceKey { get; set; }
    public MaterialRetestOrigin Origin { get; set; }
    public MaterialRetestStatus Status { get; set; }
    public BatchStatus PreviousBatchStatus { get; set; }
    public bool Approved { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? OriginalExpiryDate { get; set; }
    [StringLength(2000)] public string Reason { get; set; }
    public DateTime? SampleTakenAt { get; set; }
    public Guid? SampleTakenById { get; set; }
    public decimal? SampleQuantity { get; set; }
    public int? SampledContainers { get; set; }
    public DateTime? ProposedExpiryDate { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedById { get; set; }
    public Guid? TestRequestId { get; set; }
    public TestRequest TestRequest { get; set; }
    public Guid? CoaId { get; set; }
    public Coa Coa { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedById { get; set; }
    public List<MaterialRetestEvent> Events { get; set; } = [];
}

/// <summary>Append-only actor and reason trail for every retest transition.</summary>
public class MaterialRetestEvent
{
    public Guid Id { get; set; }
    public Guid MaterialRetestRequestId { get; set; }
    public MaterialRetestRequest MaterialRetestRequest { get; set; }
    [StringLength(50)] public string Action { get; set; }
    [StringLength(2000)] public string Detail { get; set; }
    public Guid? ActorId { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class CreateMaterialRetestRequest
{
    [Required, MinLength(1), MaxLength(2000)] public string Reason { get; set; }
}

public class RecordMaterialRetestSample
{
    [Range(0.000001, double.MaxValue)] public decimal SampleQuantity { get; set; }
    [Range(1, int.MaxValue)] public int SampledContainers { get; set; }
    [Required] public DateTime ProposedExpiryDate { get; set; }
}

public class LinkMaterialRetestTestRequest
{
    [Required] public Guid TestRequestId { get; set; }
}

public class MaterialRetestDecision
{
    [Required] public string Password { get; set; }
    [Required, MinLength(1), MaxLength(1000)] public string Comments { get; set; }
}
