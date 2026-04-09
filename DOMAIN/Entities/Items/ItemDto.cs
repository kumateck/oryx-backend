using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Items;

public class ItemDto : WithAttachment
{
    public string Name { get; set; }
    public string Code { get; set; }
    public InventoryClassification Classification { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public bool HasBatch { get; set; }
    public Store Store { get; set; }
    public int MinimumLevel { get; set; }
    public int MaximumLevel { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
    public ItemCategoryDto ItemCategory { get; set; }
    public int AvailableQuantity { get; set; }
}

public class StoreItemStockSummaryDto
{
    public int No { get; set; }                 
    public Store Store { get; set; }            
    public string ItemName { get; set; }
    public string ItemCode { get; set; }
    public string Category { get; set; }
    public decimal TotalQuantity { get; set; }
    public string UnitOfMeasure { get; set; }
}

public class VendorStoreItemStockSummaryDto : StoreItemStockSummaryDto
{
    public string VendorName { get; set; }   
    public InventoryClassification InventoryClassification { get; set; }
}

public class VendorItemStoreSummaryDto
{
    public int No { get; set; }
    public string ItemName { get; set; }
    public string ItemCode { get; set; }
    public string Category { get; set; }
    public InventoryClassification Classification { get; set; }
    public Store Store { get; set; }
    public int VendorCount { get; set; }
}