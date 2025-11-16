using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductsSampling;

public class ProductSampling : BaseEntity
{
    [StringLength(1000000)] public string ArNumber { get; set; }
    public Guid AnalyticalTestRequestId { get; set; }
    public decimal SampleQuantity { get; set; }
    public int ContainersSampled { get; set; }
    public DateTime SampleDate { get; set; } = DateTime.UtcNow;
    public AnalyticalTestRequest AnalyticalTestRequest { get; set; }
    [StringLength(1000000)] public string IssueNumber { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
}