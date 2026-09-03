using DOMAIN.Entities.Payments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class PaymentBalanceQuery
{
    internal sealed record Total(
        Guid CurrencyId,
        string CurrencyName,
        string CurrencySymbol,
        decimal Amount
    );

    public static async Task<List<PayableBalanceDto>> GetAsync(
        ApplicationDbContext context,
        PayableType payableType,
        Guid payableId,
        IEnumerable<Total> totals
    )
    {
        var paidByCurrency = await context
            .Payments.AsNoTracking()
            .Where(payment =>
                payment.Approved
                && payment.PayableType == payableType
                && payment.PayableId == payableId
            )
            .GroupBy(payment => payment.CurrencyId)
            .Select(group => new { CurrencyId = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CurrencyId, x => x.Amount);

        return
        [
            .. totals
                .GroupBy(total => new
                {
                    total.CurrencyId,
                    total.CurrencyName,
                    total.CurrencySymbol,
                })
                .Select(group =>
                {
                    var documentTotal = group.Sum(x => x.Amount);
                    var amountPaid = paidByCurrency.GetValueOrDefault(group.Key.CurrencyId);
                    return new PayableBalanceDto
                    {
                        CurrencyId = group.Key.CurrencyId,
                        CurrencyName = group.Key.CurrencyName,
                        CurrencySymbol = group.Key.CurrencySymbol,
                        DocumentTotal = documentTotal,
                        AmountPaid = amountPaid,
                        OutstandingBalance = Math.Max(0m, documentTotal - amountPaid),
                    };
                })
        ];
    }
}
