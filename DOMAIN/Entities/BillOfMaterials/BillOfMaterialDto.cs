using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.BillOfMaterials;

public class BillOfMaterialDto
{
    public CollectionItemDto Product { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public List<BillOfMaterialItemDto> Items { get; set; } = [];
}

public class BillOfMaterialItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto MaterialType { get; set; }
    public decimal Quantity { get; set; } 
    public UnitOfMeasureDto UoM { get; set; }
    public bool IsSubstitutable { get; set; } 
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public string Function { get; set; }
    public decimal BaseQuantity { get; set; } 
    public UnitOfMeasureDto BaseUoM { get; set; }
    public int Order { get; set; }
}