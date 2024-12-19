namespace DOMAIN.Entities.Shipments.Request;

public class CreateShipmentDocumentRequest
{
    public string Code { get; set; }
    public string InvoiceNumber { get; set; }
}