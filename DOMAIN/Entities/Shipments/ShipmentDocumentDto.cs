using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.PurchaseOrders;
using SHARED;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocumentDto : WithAttachment
{
    public string Code { get; set; }
    public PurchaseOrderDto PurchaseOrder { get; set; }
    public string InvoiceNumber { get; set; }
    public List<ShipmentDiscrepancyDto> Discrepancies { get; set; } = [];
}

public class ShipmentInvoiceDto
{
    public Guid ShipmentDocumentId { get; set; }
    public ShipmentDocument ShipmentDocument { get; set; }
    public List<ShipmentInvoiceItemDto> Items { get; set; } = [];
}

public class ShipmentInvoiceItemDto
{
    public CollectionItemDto Material { get; set; }
    public CollectionItemDto UoM { get; set; }
    public CollectionItemDto Manufacturer { get; set; }
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
    public CollectionItemDto UoM { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public CollectionItemDto DType { get; set; }
    public string Reason { get; set; }
    public bool Resolved { get; set; }
}