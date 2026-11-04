using System.Text.Json;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

public sealed partial class ProductionUnstartService
{
    private sealed record Plan(ProductionActivity Activity, ProductionSchedule Schedule, List<ProductionActivityStep> Steps,
        List<BatchManufacturingRecord> Bmrs, List<BatchPackagingRecord> Bprs, List<MaterialBatchReservedQuantity> Reservations,
        ProductionUnstartPreviewDto Preview, List<ProductionMaterialConfiguration> Configurations);

    private async Task<Result<Plan>> Build(Guid activityId)
    {
        var run = await db.ProductionActivities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(item => item.ProductionScheduleProduct).ThenInclude(item => item.Product)
            .Include(item => item.ProductionScheduleProduct).ThenInclude(item => item.ProductionSchedule).SingleOrDefaultAsync(item => item.Id == activityId);
        if (run is null) return Error.NotFound("Unstart.Activity", "Production run not found.");
        var batch = run.ProductionScheduleProduct;
        var schedule = batch.ProductionSchedule;
        var preview = new ProductionUnstartPreviewDto { ActivityId = run.Id, BatchId = batch.Id, ScheduleId = schedule.Id,
            ProductName = batch.Product.Name, BatchNumber = batch.BatchNumber ?? "", ScheduleStatusBefore = (int)schedule.Status };
        var blockers = preview.Blockers;
        var prior = await db.ProductionUnstartRepairs.AsNoTracking().SingleOrDefaultAsync(item => item.ActivityId == activityId);
        if (run.DeletedAt.HasValue || prior is not null)
        {
            preview.AlreadyUnstarted = prior is not null;
            blockers.Add(prior is not null ? "This production run has already been unstarted." : "This production run is archived and cannot be repaired.");
        }
        if (batch.Cancelled || schedule.DeletedAt.HasValue || schedule.Status is ProductionStatus.Completed or ProductionStatus.Cancelled
            || run.CompletedAt.HasValue || run.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
            blockers.Add("The batch and schedule must be open and not cancelled or completed.");
        var steps = await db.ProductionActivitySteps.IgnoreQueryFilters().IgnoreAutoIncludes().Where(item => item.ProductionActivityId == activityId)
            .OrderBy(item => item.Order).ThenBy(item => item.Id).ToListAsync();
        var bmrs = await db.BatchManufacturingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().Where(item => item.ProductionScheduleProductId == batch.Id
            && steps.Select(step => step.Id).Contains(item.ProductionActivityStepId)).OrderBy(item => item.Id).ToListAsync();
        var bprs = await db.BatchPackagingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().Where(item => item.ProductionScheduleProductId == batch.Id
            && steps.Select(step => step.Id).Contains(item.ProductionActivityStepId)).OrderBy(item => item.Id).ToListAsync();
        var first = steps.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(preview.BatchNumber))
            preview.BatchNumber = bmrs.Select(item => item.BatchNumber).Concat(bprs.Select(item => item.BatchNumber))
                .FirstOrDefault(number => !string.IsNullOrWhiteSpace(number)) ?? "";
        if (first is null || bmrs.Count != 1 || bprs.Count != 1 || bmrs.Any(item => item.ProductionActivityStepId != first.Id)
            || bprs.Any(item => item.ProductionActivityStepId != first.Id))
            blockers.Add("One matching draft BMR and BPR on the first production step are required.");
        var bmrIds = bmrs.Select(item => item.Id).ToList();
        var bprIds = bprs.Select(item => item.Id).ToList();
        if (await db.BatchManufacturingRecords.AnyAsync(item => item.ProductionScheduleProductId == batch.Id && !bmrIds.Contains(item.Id))
            || await db.BatchPackagingRecords.AnyAsync(item => item.ProductionScheduleProductId == batch.Id && !bprIds.Contains(item.Id)))
            blockers.Add("Other active BMR or BPR documents exist for this batch and must be reconciled first.");
        if (bmrs.Any(item => item.IssuedDate.HasValue || item.IssuedById.HasValue || item.DeletedAt.HasValue
            || item.Status != BatchManufacturingStatus.New || item.SampledQuantity != 0)
            || bprs.Any(item => item.IssuedDate.HasValue || item.IssuedById.HasValue || item.DeletedAt.HasValue))
            blockers.Add("The BMR and BPR must both be unissued, unsampled draft records.");
        if (steps.Any(step => step.DeletedAt.HasValue || step.CompletedAt.HasValue
            || (step.Id == first?.Id ? step.Status is not (ProductionStatus.New or ProductionStatus.InProgress)
                : step.StartedAt.HasValue || step.Status != ProductionStatus.New)))
            blockers.Add("Only the initial BMR preparation step may have started. Later or completed steps prevent unstarting.");
        var reservations = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material).Include(item => item.UoM)
            .Where(item => item.ProductionScheduleProductId == batch.Id && item.ProductionUnstartRepairId == null && item.ReleasedAt == null).OrderBy(item => item.Id).ToListAsync();
        foreach (var reservation in reservations)
        {
            if (reservation.DeletedAt.HasValue || reservation.Quantity <= 0 || reservation.MaterialBatch.DeletedAt.HasValue
                || reservation.UoMId != reservation.MaterialBatch.UoMId || reservation.MaterialBatch.Material.DeletedAt.HasValue)
            { blockers.Add("Reservations contain consumed, deleted or invalid stock evidence."); break; }
            var production = await db.Warehouses.IgnoreQueryFilters().AnyAsync(item => item.Id == reservation.WarehouseId
                && item.Type == WarehouseType.Production && item.DepartmentId == batch.Product.DepartmentId && item.DeletedAt == null);
            var stored = await db.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Where(item => item.MaterialBatchId == reservation.MaterialBatchId && item.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId
                    && item.DeletedAt == null && item.UoMId == reservation.UoMId
                    && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type != WarehouseType.Production)
                .SumAsync(item => item.Quantity);
            if (!production || stored < reservations.Where(item => item.MaterialBatchId == reservation.MaterialBatchId
                && item.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId).Sum(item => item.Quantity))
            { blockers.Add("The reservations cannot be proven to remain unissued on their source shelves."); break; }
        }
        blockers.AddRange(await Dependencies(run, steps, bmrs, bprs));
        var otherStarted = await db.ProductionActivities.IgnoreAutoIncludes().AnyAsync(item => item.Id != run.Id
            && item.ProductionScheduleProduct.ProductionScheduleId == schedule.Id);
        preview.ScheduleStatusAfter = (int)(otherStarted ? ProductionStatus.InProgress : ProductionStatus.New);
        preview.StepsToArchive = steps.Count;
        preview.DraftDocumentsToArchive = bmrs.Count + bprs.Count;
        preview.Reservations = reservations.Select(item => new ProductionUnstartReservationDto { Id = item.Id,
            MaterialCode = item.MaterialBatch.Material.Code, MaterialName = item.MaterialBatch.Material.Name,
            LotNumber = item.MaterialBatch.BatchNumber, Unit = item.UoM.Symbol, Quantity = item.Quantity }).ToList();
        preview.Blockers = blockers.Distinct().ToList();
        preview.CanApply = preview.Blockers.Count == 0;
        var configurations = await db.ProductionMaterialConfigurations.IgnoreAutoIncludes()
            .Where(item => item.ProductionScheduleProductId == batch.Id).OrderBy(item => item.Id).ToListAsync();
        var plan = new Plan(run, schedule, steps, bmrs, bprs, reservations, preview, configurations);
        preview.PreviewToken = Hash(Snapshot(plan));
        return plan;
    }

    private static string Snapshot(Plan plan) => JsonSerializer.Serialize(new {
        Activity = new { plan.Activity.Id, plan.Activity.ProductionScheduleProductId, plan.Activity.Status, plan.Activity.StartedAt, plan.Activity.CompletedAt, plan.Activity.DeletedAt, plan.Activity.UpdatedAt },
        Batch = new { plan.Activity.ProductionScheduleProduct.BatchNumber, plan.Activity.ProductionScheduleProduct.Cancelled },
        Schedule = new { plan.Schedule.Id, plan.Schedule.Status }, NextScheduleStatus = plan.Preview.ScheduleStatusAfter,
        Steps = plan.Steps.Select(item => new { item.Id, item.Order, item.OperationId, item.Status, item.StartedAt, item.CompletedAt, item.DeletedAt, item.UpdatedAt }),
        Bmrs = plan.Bmrs.Select(item => new { item.Id, item.BatchNumber, item.BatchQuantity, item.ManufacturingDate, item.ExpiryDate, item.Status, item.SampledQuantity, item.IssuedDate, item.IssuedById, item.DeletedAt, item.UpdatedAt }),
        Bprs = plan.Bprs.Select(item => new { item.Id, item.BatchNumber, item.BatchQuantity, item.ManufacturingDate, item.ExpiryDate, item.ProductPackingId, item.IssuedDate, item.IssuedById, item.DeletedAt, item.UpdatedAt }),
        Reservations = plan.Reservations.Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseId, item.WarehouseLocationShelfId, item.Quantity, item.UoMId, item.DeletedAt, item.ReleasedAt, item.ProductionUnstartRepairId }),
        Configurations = plan.Configurations.Select(item => new { item.Id, item.Kind, item.Revision, item.SourceHash, item.RequirementsJson, item.DepartmentId, item.StorageWarehouseId, item.ProductionWarehouseId, item.DeletedAt }),
        plan.Preview.Blockers });
}
