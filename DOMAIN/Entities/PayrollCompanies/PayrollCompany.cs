using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Organizations;
using DOMAIN.Entities.PayGroups;
using DOMAIN.Entities.Sites;

namespace DOMAIN.Entities.PayrollCompanies;

public class PayrollCompany : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string LegalName { get; set; }
    public Guid CountryId { get; set; }
    public Country Country { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    [StringLength(50)] public string Code { get; set; }
    [StringLength(100)] public string StatutoryEmployerId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization Organization { get; set; }
    [StringLength(100)] public string TaxRegistrationNumber { get; set; }
    public bool IsActive { get; set; }
    public Guid? SiteId { get; set; }
    public Site Site { get; set; }
    public List<PayGroup> PayGroups { get; set; }
}