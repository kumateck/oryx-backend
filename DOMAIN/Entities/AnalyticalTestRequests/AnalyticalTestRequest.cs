using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequest : BaseEntity
{
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    
    public DateTime ManufacturingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string ReleasedAt { get; set; } = "Quality Control";
    public string Filled { get; set;}
    public string SampledQuantity { get; set; }
    public DateTime ReleaseDate { get; set; }
    public TestStage Stage { get; set; }
    public Guid StateId { get; set; }
    public ProductState State { get; set; }
    public Status Status { get; set; }
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

public enum Status
{
    New,
    Sampled,
    Acknowledged,
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