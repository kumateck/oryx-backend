using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Reviews;

/// <summary>
/// Corrects a purchase requisition while it is in review, before its approvals start. Its reviewer or its
/// preparer may correct it; every correction is recorded on the review.
/// </summary>
public static class PurchaseRequisitionCorrection
{
    public static async Task<Result> ApplyAsync(ApplicationDbContext context, Guid requisitionId,
        CreateRequisitionRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var requisition = await context.Requisitions.FirstOrDefaultAsync(item => item.Id == requisitionId, cancellationToken);
        if (requisition is null) return Error.NotFound("Requisition.NotFound", "The requisition was not found.");
        if (requisition.RequisitionType != RequisitionType.Purchase)
            return Error.Validation("Requisition.NotPurchase", "Only a purchase requisition can be corrected here.");
        if (!await DocumentReviewGate.IsOpenAsync(context, ReviewDocumentTypes.PurchaseRequisition, requisitionId, cancellationToken))
            return Error.Conflict("Requisition.NotInReview", "A purchase requisition can be corrected only while it is in review.");
        if (await context.WhitePlaceboPreparations.AnyAsync(item => item.RequisitionId == requisitionId, cancellationToken))
            return Error.Conflict("WhitePlacebo.ImmutableRequest", "A White Placebo ingredient request is fixed to its preparation and cannot be edited separately.");

        var lines = request?.Items ?? [];
        if (lines.Count == 0) return Error.Validation("Requisition.Items", "Add at least one material.");
        if (lines.Any(line => line.Quantity <= 0)) return Error.Validation("Requisition.Quantity", "Every quantity must be more than zero.");
        if (lines.Select(line => line.MaterialId).Distinct().Count() != lines.Count)
            return Error.Validation("Requisition.Duplicate", "List each material once.");
        var materialIds = lines.Select(line => line.MaterialId).ToList();
        var materials = await context.Materials.AsNoTracking().Where(item => materialIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Kind, item.IsWhitePlacebo }).ToListAsync(cancellationToken);
        if (materials.Count != materialIds.Count) return Error.Validation("Requisition.Material", "Choose existing materials.");
        if (materials.Any(item => item.IsWhitePlacebo))
            return Error.Validation("WhitePlacebo.InternalOnly", "White Placebo is prepared internally and cannot be purchased.");

        var existing = await context.RequisitionItems.Where(item => item.RequisitionId == requisitionId && item.DeletedAt == null)
            .Include(item => item.Material).ToListAsync(cancellationToken);
        var kinds = existing.Select(item => item.Material.Kind).Concat(materials.Select(item => item.Kind)).Distinct();
        var needed = kinds.Select(kind => kind == MaterialKind.Raw
            ? PermissionKeys.CanEditRawMaterialPurchaseRequisition : PermissionKeys.CanEditPackageMaterialPurchaseRequisition).ToList();
        var grants = await CurrentPermissionSet.Load(context, actorId);
        var missing = needed.Where(key => !grants.Contains(key)).ToList();
        if (missing.Count != 0) return Error.Forbidden("Requisition.Permission", $"You need {string.Join(" and ", missing)} for this.");

        var now = DateTime.UtcNow;
        foreach (var item in existing)
        {
            item.DeletedAt = now;
            item.LastDeletedById = actorId;
        }
        context.RequisitionItems.AddRange(lines.Select(line => new RequisitionItem
        {
            RequisitionId = requisitionId, MaterialId = line.MaterialId, UoMId = line.UomId, Quantity = line.Quantity,
            Status = RequestStatus.New, CreatedById = actorId,
        }));
        requisition.Comments = request.Comments;
        requisition.ExpectedDelivery = request.ExpectedDelivery;
        requisition.ContentRevision++;
        requisition.LastUpdatedById = actorId;
        requisition.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken);
        await DocumentReviewGate.NoteEditAsync(context, ReviewDocumentTypes.PurchaseRequisition, requisitionId, actorId, cancellationToken);
        return Result.Success();
    }
}
