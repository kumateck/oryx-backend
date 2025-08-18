using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocumentDto : WithAttachment
{
    public string Code { get; set; } 
    public ShipmentInvoiceDto ShipmentInvoice { get; set; }
    public List<ShipmentDiscrepancyDto> Discrepancies { get; set; } = [];
    public DateTime? ArrivedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime? TransitStartedAt { get; set; }
    public DocType Type { get; set; } 
    public ShipmentStatus Status { get; set; }
}

public class ShipmentInvoiceDto : BaseDto
{ 
    public string Code { get; set; }
    public SupplierDto Supplier { get; set; }
    public List<ShipmentInvoiceItemDto> Items { get; set; } = [];
    public bool IsAttached { get; set; }
    public decimal TotalCost { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class ShipmentInvoiceItemDto : BaseDto
{
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public CollectionItemDto Manufacturer { get; set; }
    public CollectionItemDto PurchaseOrder { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalCost { get; set; }
    public CurrencyDto Currency { get; set; }
    public string Reason { get; set; }
}


public class ShipmentDiscrepancyDto
{
    public ShipmentDocumentDto ShipmentDocument { get; set; }
    public List<ShipmentDiscrepancyItemDto> Items { get; set; } = [];
}

public class ShipmentDiscrepancyItemDto
{
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public CollectionItemDto Type { get; set; }
    public string Reason { get; set; }
    public bool Resolved { get; set; }
}