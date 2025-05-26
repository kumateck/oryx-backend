using DOMAIN.Entities.Countries;
using SHARED;

namespace APP.IRepository;

public interface ICountryRepository
{
    Task<Result<IEnumerable<CountryDto>>> GetCountries();
}