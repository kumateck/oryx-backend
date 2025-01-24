using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Products;

public class ProductPackage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid PackageTypeId { get; set; }
    public PackageType PackageType { get; set; }
    [StringLength(255)] public string MaterialThickness { get; set; }
    [StringLength(255)] public string OtherStandards { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUoMId { get; set; } 
    public UnitOfMeasure BaseUoM { get; set; }
}

public class PackageType : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}