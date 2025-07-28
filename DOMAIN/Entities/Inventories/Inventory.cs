using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Inventories;

public class Inventory : BaseEntity
{
    public string MaterialName { get; set; }
    public string Code { get; set; } 
    public InventoryClassification Classification { get; set; }
    public Guid InventoryTypeId { get; set; }
    public InventoryType InventoryType { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public bool HasBatch { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }

}

public class InventoryType : BaseEntity
{
    public string Name { get; set; }
}

public enum InventoryClassification
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