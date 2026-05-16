using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.PayGroups;
using DOMAIN.Entities.Sites;

namespace DOMAIN.Entities.PayrollCompanies;

public class PayrollCompanyDto : BaseDto
{
    public string Name { get; set; }
    public string LegalName { get; set; }
    public Guid CountryId { get; set; }
    public CountryDto Country { get; set; }
    public Guid CurrencyId { get; set; }
    public CurrencyDto Currency { get; set; }
    public string Code { get; set; }
    public string StatutoryEmployeeId { get; set; }
    public Guid? OrganizationId { get; set; }
    public string TaxRegistrationNumber { get; set; }
    public bool IsActive { get; set; }
    public Guid? SiteId { get; set; }
    public SiteDto Site { get; set; }
    public Guid? PayGroupId { get; set; }
    public PayGroupDto PayGroup { get; set; }
}