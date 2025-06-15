using DOMAIN.Entities.AnalyticalTestRequests;

namespace DOMAIN.Entities.ProductsSampling;

public class ProductSamplingDto
{
    public Guid AnalyticalTestRequestId { get; set; }
     
    public string SampleQuantity {get; set;}
     
    public int ContainersSampled {get; set;}
    
    public DateTime SampleDate {get; set;}
    public AnalyticalTestRequestDto AnalyticalTestRequest { get; set; }
}