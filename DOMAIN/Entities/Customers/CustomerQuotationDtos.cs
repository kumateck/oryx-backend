using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Customers;

public class CreateCustomerQuotationRequest
{
    [Required, StringLength(100)] public string Code { get; set; }
    public DateTime ValidUntil { get; set; }
    [MinLength(1)] public List<CreateCustomerQuotationItemRequest> Items { get; set; } = [];
}

public class CreateCustomerQuotationItemRequest
{
    public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    public Guid ProductPackingId { get; set; }
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? UnitPrice { get; set; }
    [Range(typeof(decimal), "0", "100")] public decimal DiscountPercent { get; set; }
}

public class ResolvedQuotationPriceDto
{
    public decimal UnitPrice { get; set; }
    public bool FromAgreement { get; set; }
}

public class CustomerQuotationDto : BaseDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; }
    public CurrencyDto Currency { get; set; }
    public string Code { get; set; }
    public CustomerQuotationStatus Status { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool Approved { get; set; }
    public decimal TotalValue { get; set; }
    public List<CustomerQuotationItemDto> Items { get; set; } = [];
    public List<CustomerQuotationApprovalDto> Approvals { get; set; } = [];
}

public class CustomerQuotationItemDto : BaseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public Guid ProductPackingId { get; set; }
    public string ProductPackingName { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalValue { get; set; }
}

public class CustomerQuotationApprovalDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public bool Required { get; set; }
    public ApprovalStatus Status { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string Comments { get; set; }
}

public class CustomerQuotationApprovalRequest
{
    public ApprovalStatus Status { get; set; }
    [StringLength(1000)] public string Comments { get; set; }
}

public class ResolvedCustomerPriceDto
{
    public decimal UnitPrice { get; set; }
    public CurrencyDto Currency { get; set; }
    public Guid? PricingAgreementId { get; set; }
}
