using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Services;

namespace DOMAIN.Entities.ServiceProviders;

public class ServiceProviderDto : BaseDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public CountryDto Country { get; set; }
    public CurrencyDto Currency { get; set; }
    public List<ServiceDto> Services { get; set; } = [];
}