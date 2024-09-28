using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Products;

public class ProductPackage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; set; }
    public Guid? ProductPackageTypeId { get; set; }
    public ProductPackageType ProductPackageType { get; set; }
   [StringLength(255)] public string MaterialThickness { get; set; }
   [StringLength(255)] public string OtherStandards { get; set; }
}

public class ProductPackageType : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}