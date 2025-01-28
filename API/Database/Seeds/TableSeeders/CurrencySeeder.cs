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
        var currencies = new List<Currency>();
        foreach (var currency in CurrencyUtils.All())
        {
            currencies.Add(new Currency
            {
                Name = currency.Name,
                Symbol = currency.Symbol
            });
        }
        dbContext.Currencies.AddRange(currencies);
        dbContext.SaveChanges();
    }
}