using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public sealed partial class IssuedBmrRepository(ApplicationDbContext context)
{
    private sealed record Scope(Guid? DepartmentId, DepartmentType Type);

    private async Task<Result<Scope>> Access(Guid actor, string permission)
    {
        var permissions = await CurrentPermissionSet.Load(context, actor);
        if (!permissions.Contains(PermissionKeys.CanViewIssuedBmrRecords) || !permissions.Contains(permission))
            return Error.Forbidden("IssuedBmr.Permission", "Your current permissions do not allow this issued-BMR action.");
        var user = await context.Users.IgnoreAutoIncludes().AsNoTracking().Where(user => user.Id == actor && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type }).SingleOrDefaultAsync();
        return user?.Type is null ? Error.Forbidden("IssuedBmr.Department", "An assigned department is required.")
            : new Scope(user.DepartmentId, user.Type.Value);
    }

    private IQueryable<BatchManufacturingRecord> Records(Scope scope) => context.BatchManufacturingRecords
        .IgnoreQueryFilters().IgnoreAutoIncludes().Where(record => record.IssuedDate.HasValue
            && (scope.Type != DepartmentType.Production || record.ProductionScheduleProduct.Product.DepartmentId == scope.DepartmentId));

    private async Task<Result<BatchManufacturingRecord>> Editable(Guid id, Scope scope)
    {
        var record = await Records(scope).Include(record => record.ProductionScheduleProduct).ThenInclude(batch => batch.Product)
            .Include(record => record.ProductionScheduleProduct).ThenInclude(batch => batch.ProductionSchedule)
            .SingleOrDefaultAsync(record => record.Id == id);
        if (record is null) return Error.NotFound("IssuedBmr.NotFound", "Issued BMR was not found in your permitted scope.");
        if (context.Database.IsRelational())
        {
            await context.Entry(record).ReloadAsync();
            await context.Entry(record.ProductionScheduleProduct).ReloadAsync();
        }
        if (record.DeletedAt.HasValue || record.ProductionScheduleProduct.Cancelled || record.ProductionScheduleProduct.Product.DeletedAt.HasValue
            || record.ProductionScheduleProduct.ProductionSchedule.DeletedAt.HasValue)
            return Error.Conflict("IssuedBmr.Retired", "Retired, cancelled or archived batch records cannot be corrected.");
        return record;
    }

    private static bool Reason(string reason) => reason?.Trim().Length is >= 3 and <= 1000;
    private static bool Matches(BatchManufacturingRecord record, IssuedBmrExpected expected) => record.BatchNumber == expected.ExpectedBatchNumber
        && record.ManufacturingDate == expected.ExpectedManufacturingDate && record.ExpiryDate == expected.ExpectedExpiryDate;
    private static Error Stale() => Error.Conflict("IssuedBmr.Changed", "The batch details changed. Reload and review the current values before continuing.");

    public async Task<Result<IssuedBmrPage>> List(Guid actor, int page, int pageSize, string searchQuery)
    {
        var access = await Access(actor, PermissionKeys.CanViewIssuedBmrRecords);
        if (access.IsFailure) return access.Error;
        if (page < 1 || pageSize < 1 || pageSize > 100) return Error.Validation("IssuedBmr.Page", "Choose a valid page size between 1 and 100.");
        var query = Records(access.Value).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var search = searchQuery.Trim().ToUpperInvariant();
            query = query.Where(record => record.BatchNumber.ToUpper().Contains(search)
                || record.ProductionScheduleProduct.Product.Name.ToUpper().Contains(search)
                || record.ProductionScheduleProduct.Product.Code.ToUpper().Contains(search));
        }
        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(record => record.IssuedDate).ThenBy(record => record.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(record => new IssuedBmrRow
            {
                Id = record.Id, ScheduleProductId = record.ProductionScheduleProductId, ProductId = record.ProductionScheduleProduct.ProductId,
                ProductName = record.ProductionScheduleProduct.Product.Name, ProductCode = record.ProductionScheduleProduct.Product.Code,
                DepartmentName = record.ProductionScheduleProduct.Product.Department.Name,
                ScheduleCode = record.ProductionScheduleProduct.ProductionSchedule.Code, BatchNumber = record.BatchNumber,
                ManufacturingDate = record.ManufacturingDate, ExpiryDate = record.ExpiryDate, IssuedAt = record.IssuedDate.Value,
                IsRetired = record.DeletedAt.HasValue || record.ProductionScheduleProduct.Cancelled
                    || record.ProductionScheduleProduct.Product.DeletedAt.HasValue || record.ProductionScheduleProduct.ProductionSchedule.DeletedAt.HasValue,
                PendingRevisions = context.IssuedBmrRevisions.Count(revision => revision.BatchManufacturingRecordId == record.Id
                    && revision.Status == IssuedBmrRevisionStatus.Pending)
            }).ToListAsync();
        var current = await Access(actor, PermissionKeys.CanViewIssuedBmrRecords);
        if (current.IsFailure || current.Value != access.Value) return Error.Forbidden("IssuedBmr.Permission", "Your access changed. Reload this page.");
        return new IssuedBmrPage { Data = rows, PageIndex = page, PageCount = (int)Math.Ceiling(count / (double)pageSize), TotalRecordCount = count };
    }

    public async Task<Result<IssuedBmrHistory>> History(Guid id, Guid actor)
    {
        var access = await Access(actor, PermissionKeys.CanViewIssuedBmrRecords);
        if (access.IsFailure) return access.Error;
        if (!await Records(access.Value).AnyAsync(record => record.Id == id)) return Error.NotFound("IssuedBmr.NotFound", "Issued BMR was not found.");
        var changes = await context.IssuedBmrChanges.AsNoTracking().Where(change => change.BatchManufacturingRecordId == id)
            .OrderByDescending(change => change.OccurredAt).Select(change => new IssuedBmrChangeDto
            {
                Id = change.Id, RevisionId = change.RevisionId, ActorName = context.Users.IgnoreQueryFilters().Where(user => user.Id == change.ActorId)
                    .Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(), OccurredAt = change.OccurredAt, Reason = change.Reason,
                PreviousBatchNumber = change.PreviousBatchNumber, BatchNumber = change.BatchNumber,
                PreviousManufacturingDate = change.PreviousManufacturingDate, PreviousExpiryDate = change.PreviousExpiryDate,
                ManufacturingDate = change.ManufacturingDate, ExpiryDate = change.ExpiryDate
            }).ToListAsync();
        var revisions = await context.IssuedBmrRevisions.AsNoTracking().Where(revision => revision.BatchManufacturingRecordId == id)
            .OrderByDescending(revision => revision.RequestedAt).Select(revision => new IssuedBmrRevisionDto
            {
                Id = revision.Id, RequestedByName = context.Users.IgnoreQueryFilters().Where(user => user.Id == revision.RequestedById)
                    .Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(), RequestedAt = revision.RequestedAt,
                Reason = revision.Reason, BatchNumber = revision.BatchNumber, PreviousManufacturingDate = revision.PreviousManufacturingDate,
                PreviousExpiryDate = revision.PreviousExpiryDate, ManufacturingDate = revision.ManufacturingDate, ExpiryDate = revision.ExpiryDate,
                Status = revision.Status, ReviewedByName = context.Users.IgnoreQueryFilters().Where(user => user.Id == revision.ReviewedById)
                    .Select(user => user.FirstName + " " + user.LastName).FirstOrDefault(), ReviewedAt = revision.ReviewedAt,
                ReviewReason = revision.ReviewReason, IsRequester = revision.RequestedById == actor
            }).ToListAsync();
        var current = await Access(actor, PermissionKeys.CanViewIssuedBmrRecords);
        if (current.IsFailure || current.Value != access.Value) return Error.Forbidden("IssuedBmr.Permission", "Your access changed. Reload this page.");
        return new IssuedBmrHistory { Changes = changes, Revisions = revisions };
    }
}
