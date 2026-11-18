using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
namespace APP.Repository;

/// <summary>Withdraw only an unissued request. The batch's pre-existing production reservations are unchanged.</summary>
internal static class StockRequisitionRejection
{
    internal static string Key(MaterialKind kind) => kind == MaterialKind.Raw
        ? PermissionKeys.CanRejectRawMaterialStockRequisition : PermissionKeys.CanRejectPackagingMaterialStockRequisition;
    internal static async Task<Result> Validate(ApplicationDbContext db, Requisition request, Guid actor, string reason)
    {
        if ((reason ?? "").Trim().Length is < 1 or > 1000)
            return Error.Validation("StockRequisition.Reason", "Record a rejection reason of 1–1000 characters.");
        var kinds = request.Items.Select(item => item.Material.Kind).Distinct().ToList();
        if (request.RequisitionType != RequisitionType.Stock || !request.ProductionScheduleProductId.HasValue || kinds.Count != 1)
            return Error.Conflict("StockRequisition.Kind", "The production stock requisition must contain one material kind.");
        if (!(await CurrentPermissionSet.Load(db, actor)).Contains(Key(kinds[0])))
            return Error.Forbidden("StockRequisition.Permission", "You cannot reject this material kind's stock requisition.");
        if (request.Status is RequestStatus.Completed or RequestStatus.Rejected || request.Items.Any(item => item.QuantityReceived > 0)
            || await db.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(issue => issue.RequisitionId == request.Id)
            || await db.BinCardInformation.IgnoreQueryFilters().AnyAsync(entry => entry.RequisitionId == request.Id))
            return Error.Conflict("StockRequisition.AlreadyIssued", "Issued stock cannot be rejected back to an unrequested state. Use its issue correction or material return workflow.");
        if (await db.ProductionExtraPackings.AnyAsync(extra => extra.ProductionScheduleProductId == request.ProductionScheduleProductId && extra.Material.Kind == kinds[0]))
            return Error.Conflict("StockRequisition.ExtraMaterial", "An extra-material request depends on this requisition. Reconcile it before rejecting the main request.");
        return Result.Success();
    }
    internal static void Apply(Requisition request)
    {
        request.Status = RequestStatus.Rejected;
        request.Approved = false;
        foreach (var item in request.Items) item.Status = RequestStatus.Rejected;
        foreach (var stage in request.Approvals.Where(stage => stage.Status == DOMAIN.Entities.Approvals.ApprovalStatus.Pending)) stage.ActivatedAt = null;
    }
}
