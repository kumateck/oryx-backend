using DOMAIN.Entities.Materials.StockMigrations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Materials;

/// <summary>Corrections override displayed references while source receipts remain unchanged.</summary>
public static class MigrationBatchReferences
{
    public static IQueryable<string> Corrections(ApplicationDbContext context, Guid batchId) =>
        context.MigrationRetestDateChanges.Where(c => c.MaterialBatchId == batchId && c.NewArNumber != null
                && !context.MaterialSamplings.Any(s => s.MaterialBatchId == batchId && s.CreatedAt > c.OccurredAt))
            .OrderByDescending(c => c.OccurredAt).Select(c => c.NewArNumber);

    public static IQueryable<string> SourceReferences(ApplicationDbContext context, Guid batchId) =>
        context.StockMigrationLines.Where(l => l.ImportedBatchId == batchId
                && l.StockMigration.Status == StockMigrationStatus.Approved && l.ArNumber != null && l.ArNumber != "")
            .Select(l => l.ArNumber).Distinct().OrderBy(ar => ar);

    public static async Task<string> Current(ApplicationDbContext context, Guid batchId) =>
        await Corrections(context, batchId).FirstOrDefaultAsync()
        ?? await context.MaterialSamplings.Where(s => s.MaterialBatchId == batchId).Select(s => s.ArNumber).FirstOrDefaultAsync()
        ?? string.Join(", ", await SourceReferences(context, batchId).ToListAsync());
}
