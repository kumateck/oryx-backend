using DOMAIN.Entities.Base;
using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocument : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    
}