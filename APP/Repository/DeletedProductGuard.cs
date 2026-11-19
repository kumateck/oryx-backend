using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// A deleted product is history only: existing batches, documents and reports stay readable, but no
/// new work may name it. Every path that starts new work with a product calls this before writing.
/// </summary>
/// <remarks>
/// The product query filter does not hide deleted products from non-production and R&amp;D users, and
/// many reads bypass filters, so this check reads the product itself, ignoring every filter.
/// Ids that do not exist are left to the caller's own not-found handling.
/// </remarks>
public static class DeletedProductGuard
{
    public const string Code = "Product.Deleted";

    public static Task<Error> Refuse(ApplicationDbContext context, Guid? productId) =>
        Refuse(context, productId.HasValue ? [productId.Value] : []);

    public static async Task<Error> Refuse(ApplicationDbContext context, IEnumerable<Guid> productIds)
    {
        var ids = productIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return null;
        var deleted = await context.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(product => ids.Contains(product.Id) && product.DeletedAt != null)
            .OrderBy(product => product.Name)
            .Select(product => product.Name)
            .ToListAsync();
        return deleted.Count == 0 ? null : Refusal(deleted);
    }

    public static Error Refusal(IReadOnlyCollection<string> productNames) =>
        Error.Validation(Code, productNames.Count == 1
            ? $"{productNames.First()} has been deleted and can no longer be used. Existing records stay readable."
            : $"These products have been deleted and can no longer be used: {string.Join(", ", productNames)}. Existing records stay readable.");
}
