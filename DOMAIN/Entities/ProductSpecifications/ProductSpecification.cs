using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductSpecifications;

public class ProductSpecification : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
}