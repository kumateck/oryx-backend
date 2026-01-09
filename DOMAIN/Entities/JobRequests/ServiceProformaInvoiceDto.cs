using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ServiceProviders;
using SHARED;

namespace DOMAIN.Entities.JobRequests;

public class ServiceProformaInvoiceDto : BaseDto
{
    public string InvoiceNumber { get; set; }
    public CollectionItemDto JobOrder { get; set; }
    public ServiceQuotationDto ServiceQuotation { get; set; }
    public ServiceProviderDto ServiceProvider { get; set; }
    public DateTime RequestedDate { get; set; }
    public Guid RequestedById { get; set; }
    public DateTime? ResponseReceivedDate { get; set; }
    public string Notes { get; set; }
    public List<ServiceProformaInvoiceItemDto> Items { get; set; } = [];
    public decimal ServiceCharge { get; set; }
    public CurrencyDto Currency { get; set; }
    public decimal TotalCost { get; set; }
    public ServiceProformaInvoiceStatus Status { get; set; }
    public string ResponseNotes { get; set; }
    public string ProformaInvoiceDocumentUrl { get; set; }
}

public class ServiceProformaInvoiceItemDto : BaseDto
{
    public Guid ServiceProformaInvoiceId { get; set; }
    public ItemDto Item { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

