using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class AnalyticalTestRequest : BaseEntity
{
    public string BatchNumber { get; set; }
    
    public string ProductName { get; set; }
    
    public string ProductSchedule { get; set; }
    
    public DateTime ManufacturingDate { get; set; }
    
    public DateTime ExpiryDate { get; set; }

    public string ReleasedAt { get; set; } = "Quality Control";
    
    public string Filled { get; set;}
    
    public string SampledQuantity { get; set; }
    
    public DateTime ReleaseDate { get; set; }
    public string QcManagerSignature { get; set; }
    public string QaManagerSignature { get; set; }
    
    public TestStage Stage { get; set; }
    
    public State State { get; set; }
    
    public Status Status { get; set; }
} 
public enum TestStage
{
     Intermediate,
     Bulk,
     Finished
}

public enum Status
{
    Quarantine,
    UnderTest,
    TestComplete,
    Approved,
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