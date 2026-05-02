using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequest : BaseEntity
{
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    public ProductionScheduleProduct ProductionScheduleProduct { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public User ReleasedBy { get; set; }
    public string Filled { get; set; }
    public string SampledQuantity { get; set; }
    public TestStage Stage { get; set; }
    public Guid StateId { get; set; }
    public ProductState State { get; set; }
    public int NumberOfContainers { get; set; }
    public Guid? SampledById { get; set; }
    public User SampledBy { get; set; }
    public Guid? AcknowledgedById { get; set; }
    public User AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? SampledAt { get; set; }
    public AnalyticalTestStatus Status { get; set; }
    public Guid? TestedById { get; set; }
    public User TestedBy { get; set; }
    public DateTime? TestedAt { get; set; }
    public DateTime? AssignedAt { get; set; }

    public List<AnalyticalTestRequestAssignee> Assignees { get; set; } = [];

    [StringLength(1000)]
    public string ArNumber { get; set; }

    [StringLength(100)]
    public string IssueNumber { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
}

public class AnalyticalTestRequestAssignee : BaseEntity
{
    public Guid AnalyticalTestRequestId { get; set; }
    public AnalyticalTestRequest AnalyticalTestRequest { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public class ProductState : BaseEntity
{
    [StringLength(1000)]
    public string Name { get; set; }
}

public enum TestStage
{
    Intermediate,
    Bulk,
    Finished,
}

public enum AnalyticalTestStatus
{
    New = 0,
    Sampled = 1,
    Acknowledged = 2,
    Testing = 3,
    TestTaken = 4,
    Released = 5,
    Assigned = 6,
}

public enum State
{
    Liquid,
    Granules,
    CompressedTablet,
    FilledCapsules,
    Ointment,
    Coated,
}
