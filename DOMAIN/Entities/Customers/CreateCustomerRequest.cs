using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Customers;

public class CreateCustomerRequest
{
    [Required, MinLength(3, ErrorMessage = "Client name must not be less than 3 characters")] public string Name { get; set; }
    
    [Required, EmailAddress] public string Email { get; set; }
    
    [Required, Phone] public string Phone { get; set; }
    
    [Required] public string Address { get; set; }
}