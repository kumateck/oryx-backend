using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Countries;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CountryRepository(ApplicationDbContext context, IMapper mapper) : ICountryRepository
{
    public async Task<Result<IEnumerable<CountryDto>>> GetCountries()
    {
        var countries = await context.Countries.ToListAsync();
        var countryDtos = mapper.Map<IEnumerable<CountryDto>>(countries);

        return Result.Success(countryDtos);
    }
}
