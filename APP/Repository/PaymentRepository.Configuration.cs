using System.Data;
using DOMAIN.Entities.Currencies;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository
{
    public async Task<Result> SetBaseCurrency(Guid currencyId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        var target = await context.Currencies.FirstOrDefaultAsync(currency => currency.Id == currencyId);
        if (target is null)
            return Error.NotFound("Currency.NotFound", "Currency not found.");

        var current = await context.Currencies.Where(currency => currency.IsBaseCurrency).ToListAsync();
        foreach (var currency in current)
            currency.IsBaseCurrency = false;
        target.IsBaseCurrency = true;
        await context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> AddExchangeRate(CreateExchangeRateRequest request, Guid userId)
    {
        if (request.RateToBase <= 0)
            return Error.Validation("ExchangeRate.Rate", "Exchange rate must be greater than zero.");
        var currency = await context.Currencies.FirstOrDefaultAsync(item => item.Id == request.CurrencyId);
        if (currency is null)
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        if (currency.IsBaseCurrency && request.RateToBase != 1m)
            return Error.Validation("ExchangeRate.BaseCurrency", "The base currency rate must be 1.");
        if (await context.ExchangeRates.AnyAsync(rate =>
                rate.CurrencyId == request.CurrencyId && rate.EffectiveDate == request.EffectiveDate))
            return Error.Conflict("ExchangeRate.Duplicate", "A rate already exists for this effective date.");

        var rate = new ExchangeRate
        {
            CurrencyId = request.CurrencyId,
            RateToBase = request.RateToBase,
            EffectiveDate = request.EffectiveDate,
            CreatedById = userId,
        };
        await context.ExchangeRates.AddAsync(rate);
        await context.SaveChangesAsync();
        return rate.Id;
    }

    public async Task<Result<ExchangeRateDto>> GetExchangeRate(Guid currencyId, DateTime asOf)
    {
        var resolved = await ResolveRate(currencyId, asOf);
        return resolved.IsSuccess ? resolved.Value : resolved.Error;
    }

    private async Task<Result<ExchangeRateDto>> ResolveRate(Guid currencyId, DateTime asOf)
    {
        var currency = await context.Currencies.AsNoTracking().FirstOrDefaultAsync(item => item.Id == currencyId);
        if (currency is null)
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        if (currency.IsBaseCurrency)
            return new ExchangeRateDto
            {
                Currency = mapper.Map<CurrencyDto>(currency),
                RateToBase = 1m,
                EffectiveDate = asOf,
            };

        var rate = await context.ExchangeRates.AsNoTracking()
            .Where(item => item.CurrencyId == currencyId && item.EffectiveDate <= asOf)
            .OrderByDescending(item => item.EffectiveDate)
            .FirstOrDefaultAsync();
        if (rate is null)
            return Error.Validation(
                "ExchangeRate.Missing",
                $"No exchange rate exists for {currency.Name} as of {asOf:yyyy-MM-dd}."
            );
        return new ExchangeRateDto
        {
            Id = rate.Id,
            CreatedAt = rate.CreatedAt,
            Currency = mapper.Map<CurrencyDto>(currency),
            RateToBase = rate.RateToBase,
            EffectiveDate = rate.EffectiveDate,
        };
    }
}
