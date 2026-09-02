using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Customers;

public class CustomerDto : BaseDto
{
    public string Name { get; set; }

    public string Email { get; set; }

    public string Phone { get; set; }

    public string Address { get; set; }
    public decimal? CreditLimit { get; set; }
    public TermsOfPaymentDto TermsOfPayment { get; set; }
    public CustomerType? Type { get; set; }
    public CurrencyDto Currency { get; set; }
    public string BillingAddress { get; set; }
    public string ShippingAddress { get; set; }
}
