using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Vendors;

public class CreateVendorRequest
{
    [Required, MinLength(3, ErrorMessage="Name should be at least 3 characters")]
    public string Name { get; set; }
    [Required] public string Address { get; set; }
    [Required, Phone] public string Phone { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required] public Guid CountryId { get; set; }
    [Required] public Guid CurrencyId { get; set; }
    [Required, MinLength(1, ErrorMessage="At least one vendor item must be selected")] 
    public List<Guid> VendorItemIds { get; set; }
}