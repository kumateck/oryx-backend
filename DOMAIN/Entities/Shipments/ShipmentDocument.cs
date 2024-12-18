using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocument : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    [StringLength(255)] public string InvoiceNumber { get; set; }
}