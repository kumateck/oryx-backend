namespace DOMAIN.Entities.Inventory;

public class CreateInventoryRequest
{
    public string MaterialName { get; set; }
    public string Code { get; set; }
    public InventoryType Classification { get; set; }
    public string Type { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
}