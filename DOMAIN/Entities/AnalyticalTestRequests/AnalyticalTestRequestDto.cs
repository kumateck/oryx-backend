using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequestDto : BaseDto
{
    public CollectionItemDto BatchManufacturingRecord { get; set; }
    public CollectionItemDto Product { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto ProductionActivityStep { get; set; }
    public DateTime ManufacturingDate { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    
    public string ReleasedAt { get; set; }
    
    public string Filled { get; set; }
    
    public DateTime ReleaseDate { get; set; }
    
    public string SampledQuantity { get; set; }
    
    public TestStage Stage { get; set; }
    
    public CollectionItemDto State { get; set; }
    
    public AnalyticalTestStatus Status { get; set; }
}