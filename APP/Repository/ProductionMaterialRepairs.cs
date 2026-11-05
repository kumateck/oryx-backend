using System.Text.Json;
using APP.IRepository;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ProductionMaterialRepairs
{
    internal static async Task<Result<ProductionMaterialRepairPreview>> Preview(ApplicationDbContext context,
        Guid activityId, Guid actorId, MaterialKind kind)
    {
        var batchId = await context.ProductionActivities.IgnoreQueryFilters().Where(item => item.Id == activityId)
            .Select(item => item.ProductionScheduleProductId).SingleOrDefaultAsync();
        var access = await ProductionMaterialSources.Authorize(context, actorId, batchId, kind);
        if (access.IsFailure) return access.Error;
        var plan = await ProductionMaterialRepairPlanning.Build(context, activityId, kind);
        return plan.IsSuccess ? plan.Value.Preview : plan.Error;
    }

    internal static async Task<Result<ProductionMaterialRepairResult>> Apply(ApplicationDbContext context,
        IApprovalRepository approvals, Guid activityId, Guid actorId, MaterialKind kind, ApplyProductionMaterialRepair request)
    {
        if (request.RequestId == Guid.Empty || request.PreviewHash?.Length != 64 || string.IsNullOrWhiteSpace(request.Reason)
            || request.Reason.Trim().Length < 5 || request.Reason.Length > 2000)
            return Error.Validation("Repair.Input", "A preview, unique request ID and a reason of 5–2000 characters are required.");
        try
        {
            await using var transaction = await ProductionMaterialTransaction.Begin(context);
            var batchId = await context.ProductionActivities.IgnoreQueryFilters().Where(item => item.Id == activityId)
                .Select(item => item.ProductionScheduleProductId).SingleOrDefaultAsync();
            var access = await ProductionMaterialSources.Authorize(context, actorId, batchId, kind);
            if (access.IsFailure) return access.Error;
            var previous = await context.ProductionMaterialRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RequestId);
            if (previous is not null)
            {
                if (previous.ActorId != actorId || previous.ProductionScheduleProductId != batchId || previous.Kind != kind
                    || previous.PreviewHash != request.PreviewHash || previous.Reason != request.Reason.Trim())
                    return Error.Conflict("Repair.RequestId", "This request ID belongs to a different repair.");
                var saved = JsonSerializer.Deserialize<ProductionMaterialRepairResult>(previous.ResultJson);
                return saved is not null ? saved : Error.Conflict("Repair.Result", "The saved repair result could not be verified.");
            }
            var planned = await ProductionMaterialRepairPlanning.Build(context, activityId, kind);
            if (planned.IsFailure) return planned.Error;
            var plan = planned.Value;
            if (request.PreviewHash != plan.Preview.PreviewHash)
                return Error.Conflict("Repair.StalePreview", "The saved product or batch stock changed. Get a fresh preview before applying.");
            if (plan.Preview.Blockers.Count != 0)
                return Error.Conflict("Repair.Blocked", string.Join(" ", plan.Preview.Blockers));
            if (!plan.Preview.HasChanges)
                return new ProductionMaterialRepairResult { Changed = false, Revision = plan.Configuration?.Revision ?? 0 };
            var before = ProductionMaterialSources.Json(new
            {
                Configuration = plan.Configuration?.RequirementsJson,
                SourceHash = plan.Configuration?.SourceHash, Revision = plan.Configuration?.Revision,
                Reservations = plan.Reservations.Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseId,
                    item.WarehouseLocationShelfId, item.Quantity, item.UoMId }),
                Requisition = plan.Requisition is null ? null : new
                {
                    plan.Requisition.Id, plan.Requisition.ContentRevision, plan.Requisition.Status, plan.Requisition.Approved,
                    Items = plan.Requisition.Items.Select(item => new { item.Id, item.MaterialId, item.UoMId, item.Quantity, item.Status }),
                    Approvals = plan.Requisition.Approvals.Select(item => new { item.Id, item.ContentRevision, item.ApprovalId,
                        item.UserId, item.RoleId, item.Required, item.Order, item.Status, item.ApprovalTime, item.ApprovedById, item.Comments })
                }
            });
            // Recheck current grants after all preflight work, immediately before changing records.
            access = await ProductionMaterialSources.Authorize(context, actorId, batchId, kind);
            if (access.IsFailure) return access.Error;
            ProductionMaterialAllocator.Apply(context, plan.Source, plan.Reservations, plan.Allocation.Allocations);
            var configuration = plan.Configuration ?? new ProductionMaterialConfiguration
            { ProductionScheduleProductId = batchId, Kind = kind };
            if (plan.Configuration is null) context.ProductionMaterialConfigurations.Add(configuration);
            configuration.Revision++;
            configuration.SourceHash = plan.Source.SourceHash;
            configuration.DepartmentId = plan.Source.DepartmentId;
            configuration.StorageWarehouseId = plan.Source.StorageId;
            configuration.ProductionWarehouseId = plan.Source.ProductionId;
            configuration.RequirementsJson = ProductionMaterialSources.Json(plan.Source.Requirements);
            if (plan.Preview.ApprovalRequired && plan.Requisition is not null)
            {
                // Keep the document and old approval rows. Decisions are filtered by content revision.
                plan.Requisition.ContentRevision++;
                plan.Requisition.Approved = false;
                plan.Requisition.Status = RequestStatus.New;
                var remaining = plan.Source.Requirements.ToDictionary(item => item.MaterialId);
                foreach (var item in plan.Requisition.Items.Where(item => item.DeletedAt == null).ToList())
                {
                    if (remaining.Remove(item.MaterialId, out var requirement))
                    { item.Quantity = requirement.Quantity; item.UoMId = requirement.UnitId; item.Status = RequestStatus.New; }
                    else { item.DeletedAt = DateTime.UtcNow; item.RetiredByRepairId = request.RequestId; }
                }
                foreach (var requirement in remaining.Values)
                    context.RequisitionItems.Add(new RequisitionItem { RequisitionId = plan.Requisition.Id,
                        MaterialId = requirement.MaterialId, UoMId = requirement.UnitId, Quantity = requirement.Quantity, Status = RequestStatus.New });
            }
            await context.SaveChangesAsync();
            if (plan.Preview.ApprovalRequired && plan.Requisition is not null)
                await approvals.CreateInitialApprovalsAsync(kind == MaterialKind.Raw ? "RawStockRequisition" : "PackageStockRequisition", plan.Requisition.Id, actorId);
            var savedReservations = await context.MaterialBatchReservedQuantities.AsNoTracking().IgnoreAutoIncludes()
                .Include(item => item.MaterialBatch).Include(item => item.UoM)
                .Where(item => item.ProductionScheduleProductId == batchId && item.MaterialBatch.Material.Kind == kind).ToListAsync();
            var expected = new Requisition { Items = plan.Source.Requirements.Select(item => new RequisitionItem
            { MaterialId = item.MaterialId, Quantity = item.Quantity, UoMId = item.UnitId,
                UoM = new DOMAIN.Entities.Base.UnitOfMeasure { Id = item.UnitId, Symbol = item.Unit } }).ToList() };
            var consistent = StockIssueQuantityValidator.Validate(expected, savedReservations);
            if (consistent.IsFailure) return consistent.Error;
            if (plan.Requisition is not null)
            {
                var savedRequisition = await context.Requisitions.AsNoTracking().IgnoreAutoIncludes().Include(item => item.Items)
                    .SingleAsync(item => item.Id == plan.Requisition.Id);
                if (!ProductionMaterialRepairPlanning.Matches(savedRequisition, plan.Source.Requirements))
                    return Error.Conflict("Repair.Verification", "The corrected requisition could not be reconciled. Nothing was applied.");
            }
            var result = new ProductionMaterialRepairResult { RepairId = request.RequestId, Changed = true,
                Revision = configuration.Revision, ApprovalRequired = plan.Preview.ApprovalRequired };
            context.ProductionMaterialRepairs.Add(new ProductionMaterialRepair
            {
                Id = request.RequestId, ActorId = actorId, ProductionScheduleProductId = batchId, Kind = kind,
                PreviewHash = request.PreviewHash, Reason = request.Reason.Trim(), BeforeJson = before,
                AfterJson = ProductionMaterialSources.Json(new { configuration.Revision, plan.Source.SourceHash,
                    plan.Source.DefinitionJson, plan.Source.Requirements, plan.Allocation.Allocations, RequisitionId = plan.Requisition?.Id, ContentRevision = plan.Requisition?.ContentRevision }),
                ResultJson = ProductionMaterialSources.Json(result)
            });
            var logReason = request.Reason.Trim().Length <= 700 ? $" Reason: {request.Reason.Trim()}" : " The full reason is recorded in the repair audit.";
            context.ProductionActivityLogs.Add(new ProductionActivityLog { ProductionActivityId = activityId, UserId = actorId,
                Timestamp = DateTime.UtcNow, Message = $"{(kind == MaterialKind.Raw ? "BOM" : "Packaging")} repaired from saved product configuration. Repair {request.RequestId}; revision {configuration.Revision}.{logReason}" });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return result;
        }
        catch (DbUpdateException)
        {
            return Error.Conflict("Repair.Conflict", "The repair could not be committed. Nothing was changed. Reload the batch and preview again.");
        }
        catch (Npgsql.PostgresException)
        {
            return Error.Conflict("Repair.Busy", "Production stock is busy or changed. Nothing was applied. Reload and preview again.");
        }
    }

    /// <summary>
    /// Reserves a batch's materials when production starts. Raw materials are required: the start is refused
    /// if any is short. Packaging is used later in the run, so, as before configurations were introduced, a
    /// packaging shortage does not block the start: packaging is reserved now only when all of it is
    /// available, otherwise it is left for the later packaging requisition.
    /// </summary>
    internal static async Task<Result> Initialize(ApplicationDbContext context, Guid batchId)
    {
        foreach (var kind in new[] { MaterialKind.Raw, MaterialKind.Package })
        {
            var required = kind == MaterialKind.Raw;
            var configuration = await context.ProductionMaterialConfigurations.IgnoreQueryFilters()
                .SingleOrDefaultAsync(item => item.ProductionScheduleProductId == batchId && item.Kind == kind);
            if (configuration is not null && (configuration.DeletedAt is null
                || !await context.ProductionUnstartRepairs.AnyAsync(repair => repair.BatchId == batchId && repair.AppliedAt == configuration.DeletedAt)))
            {
                if (!required) continue;
                return Error.Conflict("Production.Configuration", "This batch already has an applied configuration.");
            }
            var loaded = await ProductionMaterialSources.Load(context, batchId, kind);
            if (loaded.IsFailure)
            {
                if (!required) continue;
                return loaded.Error;
            }
            var source = loaded.Value;
            if (await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId
                && item.MaterialBatch.Material.Kind == kind && item.ReleasedAt == null))
            {
                if (!required) continue;
                return Error.Conflict("Production.Reservations", "Existing material allocations need reconciliation before starting this batch.");
            }
            var allocation = await ProductionMaterialAllocator.Plan(context, source, []);
            if (source.Blockers.Count != 0 || allocation.Blockers.Count != 0 || allocation.Shortages.Values.Any(value => value > 0))
            {
                if (!required) continue;
                return Error.Validation("Production.Materials", string.Join(" ", source.Blockers.Concat(allocation.Blockers).Concat(allocation.ShortageDetails)
                    .Append("All raw materials must have valid configuration and enough unreserved, usable stock before starting.")));
            }
            ProductionMaterialAllocator.Apply(context, source, [], allocation.Allocations);
            if (configuration is null)
            {
                configuration = new ProductionMaterialConfiguration { ProductionScheduleProductId = batchId, Kind = kind };
                context.ProductionMaterialConfigurations.Add(configuration);
            }
            configuration.Revision++;
            configuration.SourceHash = source.SourceHash;
            configuration.DepartmentId = source.DepartmentId;
            configuration.StorageWarehouseId = source.StorageId;
            configuration.ProductionWarehouseId = source.ProductionId;
            configuration.RequirementsJson = ProductionMaterialSources.Json(source.Requirements);
            configuration.DeletedAt = null;
            configuration.LastDeletedById = null;
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
