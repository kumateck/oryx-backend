using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;

namespace APP.Services.Production;

public sealed partial class UnstartLeftoverRepairService(ApplicationDbContext db, ICurrentUserService identity)
{
    private sealed record Plan(UnstartLeftoverPreviewDto Preview, List<Requisition> Requests, Dictionary<Guid, Guid> Activities, string Snapshot);

    private async Task<Result<Guid>> Access(MaterialKind kind, bool write)
    {
        if (kind is not (MaterialKind.Raw or MaterialKind.Package))
            return Error.Validation("UnstartLeftovers.Kind", "Choose raw or packaging materials.");
        var grants = await CurrentPermissionSet.Load(db, identity.UserId);
        var view = kind == MaterialKind.Raw ? PermissionKeys.CanViewRawMaterialRequisitions : PermissionKeys.CanViewPackagingMaterialRequisitions;
        var repair = kind == MaterialKind.Raw ? PermissionKeys.CanRepairUnstartRawMaterialRequisitions : PermissionKeys.CanRepairUnstartPackagingMaterialRequisitions;
        var department = await db.Users.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(user => user.Id == identity.UserId && !user.IsDisabled && user.DeletedAt == null)
            .Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (!department.HasValue || !grants.Contains(view) || write && !grants.Contains(repair))
            return Error.Forbidden("UnstartLeftovers.Permission", "Your current permissions and department do not allow this repair.");
        return department.Value;
    }

    public async Task<Result<UnstartLeftoverPreviewDto>> Preview(MaterialKind kind)
    {
        var access = await Access(kind, false);
        if (access.IsFailure) return access.Error;
        var plan = await Build(access.Value, kind);
        return plan.Preview;
    }

    private async Task<Plan> Build(Guid department, MaterialKind kind)
    {
        // Ignore global filters to inspect historical issue evidence and both active/retired lines.
        // Scope explicitly to the caller's department and the materials' stored kind, never a code prefix.
        var requests = await db.Requisitions.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(r => r.DeletedAt == null && r.DepartmentId == department && r.RequisitionType == RequisitionType.Stock
                && r.ProductionScheduleProductId != null && r.RndTrialBatchId == null
                && r.Items.Any(line => line.Material.Kind == kind)
                && db.ProductionUnstartRepairs.Any(repair => repair.BatchId == r.ProductionScheduleProductId && repair.AppliedAt >= r.CreatedAt)
                && !db.WhitePlaceboPreparations.IgnoreQueryFilters().Any(preparation => preparation.RequisitionId == r.Id))
            .Include(r => r.Items).ThenInclude(line => line.Material)
            .OrderBy(r => r.Id).Take(200).ToListAsync();
        var rows = new List<UnstartLeftoverRowDto>();
        var activities = new Dictionary<Guid, Guid>();
        foreach (var r in requests)
        {
            var batchId = r.ProductionScheduleProductId!.Value;
            var repair = await db.ProductionUnstartRepairs.AsNoTracking().Where(item => item.BatchId == batchId && item.AppliedAt >= r.CreatedAt)
                .OrderByDescending(item => item.AppliedAt).ThenBy(item => item.Id).FirstAsync();
            var run = await db.ProductionActivities.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Where(item => item.Id == repair.ActivityId && item.ProductionScheduleProductId == batchId)
                .Select(item => new { item.DeletedAt }).SingleOrDefaultAsync();
            var batch = await db.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Where(item => item.Id == batchId).Select(item => item.BatchNumber).SingleOrDefaultAsync();
            var row = new UnstartLeftoverRowDto { RequisitionId = r.Id, Code = r.Code ?? r.Id.ToString(), BatchNumber = batch ?? "",
                UnstartRepairId = repair.Id, UnstartedAt = repair.AppliedAt, MaterialCount = r.Items.Count(line => line.DeletedAt == null) };
            if (run?.DeletedAt is null) row.Blockers.Add("The recorded Unstart does not have an archived production run.");
            if (await db.ProductionActivities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId && item.DeletedAt == null))
                row.Blockers.Add("This batch has restarted. Its requests must be reviewed with the current run.");
            if (r.Items.Count == 0 || r.Items.Any(line => line.Material == null || line.Material.Kind != kind))
                row.Blockers.Add("The request has missing or mixed material kinds.");
            if (r.Status == RequestStatus.Completed || r.Items.Any(line => line.Status == RequestStatus.Completed || line.QuantityReceived != 0)
                || await db.StockRequisitionIssues.IgnoreQueryFilters().AnyAsync(issue => issue.RequisitionId == r.Id)
                || await db.BinCardInformation.IgnoreQueryFilters().AnyAsync(card => card.RequisitionId == r.Id && card.QuantityIssued != 0))
                row.Blockers.Add("Stock has already been issued or received for this request.");
            if (await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId
                && item.DeletedAt == null && item.ReleasedAt == null))
                row.Blockers.Add("This batch still has active stock reservations. Reconcile them before removing requests.");
            rows.Add(row);
            activities[r.Id] = repair.ActivityId;
        }
        var snapshot = JsonSerializer.Serialize(new { department, kind, actor = identity.UserId, rows,
            requests = requests.Select(r => new { r.Id, r.CreatedAt, r.UpdatedAt, r.DepartmentId, r.RequisitionType, r.Status, r.Approved,
                r.ContentRevision, r.ProductionScheduleProductId, r.ProductionActivityStepId,
                items = r.Items.OrderBy(line => line.Id).Select(line => new { line.Id, line.MaterialId, Kind = (MaterialKind?)line.Material?.Kind, line.Quantity,
                    line.UoMId, line.QuantityReceived, line.IssuedQuantity, line.Status, line.DeletedAt, line.UpdatedAt }) }) });
        return new(new() { PreviewToken = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot))), Requisitions = rows },
            requests, activities, snapshot);
    }
}
