using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PurchaseOrders;
using SHARED;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocumentDto : WithAttachment
{
    public string Code { get; set; }
    public ShipmentInvoiceDto ShipmentInvoice { get; set; }
    public List<ShipmentDiscrepancyDto> Discrepancies { get; set; } = [];
}

public class ShipmentInvoiceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public DateTime? ShipmentArrivedAt { get; set; }
    public List<ShipmentInvoiceItemDto> Items { get; set; } = [];
}

public class ShipmentInvoiceItemDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public CollectionItemDto Manufacturer { get; set; }
    public CollectionItemDto PurchaseOrder { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
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