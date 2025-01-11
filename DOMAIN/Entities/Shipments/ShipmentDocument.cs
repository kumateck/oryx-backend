using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocument : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    [StringLength(255)] public string InvoiceNumber { get; set; }
}

public class ShipmentDiscrepancy : BaseEntity
{
    public Guid ShipmentDocumentId { get; set; }
    public ShipmentDocument ShipmentDocument { get; set; }
    
}

public class ShipmentDiscrepancyItem : BaseEntity
{
    public Guid ShipmentDiscrepancyId { get; set; }
    public ShipmentDiscrepancy ShipmentDiscrepancy { get; set; }
    
}