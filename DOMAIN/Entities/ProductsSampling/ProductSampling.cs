using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductsSampling;

public class ProductSampling : BaseEntity
{
     public string ArNumber { get; set; }
     public Guid AnalyticalTestRequestId { get; set; }
     
     public string SampleQuantity {get; set;}
     
     public int ContainersSampled {get; set;}
     
     public DateTime SampleDate {get; set;} = DateTime.UtcNow;
     
     public AnalyticalTestRequest AnalyticalTestRequest { get; set; }
     
}