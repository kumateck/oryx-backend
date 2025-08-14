using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ItemStockRequisitions;

namespace DOMAIN.Entities.Items;

public class Item : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; } 
    public InventoryClassification Classification { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    public bool HasBatch { get; set; }
    public string BatchNumber { get; set; }
    public int MinimumLevel { get; set; }
    public int MaximumLevel { get; set; }
    public int ReorderLevel { get; set; }
    public string Category { get; set; }
    public Store Store { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
    public int AvailableQuantity { get; set; } = 0;
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

public class ItemStockRequisitionItem : BaseEntity
{

    public Guid ItemStockRequisitionId { get; set; }
    public ItemStockRequisition ItemStockRequisition { get; set; }

    public Guid ItemId { get; set; }
    public Item Item { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int QuantityRequested { get; set; }
}
public class StockItems
{
    public Guid ItemId { get; set; }
    public int QuantityRequested { get; set; }
}

public class ItemStockRequisitionItemDto : BaseDto
{
    public Guid ItemStockRequisitionId { get; set; }
    public ItemStockRequisitionDto ItemStockRequisition { get; set; }

    public Guid ItemId { get; set; }
    public ItemDto Item { get; set; }
    
    public int QuantityRequested { get; set; }
}
