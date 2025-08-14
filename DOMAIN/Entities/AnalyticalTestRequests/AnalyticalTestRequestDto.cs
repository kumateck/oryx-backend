using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequestDto : BaseDto
{
    public BatchManufacturingRecordDto BatchManufacturingRecord { get; set; }
    public CollectionItemDto Product { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    public ProductionActivityStepDto ProductionActivityStep { get; set; }
    public DateTime ManufacturingDate { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    public string Filled { get; set; }
    
    public DateTime? ReleasedAt { get; set; }
    
    public string SampledQuantity { get; set; }
    
    public TestStage Stage { get; set; }
    
    public CollectionItemDto State { get; set; }
    
    public AnalyticalTestStatus Status { get; set; }
    public int NumberOfContainers { get; set; }
    public UserDto SampledBy { get; set; }
    public UserDto ReleasedBy { get; set; }
    public DateTime? SampledAt { get; set; }
    public UserDto AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string ArNumber { get; set; }
}