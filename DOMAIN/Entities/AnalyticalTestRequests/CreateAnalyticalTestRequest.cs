using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.AnalyticalTestRequests;

public class CreateAnalyticalTestRequest
{
    [Required] public string BatchNumber { get; set; }
    
    [Required] public string ProductName { get; set; }
    
    [Required] public string ProductSchedule { get; set; }
    
    [Required] public string SampledQuantity { get; set; }
    
    [Required] public DateTime ManufacturingDate { get; set; }
    
    [Required] public DateTime ExpiryDate { get; set; }
    
    [Required] public string ReleasedAt { get; set; }
    
    [Required] public DateTime ReleaseDate { get; set; }
    
    [Required] public string QcManagerSignature { get; set; }
    
    [Required]  public string QaManagerSignature { get; set; }
    
    [Required] public TestStage Stage { get; set; }
    
    [Required] public State State { get; set; }
    
    [Required] public Status Status { get; set; }
}