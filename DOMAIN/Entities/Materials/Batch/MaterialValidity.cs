using System.Linq.Expressions;

namespace DOMAIN.Entities.Materials.Batch;

/// <summary>Use active expiry first, then the raw-material retest extension; packaging keeps its expiry.</summary>
public static class MaterialValidity
{
    public static Expression<Func<MaterialBatch, DateTime?>> DateAt(DateTime asOf) => batch =>
        batch.Material.Kind == MaterialKind.Raw && !(batch.ExpiryDate > asOf)
            ? batch.RetestDate ?? batch.ExpiryDate : batch.ExpiryDate;

    public static DateTime? EffectiveDate(MaterialBatch batch, DateTime? asOf = null) =>
        batch.Material?.Kind == MaterialKind.Raw && !(batch.ExpiryDate > (asOf ?? DateTime.UtcNow))
            ? batch.RetestDate ?? batch.ExpiryDate : batch.ExpiryDate;

    public static bool IsExpired(MaterialBatch batch, DateTime asOf) =>
        EffectiveDate(batch, asOf) is { } date && date != DateTime.MinValue && date <= asOf;

    /// <summary>Compose the same rule into SQL, including shelf and reservation queries.</summary>
    public static Expression<Func<T, DateTime?>> DateFor<T>(Expression<Func<T, MaterialBatch>> batch)
    {
        var date = DateAt(DateTime.UtcNow);
        var body = new Substitute(date.Parameters[0], batch.Body).Visit(date.Body);
        return Expression.Lambda<Func<T, DateTime?>>(body, batch.Parameters);
    }

    public static IQueryable<T> WhereMaterialDate<T>(this IQueryable<T> query,
        Expression<Func<T, MaterialBatch>> batch, Expression<Func<DateTime?, bool>> condition)
    {
        var date = DateFor(batch);
        var body = new Substitute(condition.Parameters[0], date.Body).Visit(condition.Body);
        return query.Where(Expression.Lambda<Func<T, bool>>(body, batch.Parameters));
    }

    private sealed class Substitute(Expression source, Expression target) : ExpressionVisitor
    {
        public override Expression Visit(Expression node) => node == source ? target : base.Visit(node);
    }
}
