using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ResponseFinalApproval
{
    internal static async Task<Result> ApplyAsync(
        ApplicationDbContext context,
        Response response,
        Guid? releasedById)
    {
        response.Approved = true;
        response.Rejected = false;
        BatchManufacturingRecord productBatchToApprove = null;

        if (response.MaterialBatchId.HasValue)
        {
            var rawDataExists = await context.MaterialAnalyticalRawData
                .AnyAsync(item => item.MaterialStandardTestProcedure.MaterialId
                    == response.MaterialBatch.MaterialId);
            if (!rawDataExists)
                return Error.NotFound(
                    "Response.MaterialAnalyticalRawDataNotFound",
                    $"Analytical raw data for material batch {response.MaterialBatchId} was not found.");

            if (response.MaterialBatch is null)
                return Error.NotFound(
                    "Response.BatchNotFound",
                    $"Response batch {response.MaterialBatchId} was not found.");

            response.MaterialBatch.Status = BatchStatus.Approved;
        }

        if (response.BatchManufacturingRecordId.HasValue)
        {
            if (response.BatchManufacturingRecord is null)
                return Error.NotFound(
                    "Response.BmrNotFound",
                    $"Response BMR {response.BatchManufacturingRecordId} was not found.");

            var rawDataExists = await context.ProductAnalyticalRawData
                .AnyAsync(item => item.ProductStandardTestProcedure.ProductId
                    == response.BatchManufacturingRecord.ProductionScheduleProduct.ProductId);
            if (!rawDataExists)
                return Error.NotFound(
                    "Response.ProductAnalyticalRawDataNotFound",
                    $"Analytical raw data for BMR {response.BatchManufacturingRecordId} was not found.");

            productBatchToApprove = response.BatchManufacturingRecord;
        }

        if (response.ProductionActivityStepId.HasValue)
        {
            var productionActivityStep = await context.ProductionActivitySteps
                .FirstOrDefaultAsync(item => item.Id == response.ProductionActivityStepId);
            if (productionActivityStep is null)
                return Error.NotFound(
                    "Response.ProductionActivityStepNotFound",
                    "Production activity step was not found.");

            var atr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(item =>
                item.ProductionActivityStepId == response.ProductionActivityStepId
                && item.BatchManufacturingRecordId == response.BatchManufacturingRecordId);
            if (atr is null)
                return Error.NotFound(
                    "Response.Atr",
                    $"Analytical test request for step {response.ProductionActivityStepId} was not found.");

            productionActivityStep.CompletedAt = DateTime.UtcNow;
            productionActivityStep.Status = ProductionStatus.Completed;
            atr.ReleasedAt = DateTime.UtcNow;
            atr.ReleasedById = releasedById;
            atr.Status = AnalyticalTestStatus.Released;

            var hasUnreleasedStage = await context.AnalyticalTestRequests.AnyAsync(item =>
                item.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                && item.Id != atr.Id
                && item.Status != AnalyticalTestStatus.Released);
            if (productBatchToApprove is not null && !hasUnreleasedStage)
                productBatchToApprove.Status = BatchManufacturingStatus.Approved;
        }
        else if (productBatchToApprove is not null)
        {
            productBatchToApprove.Status = BatchManufacturingStatus.Approved;
        }

        return Result.Success();
    }
}
