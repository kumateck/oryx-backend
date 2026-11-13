using System.Globalization;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;

namespace APP.Utils;

internal static class ReservationTotals
{
    public static decimal Sum(IQueryable<MaterialBatchReservedQuantity> reservations, bool postgres) =>
        postgres ? Parse(AsText(reservations).SingleOrDefault()) : reservations.Sum(row => row.Quantity);

    public static async Task<decimal> SumAsync(IQueryable<MaterialBatchReservedQuantity> reservations, bool postgres) =>
        postgres ? Parse(await AsText(reservations).SingleOrDefaultAsync()) : await reservations.SumAsync(row => row.Quantity);

    public static async Task<Dictionary<Guid, decimal>> ByMaterialAsync(
        IQueryable<MaterialBatchReservedQuantity> reservations, bool postgres)
    {
        var groups = reservations.GroupBy(row => row.MaterialBatch.MaterialId);
        if (!postgres)
            return await groups.Select(group => new { MaterialId = group.Key, Total = group.Sum(row => row.Quantity) })
                .ToDictionaryAsync(row => row.MaterialId, row => row.Total);

        return await groups.Select(group => new { MaterialId = group.Key, Total = group.Sum(row => row.Quantity).ToString() })
            .ToDictionaryAsync(row => row.MaterialId, row => Parse(row.Total));
    }

    // PostgreSQL aggregates at full precision. Reading the result as text lets decimal.Parse
    // round once when the sum has more significant digits than Npgsql's decimal reader accepts.
    private static IQueryable<string> AsText(IQueryable<MaterialBatchReservedQuantity> reservations) =>
        reservations.GroupBy(_ => 1).Select(group => group.Sum(row => row.Quantity).ToString());

    private static decimal Parse(string value) =>
        value is null ? 0 : decimal.Parse(value, CultureInfo.InvariantCulture);
}
