using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Analytical_Test_Requests;

public class AnalyticalTestRequest : BaseEntity
{
    public string BatchNumber { get; set; }
    
    public string ProductName { get; set; }
    
    public string ProductSchedule { get; set; }
    
    public DateTime ManufacturingDate { get; set; }
    
    public DateTime ExpiryDate { get; set; }
    
    public string ReleasedAt { get; set; }
    
    public string SampledQuantity { get; set; }
    
    public DateTime ReleaseDate { get; set; }
    public string QcManagerSignature { get; set; }
    public string QaManagerSignature { get; set; }
    
    public Stage Stage { get; set; }
    
    public Category Category { get; set; }
    
    public Status Status { get; set; }
} 
public enum Stage
{
     Intermediate,
     Bulk,
     Finished
}

public enum Status
{
    Pending,
    InProgress,
    Completed,
}

public enum Category
{
    Liquid,
    Granules,
    CompressedTablet,
    FilledCapsules,
    Ointment,
    Coated
}