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
    public int MinimumLevel { get; set; }
    public int MaximumLevel { get; set; }
    public int ReorderLevel { get; set; }
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

public enum InventoryClassification
{
    Recoverable,
    NonRecoverable
}
