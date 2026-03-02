namespace DOMAIN.Entities.Reports.GeneralInventory;

public class GeneralInventoryDashboardDto
{
    public ItemCountDto ItemCounts { get; set; }
    public ItemStockRequisitionCountDto ItemStockRequisitions { get; set; }
    public int Totalvendors { get; set; }
    
}

public class ItemCountDto
{
    public int EquipmentStoreCount { get; set; }
    public int GeneralSoreCount { get; set; }
    public int ItStoreCount { get; set; }
    public int ReagentStoreCount { get; set; }
    public int Total=> EquipmentStoreCount + GeneralSoreCount + ItStoreCount + ReagentStoreCount;
}

public class ItemStockRequisitionCountDto
{
   public int PendingCount { get; set; }
   public int PartialCount { get; set; }
   public int CompletedCount { get; set; }
   public int Total=> PendingCount + PartialCount + CompletedCount;
}

public class ItemBelowReorderDto
{
    public string ItemName { get; set; }
    public string Code { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal ReorderLevel { get; set; }
}