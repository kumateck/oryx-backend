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
    public UnitOfMeasureDto UoM { get; set; }
}