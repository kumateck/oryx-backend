using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateMarketRequisition
{
    
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
    
}

public class CreateMarketRequisitionVendor
{
    
}

public class MarketRequisitionVendor : BaseEntity
{
    public Guid MarketRequisitionId { get; set; }
    public MarketRequisition MarketRequisition { get; set; }
    public string VendorName { get; set; }
    public string VendorAddress { get; set; }
    public string VendorPhoneNumber { get; set; }
    public decimal PricePerUnit { get; set; }
    public string ModeOfPayment { get; set; }
    public bool Complete { get; set; }
}

public class MarketRequisitionVendorDto : BaseDto
{
    
}