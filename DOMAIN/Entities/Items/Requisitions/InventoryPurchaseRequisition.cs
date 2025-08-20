using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateInventoryPurchaseRequisition
{
    public string Code { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Remarks { get; set; }
    public List<CreateInventoryPurchaseRequisitionItem> Items { get; set; } = [];
}

public class CreateInventoryPurchaseRequisitionItem
{
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public class InventoryPurchaseRequisition : BaseEntity
{
    [StringLength(1000)] public string Code { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    [StringLength(10000)] public string Remarks { get; set; }
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
    public string Code { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string Remarks { get; set; }
    public InventoryPurchaseRequisitionStatus Status { get; set; }
    public List<InventoryPurchaseRequisitionItemDto> Items { get; set; } = [];
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
    public RequestStatus Status { get; set; }  
}

public class InventoryPurchaseRequisitionItemDto : BaseDto
{
    public CollectionItemDto InventoryPurchaseRequisition { get; set; }
    public ItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public RequestStatus Status { get; set; }  
}