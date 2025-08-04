using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateMarketRequisition
{
    public Guid InventoryPurchaseRequisitionItemId { get; set; }
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
}

public class MarketRequisition : BaseEntity
{
    public Guid InventoryPurchaseRequisitionItemId { get; set; }
    public InventoryPurchaseRequisitionItem InventoryPurchaseRequisitionItem { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class MarketRequisitionDto
{
    public InventoryPurchaseRequisitionItemDto InventoryPurchaseRequisitionItem { get; set; }
    public ItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class CreateMarketRequisitionVendor
{
    public Guid MarketRequisitionId { get; set; }
    public string VendorName { get; set; }
    public string VendorAddress { get; set; } 
    public string VendorPhoneNumber { get; set; }
    public decimal PricePerUnit { get; set; }
    public string ModeOfPayment { get; set; }
    public Guid TermsOfPaymentId { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
}

public class MarketRequisitionVendor : BaseEntity
{
    public Guid MarketRequisitionId { get; set; }
    public MarketRequisition MarketRequisition { get; set; }
    [StringLength(1000)] public string VendorName { get; set; }
    [StringLength(10000)]  public string VendorAddress { get; set; }
    [StringLength(1000)]  public string VendorPhoneNumber { get; set; }
    public decimal PricePerUnit { get; set; }
    [StringLength(1000)] public string ModeOfPayment { get; set; }
    public Guid TermsOfPaymentId { get; set; }
    public TermsOfPayment TermsOfPayment { get; set; }
    [StringLength(1000)] public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public bool Complete { get; set; }
}

public class MarketRequisitionVendorDto : BaseDto
{
    
}