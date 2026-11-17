using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace APP.Utils;

/// <summary>
/// Grouped quantity sums that are safe to read from PostgreSQL. Postgres adds numerics at full precision, so
/// summing exact snapshots such as 666.66666666666666666666666667 gives more significant digits than
/// System.Decimal holds and Npgsql's decimal reader throws OverflowException. Reading the sum as text lets
/// decimal.Parse round once instead. Same approach as <see cref="ReservationTotals"/>.
/// </summary>
internal static class QuantityTotals
{
    public static async Task<Dictionary<Guid, decimal>> ToTotalsByAsync<T>(this IQueryable<T> rows,
        Expression<Func<T, Guid>> key, Expression<Func<T, decimal>> quantity, bool postgres)
    {
        var groups = rows.Select(Keyed(key, quantity)).GroupBy(row => row.Key);
        if (!postgres)
            return await groups.Select(group => new { group.Key, Total = group.Sum(row => row.Quantity) })
                .ToDictionaryAsync(row => row.Key, row => row.Total);

        return await groups.Select(group => new { group.Key, Total = group.Sum(row => row.Quantity).ToString() })
            .ToDictionaryAsync(row => row.Key, row => Parse(row.Total));
    }

    private static decimal Parse(string value) =>
        value is null ? 0 : decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static Expression<Func<T, KeyedQuantity>> Keyed<T>(Expression<Func<T, Guid>> key, Expression<Func<T, decimal>> quantity)
    {
        var row = key.Parameters[0];
        var quantityBody = new Rebind(quantity.Parameters[0], row).Visit(quantity.Body);
        return Expression.Lambda<Func<T, KeyedQuantity>>(Expression.MemberInit(Expression.New(typeof(KeyedQuantity)),
            Expression.Bind(typeof(KeyedQuantity).GetProperty(nameof(KeyedQuantity.Key))!, key.Body),
            Expression.Bind(typeof(KeyedQuantity).GetProperty(nameof(KeyedQuantity.Quantity))!, quantityBody)), row);
    }

    private sealed class KeyedQuantity
    {
        public Guid Key { get; init; }
        public decimal Quantity { get; init; }
    }

    private sealed class Rebind(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
