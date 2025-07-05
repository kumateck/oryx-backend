using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductSpecifications;

public class ProductSpecificationDto : BaseDto
{
    public Guid ProductId { get; set; }
    public ProductDto Product { get; set; }
}