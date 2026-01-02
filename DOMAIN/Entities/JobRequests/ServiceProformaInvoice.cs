using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Represents a proforma invoice request sent to a service provider after quotation selection
/// </summary>
public class ServiceProformaInvoice : BaseEntity
{
    [StringLength(100)]
    public string InvoiceNumber { get; set; }

    public Guid JobOrderId { get; set; }
    public JobOrder JobOrder { get; set; }

    public Guid ServiceQuotationId { get; set; }
    public ServiceQuotation ServiceQuotation { get; set; }

    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; }

    public DateTime RequestedDate { get; set; }

    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; }

    public DateTime? ResponseReceivedDate { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; }

    // Items/materials from the quotation
    public List<ServiceProformaInvoiceItem> Items { get; set; } = [];

    // Service charge
    public decimal ServiceCharge { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    // Total cost (service charge + sum of items)
    public decimal TotalCost => ServiceCharge + Items.Sum(i => i.TotalPrice);

    public ServiceProformaInvoiceStatus Status { get; set; } = ServiceProformaInvoiceStatus.Requested;

    [StringLength(2000)]
    public string ResponseNotes { get; set; }

    // File attachment for proforma invoice document
    [StringLength(1000)]
    public string ProformaInvoiceDocumentUrl { get; set; }
}

/// <summary>
/// Represents items/materials in a service proforma invoice
/// </summary>
public class ServiceProformaInvoiceItem : BaseEntity
{
    public Guid ServiceProformaInvoiceId { get; set; }
    public ServiceProformaInvoice ServiceProformaInvoice { get; set; }

    public Guid? ItemId { get; set; }
    public Item Item { get; set; }

    [StringLength(500)]
    public string ItemName { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;
}

public enum ServiceProformaInvoiceStatus
{
    Requested,
    ResponseReceived,
    Approved,
    Rejected
}

