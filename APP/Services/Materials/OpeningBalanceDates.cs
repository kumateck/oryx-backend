using DOMAIN.Entities.Materials.StockMigrations;

namespace APP.Services.Materials;

/// <summary>The agreed opening-stock reconciliation rule applies only within one material and batch.</summary>
public static class OpeningBalanceDates
{
    public static void Reconcile(IEnumerable<StockMigrationLine> lines)
    {
        var batches = lines.Where(l => !l.Excluded && l.QuantityInSheet > 0 && !string.IsNullOrWhiteSpace(l.BatchNumber))
            .GroupBy(l => (Material: l.MaterialId?.ToString() ?? l.MaterialCode?.Trim().ToUpperInvariant(),
                Batch: l.BatchNumber.Trim().ToUpperInvariant()));
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch.Key.Material)) continue;
            var made = batch.Select(l => l.ManufacturingDate).Min();
            var expires = batch.Select(l => l.ExpiryDate).Max();
            foreach (var line in batch)
            {
                if (line.ManufacturingDate == made && line.ExpiryDate == expires) continue;
                line.ManufacturingDate = made;
                line.ExpiryDate = expires;
                const string note = "Same material and batch: earliest manufacturing and latest expiry dates were applied.";
                if (!(line.Corrections ?? "").Contains(note))
                    line.Corrections = string.Join('\n', new[] { line.Corrections, note }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }
    }

    public static DateTime? ExpiryMonthEnd(DateTime? month) => month is { } d
        ? new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 0, 0, 0, DateTimeKind.Utc)
        : null;
}
