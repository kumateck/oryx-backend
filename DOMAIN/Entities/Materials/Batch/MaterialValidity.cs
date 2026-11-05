using System.Linq.Expressions;

namespace DOMAIN.Entities.Materials.Batch;

/// <summary>
/// Use the active expiry first, then the retest date, for every material kind (raw and packaging).
/// A batch with neither date set (or only the MinValue sentinel) never expires.
/// </summary>
public static class MaterialValidity
{
    public static Expression<Func<MaterialBatch, DateTime?>> DateAt(DateTime asOf) => batch =>
        !(batch.ExpiryDate > asOf) ? batch.RetestDate ?? batch.ExpiryDate : batch.ExpiryDate;

    public static DateTime? EffectiveDate(MaterialBatch batch, DateTime? asOf = null) =>
        !(batch.ExpiryDate > (asOf ?? DateTime.UtcNow)) ? batch.RetestDate ?? batch.ExpiryDate : batch.ExpiryDate;

    public static bool IsExpired(MaterialBatch batch, DateTime asOf) =>
        IsExpiredDate(EffectiveDate(batch, asOf), asOf);

    public static bool IsExpiredDate(DateTime? date, DateTime asOf) =>
        date.HasValue && date != DateTime.MinValue && date <= asOf;

    public static bool IsExpiringBy(DateTime? date, DateTime asOf, DateTime through) =>
        date.HasValue && date != DateTime.MinValue && date > asOf && date <= through;

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

    /// <summary>Project the effective date without duplicating its business rule in SQL reports.</summary>
    public static IQueryable<TResult> SelectMaterialDate<T, TResult>(this IQueryable<T> query,
        Expression<Func<T, MaterialBatch>> batch, Expression<Func<T, DateTime?, TResult>> projection)
    {
        var date = DateFor(batch);
        var body = new Substitute(projection.Parameters[0], batch.Parameters[0]).Visit(projection.Body);
        body = new Substitute(projection.Parameters[1], date.Body).Visit(body);
        return query.Select(Expression.Lambda<Func<T, TResult>>(body, batch.Parameters));
    }

    private sealed class Substitute(Expression source, Expression target) : ExpressionVisitor
    {
        public override Expression Visit(Expression node) => node == source ? target : base.Visit(node);
    }
}
