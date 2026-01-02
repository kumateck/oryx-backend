using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.ServiceProviders;

namespace DOMAIN.Entities.JobRequests;

public class ServiceQuotationDto : BaseDto
{
    public string QuotationNumber { get; set; }
    public Guid JobOrderId { get; set; }
    public ServiceProviderDto ServiceProvider { get; set; }
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
    public decimal TotalServiceCharge {get;set;}
    public decimal TotalItemCost { get; set; }
    public decimal GrandTotal { get; set; }
}

