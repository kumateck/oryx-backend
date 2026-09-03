using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Customers;

public class Customer : BaseEntity
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public string Address { get; set; }

    public decimal? CreditLimit { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public TermsOfPayment TermsOfPayment { get; set; }
    public CustomerType? Type { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public string BillingAddress { get; set; }
    public string ShippingAddress { get; set; }
    public List<CustomerContact> Contacts { get; set; } = [];
    public List<CustomerPricingAgreement> PricingAgreements { get; set; } = [];
}

public enum CustomerType
{
    Distributor = 0,
    Pharmacy = 1,
    Hospital = 2,
    Retail = 3,
    Other = 4,
}
