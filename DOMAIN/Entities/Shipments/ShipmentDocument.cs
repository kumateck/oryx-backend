using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;

namespace DOMAIN.Entities.Shipments;

public class ShipmentDocument : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public List<ShipmentDiscrepancy> Discrepancies { get; set; } = [];
    public Guid? ShipmentInvoiceId { get; set; }
    public ShipmentInvoice ShipmentInvoice { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime? TransitStartedAt { get; set; }
    public DocType Type { get; set; } 
    public DateTime? CompletedDistributionAt { get; set; }
    public ShipmentStatus Status { get; set; }
}

public enum ShipmentStatus
{
    New,
    InTransit,
    Cleared,
    Arrived
}

public enum DocType
{
    Shipment,
    Waybill
}

public class ShipmentInvoice : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public List<ShipmentInvoiceItem> Items { get; set; } = [];
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class ShipmentInvoiceItem : BaseEntity
{
    public Guid ShipmentInvoiceId { get; set; }
    public ShipmentInvoice ShipmentInvoice { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    //add manufacturing date
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    [StringLength(255)] public string Reason { get; set; }
    public bool Distributed { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class DistributionShipmentInvoiceItemDto 
{
    public List<ShipmentInvoiceItem> ShipmentInvoiceItems { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public UnitOfMeasure UoM { get; set; }
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
    public Guid? TypeId { get; set; }
    public ShipmentDiscrepancyType Type { get; set; }
    [StringLength(255)] public string Reason { get; set; }
    public bool Resolved { get; set; }
}

public class ShipmentDiscrepancyType : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}