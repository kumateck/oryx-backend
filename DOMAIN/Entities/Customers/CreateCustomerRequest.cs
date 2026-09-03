using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DOMAIN.Entities.Customers;

public class CreateCustomerRequest
{
    private decimal? creditLimit;
    private Guid? termsOfPaymentId;
    private CustomerType? type;
    private Guid? currencyId;
    private string billingAddress;
    private string shippingAddress;

    [Required, MinLength(3, ErrorMessage = "Client name must not be less than 3 characters")] public string Name { get; set; }

    [Required, EmailAddress] public string Email { get; set; }

    [Required, Phone] public string Phone { get; set; }

    [Required] public string Address { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? CreditLimit
    {
        get => creditLimit;
        set { creditLimit = value; CreditLimitProvided = true; }
    }
    public Guid? TermsOfPaymentId
    {
        get => termsOfPaymentId;
        set { termsOfPaymentId = value; TermsOfPaymentIdProvided = true; }
    }
    public CustomerType? Type
    {
        get => type;
        set { type = value; TypeProvided = true; }
    }
    public Guid? CurrencyId
    {
        get => currencyId;
        set { currencyId = value; CurrencyIdProvided = true; }
    }
    [StringLength(2000)]
    public string BillingAddress
    {
        get => billingAddress;
        set { billingAddress = value; BillingAddressProvided = true; }
    }
    [StringLength(2000)]
    public string ShippingAddress
    {
        get => shippingAddress;
        set { shippingAddress = value; ShippingAddressProvided = true; }
    }

    [JsonIgnore] public bool CreditLimitProvided { get; private set; }
    [JsonIgnore] public bool TermsOfPaymentIdProvided { get; private set; }
    [JsonIgnore] public bool TypeProvided { get; private set; }
    [JsonIgnore] public bool CurrencyIdProvided { get; private set; }
    [JsonIgnore] public bool BillingAddressProvided { get; private set; }
    [JsonIgnore] public bool ShippingAddressProvided { get; private set; }
}
