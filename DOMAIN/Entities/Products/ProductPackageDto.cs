using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductPackageDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Product { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public CollectionItemDto MaterialType { get; set; }
    public string MaterialThickness { get; set; }
    public string OtherStandards { get; set; }
}