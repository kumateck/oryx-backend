using DOMAIN.Entities.Countries;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class CountrySeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext != null)
        {
            SeedCountries(dbContext);
        }
    }

    private static void SeedCountries(ApplicationDbContext dbContext)
    {
        if (dbContext.Countries.Any()) return;

        var countries = new List<Country>();
        foreach (var country in CountryUtils.GetAllCountries())
        {
            var existingCountry = dbContext.Countries.FirstOrDefault(c => c.Name == country.Name);
            if (existingCountry is null)  
            {
                countries.Add(new Country
                {
                    Name = country.Name,
                    Nationality = country.Nationality,
                    Code = country.Code  
                });
            }
        }
        dbContext.Countries.AddRange(countries);
        dbContext.SaveChanges();
    }
}