using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public class IssuedBmrChange
{
    public Guid Id { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid ActorId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? RevisionId { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; }
    [StringLength(100)] public string PreviousBatchNumber { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public DateTime? PreviousManufacturingDate { get; set; }
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string AffectedRecordsJson { get; set; } = "{}";
}

public enum IssuedBmrRevisionStatus { Pending = 0, Approved = 1, Rejected = 2 }

public class IssuedBmrRevision
{
    public Guid Id { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid RequestedById { get; set; }
    public DateTime RequestedAt { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public DateTime? PreviousManufacturingDate { get; set; }
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public IssuedBmrRevisionStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }
    [StringLength(1000)] public string ReviewReason { get; set; }
}

public class IssuedBmrPage
{
    public List<IssuedBmrRow> Data { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageCount { get; set; }
    public int TotalRecordCount { get; set; }
}

public class IssuedBmrRow
{
    public Guid Id { get; set; }
    public Guid ScheduleProductId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public string DepartmentName { get; set; }
    public string ScheduleCode { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime IssuedAt { get; set; }
    public bool IsRetired { get; set; }
    public int PendingRevisions { get; set; }
}

public class IssuedBmrExpected
{
    [Required] public string ExpectedBatchNumber { get; set; }
    public DateTime? ExpectedManufacturingDate { get; set; }
    public DateTime? ExpectedExpiryDate { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; }
}

public class CorrectIssuedBmrNumber : IssuedBmrExpected
{
    [Required, StringLength(100)] public string BatchNumber { get; set; }
}

public class RequestIssuedBmrRevision : IssuedBmrExpected
{
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
}

public class ReviewIssuedBmrRevision
{
    public bool Approve { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; }
}

public class IssuedBmrHistory
{
    public List<IssuedBmrChangeDto> Changes { get; set; } = [];
    public List<IssuedBmrRevisionDto> Revisions { get; set; } = [];
}

public class IssuedBmrChangeDto
{
    public Guid Id { get; set; }
    public Guid? RevisionId { get; set; }
    public string ActorName { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Reason { get; set; }
    public string PreviousBatchNumber { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? PreviousManufacturingDate { get; set; }
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class IssuedBmrRevisionDto
{
    public Guid Id { get; set; }
    public string RequestedByName { get; set; }
    public DateTime RequestedAt { get; set; }
    public string Reason { get; set; }
    public string BatchNumber { get; set; }
    public DateTime? PreviousManufacturingDate { get; set; }
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public IssuedBmrRevisionStatus Status { get; set; }
    public string ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string ReviewReason { get; set; }
    public bool IsRequester { get; set; }
}
