using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Charges;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Vendors;

namespace DOMAIN.Entities.ItemShipments;

// ========== Item Shipment Invoice ==========

public class ItemShipmentInvoice : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor Vendor { get; set; }
    public List<ItemShipmentInvoiceItem> Items { get; set; } = [];
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
    public DateTime ShipmentArrivedAt { get; set; }
    public Currency Currency { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class ItemShipmentInvoiceItem : BaseEntity
{
    public Guid ItemShipmentInvoiceId { get; set; }
    public ItemShipmentInvoice ItemShipmentInvoice { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    [StringLength(255)] public string Reason { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

// ========== Item Shipment Document ==========

public class ItemShipmentDocument : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid? ItemShipmentInvoiceId { get; set; }
    public ItemShipmentInvoice ItemShipmentInvoice { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime? TransitStartedAt { get; set; }
    public DateTime? AtPortAt { get; set; }
    public DocType Type { get; set; }
    public ShipmentStatus Status { get; set; }
    public bool Approved { get; set; }
}

// ========== Item Billing Sheet ==========

public class ItemBillingSheet : BaseEntity
{
    [StringLength(1000)] public string Code { get; set; }
    [StringLength(1000)] public string BillOfLading { get; set; }
    public Guid? VendorId { get; set; }
    public Vendor Vendor { get; set; }
    public Guid InvoiceId { get; set; }
    public ItemShipmentInvoice Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    [StringLength(100)] public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public BillingSheetStatus Status { get; set; }
    [StringLength(100)] public string ContainerNumber { get; set; }
    [StringLength(1000)] public string NumberOfPackages { get; set; }
    public Guid? ContainerPackageStyleId { get; set; }
    public PackageStyle ContainerPackageStyle { get; set; }
    [StringLength(1000)] public string PackageDescription { get; set; }
    public List<ItemBillingSheetCharge> Charges { get; set; } = [];
}

public class ItemBillingSheetCharge
{
    public Guid Id { get; set; }
    public Guid ChargeId { get; set; }
    public Charge Charge { get; set; }
    public Guid ItemBillingSheetId { get; set; }
    public ItemBillingSheet ItemBillingSheet { get; set; }
    public bool Paid { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
    public Guid? LastUpdatedById { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
}

// ========== DTOs ==========

public class ItemShipmentInvoiceDto : BaseDto
{
    public string Code { get; set; }
    public SupplierDto Supplier { get; set; }
    public List<ItemShipmentInvoiceItemDto> Items { get; set; } = [];
    public decimal TotalCost { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsUsed { get; set; }
}

public class ItemShipmentInvoiceListDto : BaseDto
{
    public string Code { get; set; }
    public SupplierListDto Supplier { get; set; }
    public decimal TotalCost { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsUsed { get; set; }
}

public class ItemShipmentInvoiceItemDto : BaseDto
{
    public ItemDto Item { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public CurrencyDto Currency { get; set; }
    public string Reason { get; set; }
}

public class ItemShipmentDocumentDto : BaseDto
{
    public string Code { get; set; }
    public ItemShipmentInvoiceDto ItemShipmentInvoice { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime? TransitStartedAt { get; set; }
    public DocType Type { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime? AtPortAt { get; set; }
    public bool Approved { get; set; }
    public bool HasBillingSheet { get; set; }
}

public class ItemBillingSheetDto : BaseDto
{
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public SupplierDto Supplier { get; set; }
    public ItemShipmentInvoiceDto Invoice { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public BillingSheetStatus Status { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public List<ItemBillingSheetChargeDto> Charges { get; set; } = [];
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; }
    public string PackageDescription { get; set; }
}

public class ItemBillingSheetChargeDto
{
    public Guid Id { get; set; }
    public ChargeDto Charge { get; set; }
    public CurrencyDto Currency { get; set; }
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
}

// ========== Requests ==========

public class CreateItemShipmentInvoice
{
    public string Code { get; set; }
    public Guid? VendorId { get; set; }
    
    public DateTime ShipmentArrivedAt { get; set; }
    public List<CreateItemShipmentInvoiceItem> Items { get; set; } = [];
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
}

public class CreateItemShipmentInvoiceItem
{
    public Guid ItemId { get; set; }
    public Guid UoMId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    [StringLength(255)] public string Reason { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
}

public class CreateItemShipmentDocumentRequest
{
    public string Code { get; set; }
    public Guid? ItemShipmentInvoiceId { get; set; }
}

public class CreateItemBillingSheetRequest : UpdateItemBillingSheetRequest
{
    public List<CreateBillingSheetCharge> Charges { get; set; } = [];
}

public class UpdateItemBillingSheetRequest
{
    public string Code { get; set; }
    public string BillOfLading { get; set; }
    public Guid? VendorId { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime ExpectedArrivalDate { get; set; }
    public DateTime FreeTimeExpiryDate { get; set; }
    public string FreeTimeDuration { get; set; }
    public DateTime DemurrageStartDate { get; set; }
    public string ContainerNumber { get; set; }
    public string NumberOfPackages { get; set; }
    public string PackageDescription { get; set; }
    public Guid? ContainerPackageStyleId { get; set; }
}