using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Routes;

namespace DOMAIN.Entities.Products;

public class Product : BaseEntity
{
    [StringLength(255)] public string Code { get; set; } 
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string GenericName { get; set; }
    [StringLength(255)] public string StorageCondition { get; set; }
    [StringLength(255)] public string PackageStyle { get; set; }
    [StringLength(255)] public string FilledWeight { get; set; }
    [StringLength(255)] public string ShelfLife { get; set; }
    [StringLength(255)] public string ActionUse { get; set; }
    [StringLength(255)] public string Description { get; set; }
    [StringLength(255)] public string FdaRegistrationNumber { get; set; }
    [StringLength(255)] public string MasterFormulaNumber { get; set; }
    [StringLength(1000000)] public string PrimaryPackDescription { get; set; }
    [StringLength(1000000)] public string SecondaryPackDescription { get; set; }
    [StringLength(1000000)] public string TertiaryPackDescription { get; set; }
    public Guid CategoryId { get; set; }
    public ProductCategory Category { get; set; }
    public decimal BaseQuantity { get; set; } 
    public decimal BasePackingQuantity { get; set; } 
    public Guid? BaseUomId { get; set; }
    public UnitOfMeasure BaseUoM { get; set; }
    public Guid? BasePackingUomId { get; set; }
    public UnitOfMeasure BasePackingUoM { get; set; }
    public Guid? EquipmentId { get; set; }
    public Equipment Equipment { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
    public List<FinishedProduct> FinishedProducts { get; set; } = [];
    public List<ProductBillOfMaterial> BillOfMaterials { get; set; } = [];
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
    public decimal Quantity { get; set; }  // Quantity of the component needed
    public int Version { get; set; }   // Version of the BOM
    public DateTime EffectiveDate { get; set; }
    public bool IsActive { get; set; } = true;
}