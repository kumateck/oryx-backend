using DOMAIN.Entities.Payments;
using DOMAIN.Entities.ProductionOrders;

namespace DOMAIN.Entities.Customers;

public class CustomerCreditStatusDto
{
    public Guid CustomerId { get; set; }
    public decimal? CreditLimit { get; set; }
    public Guid? PreferredCurrencyId { get; set; }
    public string PreferredCurrencyName { get; set; }
    public decimal? OutstandingInPreferredCurrency { get; set; }
    public decimal? AvailableCredit { get; set; }
    public decimal AdditionalOrderValue { get; set; }
    public bool IsWithinCreditLimit { get; set; }
    public List<PayableBalanceDto> OutstandingByCurrency { get; set; } = [];
}

public class CustomerOrderHistoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public ProductionOrderStatus Status { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerSummaryDto
{
    public Guid CustomerId { get; set; }
    public int LifetimeOrderCount { get; set; }
    public decimal TotalOrderValue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }
    public decimal? CurrentOutstandingBalance { get; set; }
    public Guid? PreferredCurrencyId { get; set; }
}
