using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.PurchaseOrders;
using SHARED;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocumentDto : WithAttachment
{
    public string Code { get; set; }
    public PurchaseOrderDto PurchaseOrder { get; set; }
    public string InvoiceNumber { get; set; }
}