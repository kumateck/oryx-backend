using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Customers;

public class CustomerContact : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string Role { get; set; }
    [EmailAddress, StringLength(255)] public string Email { get; set; }
    [StringLength(50)] public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}

public class CustomerContactRequest
{
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string Role { get; set; }
    [EmailAddress, StringLength(255)] public string Email { get; set; }
    [StringLength(50)] public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}

public class CustomerContactDto : BaseDto
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}
