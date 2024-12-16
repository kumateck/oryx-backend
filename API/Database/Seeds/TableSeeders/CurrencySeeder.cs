using APP.Utils;
using DOMAIN.Entities.Currencies;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class CurrencySeeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext.Currencies.Any()) return;

        SeedCurrencies(dbContext);
    }

    private void SeedCurrencies(ApplicationDbContext dbContext)
    {
        foreach (var currency in CurrencyUtils.All())
        {
            dbContext.Currencies.Add(new Currency
            {
                Name = currency.Name,
                Symbol = currency.Symbol
            });
        }
        
        dbContext.SaveChanges();
    }
}