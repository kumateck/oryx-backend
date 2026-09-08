using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository
{
    public async Task<Result<PaymentListDto>> GetPayments(PaymentListRequest request)
    {
        if (request.Page < 1 || request.Page > 1000000 || request.PageSize < 1 || request.PageSize > 100
            || request.SearchQuery?.Length > 255
            || (request.Status.HasValue && !Enum.IsDefined(request.Status.Value)))
            return Error.Validation("Payment.Filters", "Invalid payment list filters.");

        var query = context.Payments.AsNoTracking();
        if (request.Status.HasValue)
            query = query.Where(payment => payment.Status == request.Status.Value);
        if (request.IsReceipt.HasValue)
            query = request.IsReceipt.Value
                ? query.Where(payment => payment.PayableType == PayableType.CustomerInvoice)
                : query.Where(payment => payment.PayableType != PayableType.CustomerInvoice);
        var search = request.SearchQuery?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(payment => payment.Reference.Contains(search));

        var total = await query.CountAsync();
        var data = await query.OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.CreatedAt).ThenBy(payment => payment.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(payment => new PaymentListItemDto
            {
                Id = payment.Id, Amount = payment.Amount, PaymentDate = payment.PaymentDate,
                Reference = payment.Reference, Method = payment.Method,
                PayableType = payment.PayableType, PayableId = payment.PayableId,
                Status = payment.Status,
                Currency = new CurrencyDto
                {
                    Id = payment.Currency.Id, Name = payment.Currency.Name,
                    Symbol = payment.Currency.Symbol, IsBaseCurrency = payment.Currency.IsBaseCurrency,
                },
            }).ToListAsync();
        return new PaymentListDto
        {
            Data = data, PageIndex = request.Page, TotalRecordCount = total,
            PageCount = (int)Math.Ceiling(total / (double)request.PageSize),
        };
    }

    public async Task<Result<CashflowCurrencyDto>> GetCurrencyConfiguration(DateTime? asOf = null)
    {
        var date = asOf ?? DateTime.UtcNow;
        var currencies = await context.Currencies.AsNoTracking().OrderBy(currency => currency.Name).ToListAsync();
        var rates = await context.ExchangeRates.AsNoTracking()
            .Where(rate => rate.EffectiveDate <= date)
            .GroupBy(rate => rate.CurrencyId)
            .Select(group => group.OrderByDescending(rate => rate.EffectiveDate).First())
            .ToDictionaryAsync(rate => rate.CurrencyId);
        var baseCurrency = currencies.FirstOrDefault(currency => currency.IsBaseCurrency);
        return new CashflowCurrencyDto
        {
            AsOf = date,
            BaseCurrency = baseCurrency is null ? null : mapper.Map<CurrencyDto>(baseCurrency),
            Rates = currencies.Select(currency =>
            {
                rates.TryGetValue(currency.Id, out var rate);
                return new CashflowRateDto
                {
                    Currency = mapper.Map<CurrencyDto>(currency),
                    RateToBase = baseCurrency is null ? null : currency.IsBaseCurrency ? 1m : rate?.RateToBase,
                    EffectiveDate = currency.IsBaseCurrency ? null : rate?.EffectiveDate,
                };
            }).ToList(),
        };
    }
}
