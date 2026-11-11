using APP.Services.Authorization;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result> UpdateMigrationBatchDetails(Guid batchId, UpdateMigrationBatchDetails request, Guid actorId)
    {
        const string permission = PermissionKeys.CanUpdateMigrationBatchDetails;
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(permission))
            return Error.Forbidden("MigrationBatch.Permission", "You do not have permission to correct migrated batch details.");

        var reason = request.Reason?.Trim();
        var grn = string.IsNullOrWhiteSpace(request.GrnNumber) ? null : request.GrnNumber.Trim();
        if (reason is null || reason.Length is < 3 or > 1000 || grn?.Length > 500)
            return Error.Validation("MigrationBatch.Details", "Enter a correction reason and a GRN reference of at most 500 characters.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync() : null;
        if (transaction is not null)
            await context.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"MaterialBatches\" WHERE \"Id\" = {batchId} FOR UPDATE");
        var batch = await context.MaterialBatches.IgnoreAutoIncludes().SingleOrDefaultAsync(row => row.Id == batchId);
        if (batch is null) return Error.NotFound("MigrationBatch.NotFound", "Material batch was not found.");
        await context.Entry(batch).ReloadAsync();

        var departmentId = await context.Users.Where(user => user.Id == actorId).Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (departmentId is null || !await context.StockMigrationLines.AnyAsync(line => line.ImportedBatchId == batchId
            && line.StockMigration.Status == StockMigrationStatus.Approved && line.StockMigration.DepartmentId == departmentId))
            return Error.Forbidden("MigrationBatch.Scope", "Only approved opening stock in your department can be corrected here.");

        if (batch.Status != BatchStatus.Available || batch.GrnId.HasValue
            || await context.MaterialRetestRequests.AnyAsync(row => row.MaterialBatchId == batchId))
            return Error.Conflict("MigrationBatch.QualityGate", "This batch requires the regular receipt or QC correction process.");

        if (batch.ManufacturingDate != request.ExpectedManufacturingDate || batch.ExpiryDate != request.ExpectedExpiryDate
            || batch.MigrationGrnNumber != request.ExpectedGrnNumber)
            return Error.Conflict("MigrationBatch.Changed", "The batch details changed. Reload the batch before correcting them.");

        var manufacturingChanged = request.ManufacturingDate != batch.ManufacturingDate;
        var expiryChanged = request.ExpiryDate != batch.ExpiryDate;
        var manufactured = manufacturingChanged && request.ManufacturingDate is { } manufactureMonth
            ? new DateTime(manufactureMonth.Year, manufactureMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            : request.ManufacturingDate;
        var expiry = expiryChanged ? OpeningBalanceDates.ExpiryMonthEnd(request.ExpiryDate) : batch.ExpiryDate;
        if ((manufacturingChanged && manufactured is { } changedManufacture && (changedManufacture.Year < 1900 || changedManufacture > DateTime.UtcNow))
            || (expiryChanged && expiry?.Year < 1900)
            || ((manufacturingChanged || expiryChanged) && manufactured is { Year: >= 1900 } made && expiry is { Year: >= 1900 } expires
                && (made.Year, made.Month).CompareTo((expires.Year, expires.Month)) >= 0))
            return Error.Validation("MigrationBatch.Dates", "Choose valid months. Manufacture cannot be in the future and expiry must be in a later month.");
        if (expiryChanged && expiry is { } correctedExpiry && batch.RetestDate is { Year: > 1900 } retest
            && (retest.Year, retest.Month).CompareTo((correctedExpiry.Year, correctedExpiry.Month)) <= 0)
            return Error.Validation("MigrationBatch.Retest", "Expiry must precede the recorded retest month. Correct the migration retest date first.");

        if (batch.ManufacturingDate == manufactured && batch.ExpiryDate == expiry && batch.MigrationGrnNumber == grn)
            return Result.Success();
        if (!(await CurrentPermissionSet.Load(context, actorId)).Contains(permission))
            return Error.Forbidden("MigrationBatch.Permission", "Your permission changed before saving.");

        context.MigrationRetestDateChanges.Add(new MigrationRetestDateChange
        {
            Id = Guid.NewGuid(), MaterialBatchId = batchId, ActorId = actorId, OccurredAt = DateTime.UtcNow, Reason = reason,
            PreviousDate = batch.RetestDate, NewDate = batch.RetestDate,
            PreviousManufacturingDate = batch.ManufacturingDate, NewManufacturingDate = manufactured,
            ExpiryDate = batch.ExpiryDate, NewExpiryDate = expiry,
            PreviousGrnNumber = batch.MigrationGrnNumber, NewGrnNumber = grn
        });
        batch.ManufacturingDate = manufactured;
        batch.ExpiryDate = expiry;
        batch.MigrationGrnNumber = grn;
        batch.LastUpdatedById = actorId;
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }
}
