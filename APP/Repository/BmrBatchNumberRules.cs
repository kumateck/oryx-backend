using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class BmrBatchNumberRules
{
    internal static async Task<Result> ValidateForIssue(
        ApplicationDbContext context, string batchNumber, Guid scheduleProductId, Guid recordId)
    {
        var stem = await context.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(batch => batch.Id == scheduleProductId)
            .Select(batch => batch.Product.BatchStem).SingleOrDefaultAsync();
        var number = batchNumber?.Trim();
        if (!string.IsNullOrEmpty(stem) && (number is null || number.Length <= stem.Length
            || !number.StartsWith(stem, StringComparison.Ordinal) || !number.Skip(stem.Length).All(char.IsAsciiDigit)))
            return Error.Validation("Bmr.BatchStem", $"Enter the product's batch stem '{stem}' followed by digits.");
        return await Validate(context, batchNumber, recordId);
    }

    internal static async Task<Result> Validate(ApplicationDbContext context, string batchNumber, Guid? currentId = null)
    {
        var number = batchNumber?.Trim();
        if (string.IsNullOrWhiteSpace(number)) return Error.Validation("Bmr.BatchNumberRequired", "Enter the batch number.");
        if (number.Length > 100) return Error.Validation("Bmr.BatchNumberLength", "The batch number cannot exceed 100 characters.");
        var normalized = number.ToUpperInvariant();
        var duplicates = await context.BatchManufacturingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(record => record.Id != currentId && record.BatchNumber != null && record.BatchNumber.Trim().ToUpper() == normalized)
            .Select(record => new { record.ProductionScheduleProductId, record.DeletedAt,
                Cancelled = record.ProductionScheduleProduct.Cancelled }).ToListAsync();
        foreach (var record in duplicates)
        {
            // Only the activated full-return workflow both cancels production and retires its BMR.
            // Ordinary deletion/cancellation, partial returns and empty draft notes cannot release a number.
            var returned = record.DeletedAt.HasValue && record.Cancelled
                && await context.MaterialReturnNotes.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                    .AnyAsync(note => note.ProductionScheduleProductId == record.ProductionScheduleProductId
                        && note.DeletedAt == null && note.BatchNumber != null && note.BatchNumber.Trim().ToUpper() == normalized
                        && note.FullReturns.Any(line => line.DeletedAt == null)
                        && !note.PartialReturns.Any(line => line.DeletedAt == null));
            if (!returned)
                return Error.Conflict("Bmr.DuplicateBatchNumber", $"Batch number '{number}' already exists. Use a different number. It can only be reused after the previous production's full return has been activated.");
        }
        return Result.Success();
    }
}
