using DOMAIN.Entities.Attachments;
using SHARED;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocumentDto : WithAttachment
{
    public string Code { get; set; }
    public CollectionItemDto PurchaseOrder { get; set; }
    public string InvoiceNumber { get; set; }
}