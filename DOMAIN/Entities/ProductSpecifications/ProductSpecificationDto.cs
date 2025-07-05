using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductSpecifications;

public class ProductSpecificationDto : BaseDto
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public List<TestSpecification> TestSpecifications { get; set; } = [];
    public TestStage TestStage { get; set; }
    public Guid ProductId { get; set; }
    public ProductDto Product { get; set; }
}