using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Services;

namespace DOMAIN.Entities.ServiceProviders;

public class ServiceProvider : BaseEntity
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public Guid CountryId { get; set; }
    public Country Country { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public List<Service> Services { get; set; } = [];
}