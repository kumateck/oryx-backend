using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequest : BaseEntity
{
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public ProductionActivityStep ProductionActivityStep { get; set; }
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string ReleasedAt { get; set; } = "Quality Control";
    public Guid? ReleasedById { get; set; }
    public User ReleasedBy { get; set; }
    public string Filled { get; set;}
    public string SampledQuantity { get; set; }
    public DateTime? ReleaseDate { get; set; }
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
}

public class ProductState : BaseEntity
{
    [StringLength(1000)] public string Name { get; set; }
}
public enum TestStage
{
     Intermediate,
     Bulk,
     Finished
}

public enum AnalyticalTestStatus
{
    New = 0,
    Sampled = 1,
    Acknowledged = 2,
    Testing = 3,
    Released = 4,
}

public enum State
{
    Liquid,
    Granules,
    CompressedTablet,
    FilledCapsules,
    Ointment,
    Coated
}