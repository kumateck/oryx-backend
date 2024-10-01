using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Routes;

namespace DOMAIN.Entities.Products;

public class Product : BaseEntity
{
    [StringLength(255)] public string Code { get; set; } // Unique identifier for the product
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string Description { get; set; }
    public Guid CategoryId { get; set; } // e.g., Tablet, Syrup, Injectable
    public ProductCategory Category { get; set; }
    public List<FinishedProduct> FinishedProducts { get; set; }
    public List<ProductBillOfMaterial> BillOfMaterials { get; set; } // List of BOMs associated with this product
    public List<ProductPackage> Packages { get; set; } = [];
    public List<Route> Routes { get; set; } = [];
}

public class ProductCategory : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}

public class ProductBillOfMaterial : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; }  // The final product this BOM is for
    public Guid BillOfMaterialId { get; set; }
    public BillOfMaterial BillOfMaterial { get; set; }
    public int Quantity { get; set; }  // Quantity of the component needed
    public int Version { get; set; }   // Version of the BOM
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}