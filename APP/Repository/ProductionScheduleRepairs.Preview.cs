using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ScheduleRepairPlan(ScheduleRepairPreview Preview, Dictionary<Guid, string> Procedures,
    Dictionary<Guid, Guid> PackingStyleOverrides);
internal sealed record RepairPackingStyle(Guid Id, string Name);

internal static partial class ProductionScheduleRepairs
{
    internal static string Permission(ScheduleRepairKind kind) => kind switch
    {
        ScheduleRepairKind.Bom => PermissionKeys.CanRepairBom,
        ScheduleRepairKind.Packaging => PermissionKeys.CanRepairPackaging,
        _ => PermissionKeys.CanRepairProcedure,
    };

    private static async Task<Result> Access(ApplicationDbContext db, Guid scheduleId, Guid productId, Guid actorId, ScheduleRepairKind kind)
    {
        if (!Enum.IsDefined(kind)) return Error.Validation("Repair.Kind", "Choose BOM, packaging or procedure.");
        var grants = await CurrentPermissionSet.Load(db, actorId);
        var department = await db.Users.IgnoreQueryFilters().IgnoreAutoIncludes().Where(user => user.Id == actorId
            && user.DeletedAt == null && !user.IsDisabled && user.Department.Type == DepartmentType.Production)
            .Select(user => user.DepartmentId).SingleOrDefaultAsync();
        if (!department.HasValue || !grants.Contains(PermissionKeys.CanViewProductSchedules)
            || !grants.Contains(PermissionKeys.CanViewPlannedProducts) || !grants.Contains(Permission(kind)))
            return Error.Forbidden("Repair.Permission", "Your current department and permissions do not allow this schedule repair.");
        if (!await db.ProductionScheduleProducts.AnyAsync(batch => batch.ProductionScheduleId == scheduleId && batch.ProductId == productId
            && batch.Product.DepartmentId == department && batch.Product.DeletedAt == null && batch.ProductionSchedule.DeletedAt == null
            && (!batch.ProductionSchedule.DepartmentId.HasValue || batch.ProductionSchedule.DepartmentId == department)))
            return Error.NotFound("Repair.Scope", "The selected product was not found in this schedule and department.");
        return Result.Success();
    }

    private static async Task<ScheduleRepairPlan> Build(ApplicationDbContext db, UserManager<User> users,
        Guid scheduleId, Guid productId, Guid actorId, ScheduleRepairKind kind)
    {
        var preview = new ScheduleRepairPreview { ScheduleId = scheduleId, ProductId = productId, Kind = kind };
        var status = await db.ProductionSchedules.Where(item => item.Id == scheduleId).Select(item => item.Status).SingleAsync();
        if (status is ProductionStatus.Completed or ProductionStatus.Cancelled)
            preview.Blockers.Add("Completed or cancelled schedules cannot be repaired.");
        var procedures = new Dictionary<Guid, string>();
        var packingStyleOverrides = new Dictionary<Guid, Guid>();
        var packingStyles = kind == ScheduleRepairKind.Packaging
            ? await db.ProductPackings.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(item => item.ProductId == productId && item.DeletedAt == null)
                .OrderBy(item => item.Name).ThenBy(item => item.Id)
                .Select(item => new RepairPackingStyle(item.Id, item.Name)).ToListAsync()
            : new List<RepairPackingStyle>();
        var batches = await db.ProductionScheduleProducts.AsNoTracking().IgnoreAutoIncludes()
            .Where(batch => batch.ProductionScheduleId == scheduleId && batch.ProductId == productId).OrderBy(batch => batch.Id).ToListAsync();
        var ids = batches.Select(batch => batch.Id).ToList();
        var runs = await db.ProductionActivities.AsNoTracking().IgnoreAutoIncludes().Where(run => ids.Contains(run.ProductionScheduleProductId)).ToListAsync();
        foreach (var batch in batches)
        {
            var active = runs.Where(run => run.ProductionScheduleProductId == batch.Id).ToList();
            var row = new ScheduleRepairBatchPreview { BatchId = batch.Id, BatchNumber = batch.BatchNumber ?? "", Status = "skipped" };
            preview.Batches.Add(row);
            if (batch.Cancelled) { row.Message = "Cancelled batch"; continue; }
            if (active.Count == 0) { row.Message = "Not started; it will use the saved master when started"; continue; }
            if (active.Count != 1) { Block(row, "Multiple production runs need reconciliation"); continue; }
            var run = active[0]; row.ActivityId = run.Id;
            if (run.Status is ProductionStatus.Completed or ProductionStatus.Cancelled || run.CompletedAt.HasValue)
            { row.Message = "Completed or cancelled run"; continue; }
            if (kind == ScheduleRepairKind.Procedure)
            {
                var inspection = await ProductionProcedureRepair.Inspect(db, users, run.Id);
                if (inspection.IsFailure) { Block(row, inspection.Error.Description); continue; }
                row.Procedure = inspection.Value.Summary;
                procedures[run.Id] = inspection.Value.Snapshot;
                row.EvidenceHash = ProductionMaterialSources.Hash(inspection.Value.Snapshot);
                row.Status = "ready";
            }
            else
            {
                Guid? packingStyleOverride = null;
                string packingStyleMessage = "";
                if (kind == ScheduleRepairKind.Packaging && !packingStyles.Any(style => style.Id == batch.ProductPackingId))
                {
                    var firstStyle = packingStyles.FirstOrDefault();
                    if (firstStyle is null)
                    {
                        Block(row, "Add an active packing style to this product before repairing its packaging.");
                        continue;
                    }
                    packingStyleOverride = firstStyle.Id;
                    packingStyleOverrides[batch.Id] = firstStyle.Id;
                    packingStyleMessage = $"The saved packing style is unavailable. This repair will assign the first active style, '{firstStyle.Name}', to the batch.";
                }
                var material = await ProductionMaterialRepairs.Preview(db, run.Id, actorId,
                    kind == ScheduleRepairKind.Bom ? MaterialKind.Raw : MaterialKind.Package, packingStyleOverride);
                if (material.IsFailure) { Block(row, material.Error.Description); continue; }
                row.Materials = material.Value;
                row.EvidenceHash = material.Value.PreviewHash;
                row.Status = material.Value.Blockers.Count != 0 ? "blocked" : material.Value.HasChanges ? "ready" : "unchanged";
                row.Message = string.Join(" ", new[] { packingStyleMessage, string.Join(" ", material.Value.Blockers) }.Where(message => message.Length != 0));
            }
        }
        if (preview.Batches.Any(row => row.Status == "blocked"))
            preview.Blockers.Add("Resolve the blocked batches before applying this product repair. Nothing will be changed while any batch is blocked.");
        preview.CanApply = preview.Blockers.Count == 0 && preview.Batches.Any(row => row.Status == "ready");
        preview.PreviewHash = ProductionMaterialSources.Hash(new { scheduleId, productId, kind, preview.Batches });
        return new ScheduleRepairPlan(preview, procedures, packingStyleOverrides);
    }

    private static void Block(ScheduleRepairBatchPreview row, string message) { row.Status = "blocked"; row.Message = message; }

    internal static async Task<Result<ScheduleRepairPreview>> Preview(ApplicationDbContext db, UserManager<User> users,
        Guid scheduleId, Guid productId, Guid actorId, ScheduleRepairKind kind)
    {
        var access = await Access(db, scheduleId, productId, actorId, kind);
        if (access.IsFailure) return access.Error;
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead) : null;
        return (await Build(db, users, scheduleId, productId, actorId, kind)).Preview;
    }
}
