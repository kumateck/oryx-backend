using APP.Services.Authorization;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result> UpdateMigrationRetestDate(Guid batchId, UpdateMigrationRetestDate request, Guid actorId)
    {
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanUpdateMigrationRetestDate))
            return Error.Forbidden("MaterialRetest.Permission", "Your current permissions do not allow migration retest-date changes.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync() : null;
        // Serialize competing corrections; refresh after locking so stale tracked values cannot win.
        if (context.Database.IsRelational())
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"MaterialBatches\" WHERE \"Id\" = {batchId} FOR UPDATE");
        var batch = await context.MaterialBatches.IgnoreAutoIncludes().Include(b => b.Material)
            .SingleOrDefaultAsync(b => b.Id == batchId);
        if (batch is null) return Error.NotFound("MaterialRetest.Batch", "Material batch was not found.");
        await context.Entry(batch).ReloadAsync();
        var department = await context.Users.Where(u => u.Id == actorId).Select(u => u.DepartmentId).SingleAsync();
        if (department is null || !await context.StockMigrationLines.AnyAsync(line => line.ImportedBatchId == batchId
            && line.StockMigration.Status == StockMigrationStatus.Approved && line.StockMigration.DepartmentId == department))
            return Error.Forbidden("MaterialRetest.MigrationScope", "Only approved opening stock in your department can be corrected here.");
        if (batch.Material.Kind != MaterialKind.Raw || batch.Status != BatchStatus.Available
            || await context.MaterialRetestRequests.AnyAsync(r => r.MaterialBatchId == batchId))
            return Error.Conflict("MaterialRetest.QualityGate", "Only available raw migration stock without a QC retest cycle can be corrected here.");
        if (batch.RetestDate != request.ExpectedRetestDate || batch.ExpiryDate != request.ExpectedExpiryDate)
            return Error.Conflict("MaterialRetest.Changed", "The dates changed. Reload the batch before correcting them.");
        var previousAr = await MigrationBatchReferences.Current(context, batchId);
        if (request.ArNumber != null && previousAr != (request.ExpectedArNumber ?? string.Empty))
            return Error.Conflict("MaterialRetest.Changed", "The AR number changed. Reload the batch before correcting it.");
        if (request.ArNumber?.Trim().Length > 500)
            return Error.Validation("MaterialRetest.ArNumber", "The AR number must be at most 500 characters.");
        var reason = request.Reason?.Trim();
        if (reason is null || reason.Length < 3 || reason.Length > 1000
            || request.RetestDate != batch.RetestDate && request.RetestDate is { } date
                && (date.Year <= 1900 || batch.ExpiryDate.HasValue && date <= batch.ExpiryDate))
            return Error.Validation("MaterialRetest.Correction", "Record a reason and a retest extension later than the original expiry, or clear the retest date.");
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(PermissionKeys.CanUpdateMigrationRetestDate))
            return Error.Forbidden("MaterialRetest.Permission", "Your permission changed before saving.");
        context.Set<MigrationRetestDateChange>().Add(new MigrationRetestDateChange
        {
            Id = Guid.NewGuid(), MaterialBatchId = batchId, PreviousDate = batch.RetestDate,
            NewDate = request.RetestDate, ExpiryDate = batch.ExpiryDate, Reason = reason,
            PreviousArNumber = previousAr, NewArNumber = request.ArNumber?.Trim(),
            ActorId = actorId, OccurredAt = DateTime.UtcNow,
        });
        batch.RetestDate = request.RetestDate;
        batch.LastUpdatedById = actorId;
        batch.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }
}
