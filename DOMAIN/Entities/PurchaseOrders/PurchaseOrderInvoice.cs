using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Procurement.Manufacturers;
using SHARED;

namespace DOMAIN.Entities.PurchaseOrders;

public class PurchaseOrderInvoice : BaseEntity
{
    [StringLength(100)] public string Code { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public List<BatchItem> BatchItems { get; set; } = [];
    public List<PurchaseOrderCharge> Charges { get; set; } = [];
}

public class BatchItem : BaseEntity
{
    [StringLength(1000)] public string BatchNumber { get; set; }
    public Guid PurchaseOrderInvoiceId { get; set; }
    public PurchaseOrderInvoice PurchaseOrderInvoice { get; set; }
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public int Quantity { get; set; }
}

public class PurchaseOrderCharge : BaseEntity
{
    public Guid PurchaseOrderInvoiceId { get; set; }
    public PurchaseOrderInvoice PurchaseOrderInvoice { get; set; }
    [StringLength(10000)] public string Description { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseOrderInvoiceDto : BaseDto
{
    public string Code { get; set; }
    public CollectionItemDto PurchaseOrder { get; set; }
    public List<BatchItemDto> BatchItems { get; set; } = [];
    public List<PurchaseOrderChargeDto> Charges { get; set; } = [];
}

public class BatchItemDto
{
    public string BatchNumber { get; set; }
    public CollectionItemDto PurchaseOrderInvoice { get; set; }
    public CollectionItemDto Manufacturer { get; set; }
    public int Quantity { get; set; }
}

public class PurchaseOrderChargeDto
{
    public PurchaseOrderInvoiceDto PurchaseOrderInvoice { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Currency { get; set; }
    public decimal Amount { get; set; }
}