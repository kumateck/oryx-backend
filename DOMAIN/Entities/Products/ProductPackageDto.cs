using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductPackageDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Product { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto PackageType { get; set; }
    public string MaterialThickness { get; set; }
    public string OtherStandards { get; set; }
    public decimal BaseQuantity { get; set; } 
    public UnitOfMeasureDto BaseUoM { get; set; }
    public decimal UnitCapacity { get; set; } 
    public CollectionItemDto DirectLinkMaterial { get; set; }
}