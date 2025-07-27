using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Inventory;

public class Inventory : BaseEntity
{
    public string MaterialName { get; set; }
    public string Code { get; set; }
    public InventoryType Classification { get; set; }
    public string Type { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }

}

public enum InventoryType
{
    Recoverable,
    NonRecoverable
}

public enum ReorderRules
{
    Minimum,
    Reorder,
    Maximum
}