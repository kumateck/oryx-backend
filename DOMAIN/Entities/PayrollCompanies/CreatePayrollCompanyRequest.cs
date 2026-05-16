using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.PayrollCompanies;

public class CreatePayrollCompanyRequest
{
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters"), Required] 
    public string Name { get; set; }
    
    [MinLength(3, ErrorMessage = "Name must be at least 3 characters"), Required] 
    public string LegalName { get; set; }
    
    [Required] public Guid CountryId { get; set; }
    [Required] public Guid CurrencyId { get; set; }
    [Required, StringLength(50)] public string Code { get; set; }
    [StringLength(100)] public string StatutoryEmployerId { get; set; }
    public Guid? OrganizationId { get; set; }
    [StringLength(100)] public string TaxRegistrationNumber { get; set; }
    public bool IsActive { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? PayGroupId { get; set; }
}