using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public sealed partial class IssuedBmrRepository
{
    public async Task<Result> CorrectNumber(Guid id, CorrectIssuedBmrNumber request, Guid actor)
    {
        await using var transaction = await ProductionMaterialTransaction.Begin(context);
        var access = await Access(actor, PermissionKeys.CanCorrectIssuedBmrBatchNumber);
        if (access.IsFailure) return access.Error;
        var loaded = await Editable(id, access.Value);
        if (loaded.IsFailure) return loaded.Error;
        var record = loaded.Value;
        if (!Matches(record, request)) return Stale();
        if (!Reason(request.Reason)) return Error.Validation("IssuedBmr.Reason", "Record a reason of 3–1000 characters.");
        var unique = await BmrBatchNumberRules.Validate(context, request.BatchNumber, id);
        if (unique.IsFailure) return unique;
        var number = request.BatchNumber.Trim();
        if (number == record.BatchNumber) return Error.Validation("IssuedBmr.NoChange", "Enter the corrected batch number.");
        var current = await Access(actor, PermissionKeys.CanCorrectIssuedBmrBatchNumber);
        if (current.IsFailure || current.Value != access.Value) return Error.Forbidden("IssuedBmr.Permission", "Your access changed before saving.");
        await Apply(record, number, record.ManufacturingDate, record.ExpiryDate, actor, request.Reason.Trim(), null);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result<Guid>> RequestRevision(Guid id, RequestIssuedBmrRevision request, Guid actor)
    {
        await using var transaction = await ProductionMaterialTransaction.Begin(context);
        var access = await Access(actor, PermissionKeys.CanRequestIssuedBmrRevision);
        if (access.IsFailure) return access.Error;
        var loaded = await Editable(id, access.Value);
        if (loaded.IsFailure) return loaded.Error;
        var record = loaded.Value;
        if (!Matches(record, request)) return Stale();
        if (!Reason(request.Reason) || request.ManufacturingDate.Year < 1901 || request.ExpiryDate <= request.ManufacturingDate)
            return Error.Validation("IssuedBmr.Dates", "Record a reason and valid manufacturing/expiry dates, with expiry later than manufacturing.");
        if (request.ManufacturingDate == record.ManufacturingDate && request.ExpiryDate == record.ExpiryDate)
            return Error.Validation("IssuedBmr.NoChange", "Change a date before requesting a revision.");
        if (await context.IssuedBmrRevisions.AnyAsync(revision => revision.BatchManufacturingRecordId == id && revision.Status == IssuedBmrRevisionStatus.Pending))
            return Error.Conflict("IssuedBmr.Pending", "This BMR already has a pending revision. Review it before requesting another.");
        var current = await Access(actor, PermissionKeys.CanRequestIssuedBmrRevision);
        if (current.IsFailure || current.Value != access.Value) return Error.Forbidden("IssuedBmr.Permission", "Your access changed before saving.");
        var revision = new IssuedBmrRevision { Id = Guid.NewGuid(), BatchManufacturingRecordId = id, RequestedById = actor,
            RequestedAt = DateTime.UtcNow, Reason = request.Reason.Trim(), BatchNumber = record.BatchNumber,
            PreviousManufacturingDate = record.ManufacturingDate, PreviousExpiryDate = record.ExpiryDate,
            ManufacturingDate = request.ManufacturingDate, ExpiryDate = request.ExpiryDate };
        context.IssuedBmrRevisions.Add(revision);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return revision.Id;
    }

    public async Task<Result> ReviewRevision(Guid id, Guid revisionId, ReviewIssuedBmrRevision request, Guid actor)
    {
        await using var transaction = await ProductionMaterialTransaction.Begin(context);
        var access = await Access(actor, PermissionKeys.CanApproveIssuedBmrRevision);
        if (access.IsFailure) return access.Error;
        var loaded = await Editable(id, access.Value);
        if (loaded.IsFailure) return loaded.Error;
        var revision = await context.IssuedBmrRevisions.SingleOrDefaultAsync(revision => revision.Id == revisionId && revision.BatchManufacturingRecordId == id);
        if (revision is null) return Error.NotFound("IssuedBmr.Revision", "The revision was not found.");
        if (revision.Status != IssuedBmrRevisionStatus.Pending) return Error.Conflict("IssuedBmr.Reviewed", "This revision has already been reviewed.");
        if (revision.RequestedById == actor) return Error.Forbidden("IssuedBmr.SelfApproval", "Someone else with revision approval permission must review this request.");
        if (!Reason(request.Reason)) return Error.Validation("IssuedBmr.Reason", "Record a review reason of 3–1000 characters.");
        var record = loaded.Value;
        if (request.Approve && (record.ManufacturingDate != revision.PreviousManufacturingDate || record.ExpiryDate != revision.PreviousExpiryDate)) return Stale();
        var current = await Access(actor, PermissionKeys.CanApproveIssuedBmrRevision);
        if (current.IsFailure || current.Value != access.Value) return Error.Forbidden("IssuedBmr.Permission", "Your access changed before saving.");
        if (request.Approve)
            await Apply(record, record.BatchNumber, revision.ManufacturingDate, revision.ExpiryDate, actor, revision.Reason, revision.Id);
        revision.Status = request.Approve ? IssuedBmrRevisionStatus.Approved : IssuedBmrRevisionStatus.Rejected;
        revision.ReviewedById = actor; revision.ReviewedAt = DateTime.UtcNow; revision.ReviewReason = request.Reason.Trim();
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Result.Success();
    }
}
