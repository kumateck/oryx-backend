using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.JobRequests;

public class CreateServiceQuotationRequest
{
    [Required]
    public Guid JobOrderId { get; set; }

    [Required]
    public Guid ServiceProviderId { get; set; }

    [StringLength(100)]
    public string QuotationNumber { get; set; }

    [Required]
    public DateTime SubmittedDate { get; set; }

    public List<CreateServiceCharge> ServiceCharges { get; set; } = [];

    [Required]
    public Guid CurrencyId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int EstimatedDays { get; set; }

    [Required]
    public DateTime EstimatedCompletionDate { get; set; }

    [StringLength(2000)]
    public string Notes { get; set; }

    public List<CreateQuotationItemRequest> Items { get; set; } = [];
}

public class CreateServiceCharge
{
    public string Name { get; set; }
    public decimal Cost { get; set; }
}

public class CreateQuotationItemRequest
{
    public Guid? ItemId { get; set; }

    [Required, StringLength(500)]
    public string ItemName { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required]
    public Guid UnitOfMeasureId { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [StringLength(500)]
    public string Supplier { get; set; }
}

public class UpdateServiceQuotationRequest
{
    public decimal? ServiceCharge { get; set; }
    public int? EstimatedDays { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string Notes { get; set; }
}

public class NegotiateQuotationRequest
{
    [Required]
    public Guid QuotationId { get; set; }

    public decimal? NegotiatedServiceCharge { get; set; }

    public List<NegotiateQuotationItemRequest> NegotiatedItems { get; set; } = [];

    [StringLength(1000)]
    public string NegotiationNotes { get; set; }
}

public class NegotiateQuotationItemRequest
{
    [Required]
    public Guid QuotationItemId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal NegotiatedUnitPrice { get; set; }
}

public class SelectQuotationRequest
{
    [Required]
    public Guid JobOrderId { get; set; }

    [Required]
    public Guid QuotationId { get; set; }
}

public class CompareQuotationsRequest
{
    [Required]
    public Guid JobOrderId { get; set; }
}

