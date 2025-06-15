using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductsSampling;

public class CreateProductSamplingRequest
{
    [Required] public Guid AnalyticalTestRequestId { get; set; }
    
    [Required] public string ArNumber { get; set; }
    
    [Required] public string SampleQuantity { get; set; }
    
    [Required] public int ContainersSampled  { get; set; }
    
}