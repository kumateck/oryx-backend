using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Customers;

public class CreateCustomerRequest
{
    [Required, MinLength(3, ErrorMessage = "Client name must not be less than 3 characters")] public string Name { get; set; }

    [Required, EmailAddress] public string Email { get; set; }

    [Required, Phone] public string Phone { get; set; }

    [Required] public string Address { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? CreditLimit { get; set; }
    public Guid? TermsOfPaymentId { get; set; }
    public CustomerType? Type { get; set; }
    public Guid? CurrencyId { get; set; }
    [StringLength(2000)] public string BillingAddress { get; set; }
    [StringLength(2000)] public string ShippingAddress { get; set; }
}
