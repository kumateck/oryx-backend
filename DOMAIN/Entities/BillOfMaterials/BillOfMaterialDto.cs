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
    public CollectionItemDto ComponentMaterial { get; set; }
    public CollectionItemDto ComponentProduct { get; set; }
    public CollectionItemDto MaterialType { get; set; }
    public int Quantity { get; set; } 
    public CollectionItemDto UoM { get; set; }
    public bool IsSubstitutable { get; set; } 
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public string Function { get; set; }
    public int Order { get; set; }
}