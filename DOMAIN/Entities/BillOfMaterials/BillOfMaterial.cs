using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.BillOfMaterials;

public class BillOfMaterial : BaseEntity
{
    public Guid ProductId { get; set; } 
    public Product Product { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public List<BillOfMaterialItem> Items { get; set; } = [];
}

public class BillOfMaterialItem : BaseEntity
{
    public Guid BillOfMaterialId { get; set; }
    public BillOfMaterial BillOfMaterial { get; set; }

    public Guid? ComponentMaterialId { get; set; }
    public Material ComponentMaterial { get; set; }

    public Guid? ComponentProductId { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; set; }
    public Product ComponentProduct { get; set; }
    [StringLength(255)] public string Grade { get; set; }
    [StringLength(255)] public string CasNumber { get; set; }
    [StringLength(255)] public string Function { get; set; }
    public int Order { get; set; }
    public decimal Quantity { get; set; } 
    public Guid? UoMId { get; set; } 
    public UnitOfMeasure UoM { get; set; }
    public bool IsSubstitutable { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUoMId { get; set; } 
    public UnitOfMeasure BaseUoM { get; set; }
}