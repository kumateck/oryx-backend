using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Vendors;

namespace DOMAIN.Entities.Items.Requisitions;

public class CreateSourceInventoryRequisition
{
    public Guid InventoryPurchaseRequisitionId { get; set; }
    public List<CreateSourceInventoryRequisitionItem> Items { get; set; } = [];
}

public enum InventoryRequisitionSource
{
    TrustedVendor,
    OpenMarket
}
public class CreateSourceInventoryRequisitionItem
{
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public decimal Quantity { get; set; }
    public InventoryRequisitionSource Source { get; set; }
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
    public Vendor Vendor { get; set; }
    [StringLength(10000)] public string Remarks { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
    public List<SourceInventoryRequisitionItem> Items { get; set; } = [];
}

public class SourceInventoryRequisitionDto : BaseDto
{
    public InventoryPurchaseRequisitionDto InventoryPurchaseRequisition { get; set; }
    public VendorDto Vendor { get; set; }
    public string Remarks { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
    public List<SourceInventoryRequisitionItemDto> Items { get; set; } = [];
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

public class SourceInventoryRequisitionItemDto : BaseDto
{
    public ItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class VendorQuotationRequest
{
    public VendorDto Vendor { get; set; }
    public DateTime? SentQuotationRequestAt { get; set; }
    public bool SentQuotationRequest => SentQuotationRequestAt is not null;
    public List<SourceInventoryRequisitionItemDto> Items { get; set; } = [];
}