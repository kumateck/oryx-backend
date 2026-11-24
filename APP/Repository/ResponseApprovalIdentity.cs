using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class ResponseApprovalIdentity
{
    internal static async Task Apply(ApplicationDbContext db, IReadOnlyCollection<ApprovalEntity> entities)
    {
        var ids = entities.Where(entity => entity.ModelType == nameof(Response)).Select(entity => entity.Id).ToList();
        if (ids.Count == 0) return;
        // IDs already come from the caller's permitted approval queue. Subject identity must
        // remain readable across production departments and for archived master records.
        var responses = await db.Responses.IgnoreQueryFilters().AsNoTracking().IgnoreAutoIncludes()
            .Include(response => response.BatchManufacturingRecord).ThenInclude(bmr => bmr.ProductionScheduleProduct).ThenInclude(batch => batch.Product)
            .Include(response => response.MaterialBatch).ThenInclude(batch => batch.Material)
            .Include(response => response.MaterialSampling).ThenInclude(sample => sample.MaterialBatch).ThenInclude(batch => batch.Material)
            .AsSplitQuery().Where(response => ids.Contains(response.Id)).ToListAsync();
        var records = responses.Select(response => {
            var product = response.BatchManufacturingRecord?.ProductionScheduleProduct?.Product;
            var material = response.MaterialBatch ?? response.MaterialSampling?.MaterialBatch;
            return new {
                response.Id, response.BatchManufacturingRecordId, response.ProductionActivityStepId,
                ProductBatch = response.BatchManufacturingRecord?.BatchNumber,
                ProductName = product?.Name, ProductCode = product?.Code,
                MaterialBatch = material?.BatchNumber, MaterialName = material?.Material?.Name, MaterialCode = material?.Material?.Code
            };
        }).ToDictionary(response => response.Id);
        var batchIds = records.Values.Select(record => record.BatchManufacturingRecordId).Where(id => id.HasValue).ToList();
        var stages = await db.AnalyticalTestRequests.AsNoTracking().Where(atr => batchIds.Contains(atr.BatchManufacturingRecordId))
            .Select(atr => new { atr.BatchManufacturingRecordId, atr.ProductionActivityStepId, atr.Stage }).ToListAsync();
        foreach (var entity in entities)
        {
            if (!records.TryGetValue(entity.Id, out var record)) continue;
            entity.BatchNumber = record.BatchManufacturingRecordId.HasValue ? record.ProductBatch : record.MaterialBatch;
            entity.ProductName = record.ProductName; entity.ProductCode = record.ProductCode;
            entity.MaterialName = record.MaterialName; entity.MaterialCode = record.MaterialCode;
            entity.ProductStage = stages.Where(stage => stage.BatchManufacturingRecordId == record.BatchManufacturingRecordId
                && stage.ProductionActivityStepId == record.ProductionActivityStepId).Select(stage => (int?)stage.Stage).FirstOrDefault();
            entity.Code = record.ProductCode ?? record.MaterialCode ?? entity.Code;
        }
    }
}
