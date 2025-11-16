using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductsSampling;

public class ProductSamplingDto : BaseDto
{
    public string ArNumber { get; set; }
    public decimal SampleQuantity { get; set; }
    public int ContainersSampled { get; set; }
    public DateTime SampleDate { get; set; }
    public AnalyticalTestRequestDto AnalyticalTestRequest { get; set; }
    public string IssueNumber { get; set; }
    public UserDto IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
}