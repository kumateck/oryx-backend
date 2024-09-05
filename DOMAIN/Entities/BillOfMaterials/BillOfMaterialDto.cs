using SHARED;

namespace DOMAIN.Entities.BillOfMaterials;

public class BillOfMaterialDto
{
    public CollectionItemDto Product { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public List<BillOfMaterialItem> Items { get; set; } = [];
}

public class BillOfMaterialItemDto
{
    public Guid BillOfMaterialId { get; set; }
    public CollectionItemDto ComponentMaterial { get; set; }
    public CollectionItemDto ComponentProduct { get; set; }
    public int Quantity { get; set; }  // Quantity of the component required
    public CollectionItemDto UoM { get; set; }
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production  
}