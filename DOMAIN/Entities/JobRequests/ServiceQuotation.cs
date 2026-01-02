using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.ServiceProviders;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Represents a quotation submitted by a service provider
/// </summary>
public class ServiceQuotation : BaseEntity
{
    [StringLength(100)]
    public string QuotationNumber { get; set; }

    public Guid JobOrderId { get; set; }
    public JobOrder JobOrder { get; set; }

    public Guid ServiceProviderId { get; set; }
    public ServiceProvider ServiceProvider { get; set; }

    public DateTime SubmittedDate { get; set; }

    // Service charge
    public List<ServiceCharge> ServiceCharges { get; set; } = [];

    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }

    // Items/materials required
    public List<QuotationItem> Items { get; set; } = [];

    public decimal TotalServiceCharge => ServiceCharges.Sum(i => i.Cost);
    public decimal TotalItemCost => Items.Sum(i => i.TotalPrice);

    // Total cost (service charge + sum of items)
    public decimal GrandTotal => TotalServiceCharge +  TotalItemCost;

    // Estimated completion time
    public int EstimatedDays { get; set; }
    public DateTime EstimatedCompletionDate { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; }

    // Negotiation and selection
    public bool IsSelected { get; set; }
    public decimal? NegotiatedServiceCharge { get; set; }
    public decimal? NegotiatedTotalCost { get; set; }

    [StringLength(1000)]
    public string NegotiationNotes { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Submitted;
}

[Owned]
public class ServiceCharge
{
    [StringLength(1000)] public string Name { get; set; }
    public decimal Cost { get; set; }
}
public enum QuotationStatus
{
    Submitted,
    UnderReview,
    Negotiating,
    Selected,
    Rejected
}

