using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateSourceInventoryRequisition
{
    public Guid InventoryPurchaseRequisitionId { get; set; }
    public List<CreateSourceInventoryRequisitionItem> Items { get; set; } = [];
}

public enum PurchaseRequisitionSource
{
    TrustedVendor,
    OpenMarket
}
public class CreateSourceInventoryRequisitionItem
{
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
    public PurchaseRequisitionSource Source { get; set; }
    public List<CreateSourceInventoryRequisitionItemVendor> Vendors { get; set; } = [];
}

public class CreateSourceInventoryRequisitionItemVendor
{
    public Guid VendorId { get; set; }
}


public class SourceInventoryRequisition : BaseEntity
{
    public Guid InventoryPurchaseRequisitionId { get; set; }
    public InventoryPurchaseRequisition InventoryPurchaseRequisition { get; set; }
    public Guid VendorId { get; set; }
    //public Vendor Vendor { get; set; }
    public string Remarks { get; set; }
    public List<SourceInventoryRequisitionItem> Items { get; set; } = [];
}

public class SourceInventoryRequisitionItem : BaseEntity
{
    public Guid SourceInventoryPurchaseRequisitionId { get; set; }
    public SourceInventoryRequisition SourceInventoryRequisition { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}