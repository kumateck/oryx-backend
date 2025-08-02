using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;

namespace DOMAIN.Entities.Items;

public class Item : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; } 
    public InventoryClassification Classification { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public bool HasBatch { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public Store Store { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }

}

public enum Store
{
    IT,
    General,
    EquipmentStore
}

public class ItemType : BaseEntity
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