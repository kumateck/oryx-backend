using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;

namespace DOMAIN.Entities.Shipments;

public class ShipmentShortageReceipt : BaseEntity
{
    public Guid ShipmentInvoiceItemId { get; set; }
    public ShipmentInvoiceItem ShipmentInvoiceItem { get; set; }
    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; }
    public decimal PhysicalQuantity { get; set; }
}
