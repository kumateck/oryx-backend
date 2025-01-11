using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocument : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    [StringLength(255)] public string InvoiceNumber { get; set; }
}

public class ShipmentInvoice : BaseEntity
{
    public Guid ShipmentDocumentId { get; set; }
    public ShipmentDocument ShipmentDocument { get; set; }
    public List<ShipmentInvoiceItem> Items { get; set; } = [];
}

public class ShipmentInvoiceItem : BaseEntity
{
    public Guid ShipmentInvoiceId { get; set; }
    public ShipmentInvoice ShipmentInvoice { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    [StringLength(255)] public string Reason { get; set; }
}


public class ShipmentDiscrepancy : BaseEntity
{
    public Guid ShipmentDocumentId { get; set; }
    public ShipmentDocument ShipmentDocument { get; set; }
    public List<ShipmentDiscrepancyItem> Items { get; set; } = [];
}

public class ShipmentDiscrepancyItem : BaseEntity
{
    public Guid ShipmentDiscrepancyId { get; set; }
    public ShipmentDiscrepancy ShipmentDiscrepancy { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public ShipmentDiscrepancyType DiscrepancyType { get; set; }
    [StringLength(255)] public string Reason { get; set; }
    public bool Resolved { get; set; }
}

public enum ShipmentDiscrepancyType
{
    UnderDelivery = 0,
    OverDelivery = 1
}