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
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; set; }
    [StringLength(255)] public string Grade { get; set; }
    [StringLength(255)] public string CasNumber { get; set; }
    public int Order { get; set; }
    public bool IsSubstitutable { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public UnitOfMeasure BaseUoM { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public bool UseDensityConversion { get; set; }
    public List<BillOfMaterialItemSubstitute> Substitutes { get; set; } = [];
}

public class BillOfMaterialItemSubstitute : BaseEntity
{
    public Guid BillOfMaterialItemId { get; set; }
    public BillOfMaterialItem BillOfMaterialItem { get; set; }
    public Guid SubstituteMaterialId { get; set; }
    public Material SubstituteMaterial { get; set; }
    /// <summary>
    /// The substitute's own quantity per unit of product, in <see cref="BaseUoM"/>. A substitute can differ in
    /// strength or water content from the main material, so it never borrows the main material's quantity.
    /// Null on substitutes saved before quantities existed; those cannot be used until the BOM is saved again.
    /// </summary>
    public decimal? BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public UnitOfMeasure BaseUoM { get; set; }
    /// <summary>The substitute's quantity for a full batch, in <see cref="BaseUoM"/>.</summary>
    public decimal? PrescribedQuantity { get; set; }
}
