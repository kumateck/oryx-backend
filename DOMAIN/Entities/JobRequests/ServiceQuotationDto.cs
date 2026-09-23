using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.ServiceProviders;
using DOMAIN.Entities.Services;
using SHARED;

namespace DOMAIN.Entities.JobRequests;

public class ServiceQuotationDto : BaseDto
{
    public string QuotationNumber { get; set; }
    public Guid JobOrderId { get; set; }
    public JobOrderStatus JobOrderStatus { get; set; }
    public ServiceDto Service { get; set; }
    public ServiceProviderReducedDto ServiceProvider { get; set; }
    public DateTime SubmittedDate { get; set; }
    public decimal ServiceCharge { get; set; }
    public CurrencyDto Currency { get; set; }
    public List<QuotationItemDto> Items { get; set; } = [];
    public decimal TotalCost { get; set; }
    public int EstimatedDays { get; set; }
    public DateTime EstimatedCompletionDate { get; set; }
    public string Notes { get; set; }
    public bool IsSelected { get; set; }
    public decimal? NegotiatedServiceCharge { get; set; }
    public decimal? NegotiatedTotalCost { get; set; }
    public string NegotiationNotes { get; set; }
    public QuotationStatus Status { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public decimal TotalItemCost { get; set; }
    public decimal GrandTotal { get; set; }
    public List<ServiceCharge> ServiceCharges { get; set; } = [];
}

public class ServiceQuotationReducedDto : BaseDto
{
    public string QuotationNumber { get; set; }
    public Guid JobOrderId { get; set; }
    public ServiceDto Service { get; set; }
    public CollectionItemDto ServiceProvider { get; set; }
    public DateTime SubmittedDate { get; set; }
    public decimal ServiceCharge { get; set; }
    public CurrencyDto Currency { get; set; }
    public decimal TotalCost { get; set; }
    public int EstimatedDays { get; set; }
    public DateTime EstimatedCompletionDate { get; set; }
    public string Notes { get; set; }
    public bool IsSelected { get; set; }
    public decimal? NegotiatedServiceCharge { get; set; }
    public decimal? NegotiatedTotalCost { get; set; }
    public string NegotiationNotes { get; set; }
    public QuotationStatus Status { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public decimal TotalItemCost { get; set; }
    public decimal GrandTotal { get; set; }
}

public class ServiceChargeDto
{ 
    public string Name { get; set; }
    public decimal Cost { get; set; }
    public string PriceUoM { get; set; }
}
