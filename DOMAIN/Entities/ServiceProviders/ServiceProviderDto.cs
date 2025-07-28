using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.ServiceProviders;

public class ServiceProviderDto : BaseDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public Guid CountryId { get; set; }
    public CountryDto Country { get; set; }
    public Guid CurrencyId { get; set; }
    public CurrencyDto Currency { get; set; }
    public List<ServiceProviderDto> Services { get; set; }
}