using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierContact : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string Role { get; set; }
    [EmailAddress, StringLength(255)] public string Email { get; set; }
    [StringLength(50)] public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}

public class SupplierContactRequest
{
    [Required, StringLength(255)] public string Name { get; set; }
    [StringLength(100)] public string Role { get; set; }
    [EmailAddress, StringLength(255)] public string Email { get; set; }
    [StringLength(50)] public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}

public class SupplierContactDto : BaseDto
{
    public Guid SupplierId { get; set; }
    public string Name { get; set; }
    public string Role { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public bool IsPrimary { get; set; }
}
