using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ProductSpecifications;

public class ProductSpecificationDto : BaseDto
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public string LabelClaim { get; set; } 
    public string PackingStyle { get; set; }
    public Guid FormId { get; set; }
    public FormDto Form { get; set; } 
    public DateTime DueDate {get;set;}
    public string Description {get;set;}

    public Guid UserId { get; set; }
    public UserDto User { get; set; }
    public string ShelfLife { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public TestStage TestStage { get; set; }
    public Guid ProductId { get; set; }
    public ProductDto Product { get; set; }
}