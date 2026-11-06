using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.WhitePlacebo;
using Microsoft.EntityFrameworkCore;

namespace INFRASTRUCTURE.Context;

public partial class ApplicationDbContext
{
    private void ValidateWhitePlaceboStock()
    {
        var batches = ChangeTracker.Entries<MaterialBatch>().Where(entry => entry.State == EntityState.Added
            || entry.State == EntityState.Modified && entry.Property(item => item.ExpiryDate).IsModified).ToList();
        if (batches.Count == 0) return;
        var materialIds = batches.Select(entry => entry.Entity.MaterialId).Distinct().ToArray();
        var guarded = Materials.IgnoreQueryFilters().Where(item => materialIds.Contains(item.Id) && item.IsWhitePlacebo)
            .Select(item => item.Id).ToHashSet();
        foreach (var entry in batches.Where(entry => guarded.Contains(entry.Entity.MaterialId)))
        {
            var batch = entry.Entity;
            var preparation = ChangeTracker.Entries<WhitePlaceboPreparation>().Select(item => item.Entity)
                .FirstOrDefault(item => item.OutputBatchId == batch.Id || item.OutputBatch == batch)
                ?? WhitePlaceboPreparations.AsNoTracking().FirstOrDefault(item => item.OutputBatchId == batch.Id);
            if (preparation?.PreparedAt is not { } prepared)
                throw new InvalidOperationException("White Placebo stock must be recorded through its ingredient-linked preparation, not an upload or supplier receipt.");
            if (batch.ExpiryDate == null || batch.ExpiryDate > prepared.AddHours(preparation.HoldHours))
                throw new InvalidOperationException("A White Placebo batch cannot exceed its preparation's established holding limit.");
        }
    }
}
