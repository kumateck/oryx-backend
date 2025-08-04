using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Items.Requisitions;
using DOMAIN.Entities.Vendors;
using SHARED;

namespace DOMAIN.Entities.VendorQuotations;

public class VendorQuotation : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } 
    public Guid SourceInventoryRequisitionId { get; set; }
    public SourceInventoryRequisition SourceInventoryRequisition { get; set; }
    public List<VendorQuotationItem> Items { get; set; } = [];
    public bool ReceivedQuotation { get; set; }
}

public class VendorQuotationDto : BaseDto
{
    public VendorDto Vendor { get; set; } 
    public SourceInventoryRequisition SourceInventoryRequisition { get; set; }
    public List<VendorQuotationItem> Items { get; set; } = [];
    public bool ReceivedQuotation { get; set; }
}

public class VendorQuotationItem : BaseEntity
{
    public Guid VendorQuotationId { get; set; }    
    public VendorQuotation VendorQuotation { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal? QuotedPrice { get; set; }
    [StringLength(1000)] public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public VendorQuotationItemStatus Status { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public TermsOfPayment TermsOfPayment { get; set; }
    public Guid? InventoryPurchaseOrderId { get; set; }
}

public enum VendorQuotationItemStatus
{
    NotProcessed,
    Processed,
    NotUsed
}

public class VendorQuotationItemDto
{
    public ItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal? QuotedPrice { get; set; }
    public CollectionItemDto TermsOfPayment { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
}

public class VendorQuotationResponseDto
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
}

public class VendorPriceComparison
{
    public CollectionItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public List<VendorPrice> VendorPrices { get; set; } = [];
}

public class VendorPrice
{
    public CollectionItemDto Vendor { get; set; }
    public VendorQuotationItemDto VendorQuotationItem { get; set; }
    public VendorQuotationItemStatus? Status { get; set; }
    public string VendorName { get; set; }
    public string VendorAddress { get; set; }
    public string VendorPhoneNumber { get; set; }
    public decimal PricePerUnit { get; set; }
    public string ModeOfPayment { get; set; }
    public CollectionItemDto OpenMarketTermsOfPayment { get; set; }
    public string DeliveryMode { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
}