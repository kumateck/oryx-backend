using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateInventoryPurchaseRequisition
{
    
}

public class InventoryPurchaseRequisition : BaseEntity
{
    public string Code { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Remarks { get; set; }
    public InventoryPurchaseRequisitionStatus Status { get; set; }
    public List<InventoryPurchaseRequisitionItem> Items { get; set; } = [];
}

public enum InventoryPurchaseRequisitionStatus
{
    Pending,
    Complete,
}

public class InventoryPurchaseRequisitionDto : BaseDto
{
    
}

public class InventoryPurchaseRequisitionItem : BaseEntity
{
    public Guid InventoryPurchaseRequisitionId { get; set; }
    public InventoryPurchaseRequisition InventoryPurchaseRequisition { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class InventoryPurchaseRequisitionItemDto : BaseDto
{
    
}