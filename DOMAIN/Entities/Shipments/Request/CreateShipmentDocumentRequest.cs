using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Shipments.Request;

public class CreateShipmentDocumentRequest
{
    public string Code { get; set; }
    public string InvoiceNumber { get; set; }
    public Guid PurchaseOrderId { get; set; }
}

public class CreateShipmentInvoice 
{
    public Guid ShipmentDocumentId { get; set; }
    public List<CreateShipmentInvoiceItem> Items { get; set; } = [];
}

public class CreateShipmentInvoiceItem 
{
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    public Guid ManufacturerId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    [StringLength(255)] public string Reason { get; set; }
}


public class CreateShipmentDiscrepancy
{
    public Guid ShipmentDocumentId { get; set; }
    public List<ShipmentDiscrepancyItem> Items { get; set; } = [];
}

public class CreateShipmentDiscrepancyItem
{
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public Guid? TypeId { get; set; }
    [StringLength(255)] public string Reason { get; set; }
}