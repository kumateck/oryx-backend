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
        if (response.RoutineTrackId.HasValue)
        {
            var track = await context.RoutineTracks
                .Include(item => item.RoutineSample).ThenInclude(item => item.RoutineExecution)
                .FirstOrDefaultAsync(item => item.Id == response.RoutineTrackId);
            if (track is null)
                return Error.NotFound("Response.RoutineTrack", "Routine track was not found.");
            var sampleIds = await context.RoutineSamples
                .Where(item => item.RoutineExecutionId == track.RoutineSample.RoutineExecutionId)
                .Select(item => item.Id).ToListAsync();
            var otherResponses = await context.Responses
                .Where(item => item.RoutineTrackId.HasValue &&
                    sampleIds.Contains(item.RoutineTrack.RoutineSampleId) &&
                    item.Id != response.Id).ToListAsync();
            var trackCount = await context.RoutineTracks
                .CountAsync(item => sampleIds.Contains(item.RoutineSampleId));
            if (sampleIds.Count > 0 && otherResponses.Count + 1 == trackCount &&
                otherResponses.All(item => item.Approved && !item.Rejected))
            {
                track.RoutineSample.RoutineExecution.Status =
                    DOMAIN.Entities.QualityRoutines.RoutineStatus.Approved;
                track.RoutineSample.RoutineExecution.DoneById ??= releasedById;
                track.RoutineSample.RoutineExecution.DoneAt = DateTime.UtcNow;
                context.RoutineAuditEvents.Add(new DOMAIN.Entities.QualityRoutines.RoutineAuditEvent
                {
                    Id = Guid.NewGuid(), RoutineExecutionId = track.RoutineSample.RoutineExecutionId,
                    ActorId = releasedById ?? response.CreatedById ?? Guid.Empty,
                    OccurredAt = DateTime.UtcNow, Action = "Approved",
                    Detail = "All required routine analysis tracks approved."
                });
            }
        }

        if (response.MaterialBatchId.HasValue)
        {
            var batch = response.MaterialBatch ?? await context.MaterialBatches
                .FirstOrDefaultAsync(item => item.Id == response.MaterialBatchId);
            if (batch is null)
                return Error.NotFound("Response.BatchNotFound", "Material batch was not found.");
            var readiness = await QualityAnalysisReadiness.MaterialAsync(
                context, response.MaterialBatchId.Value);
            if (readiness.IsFailure) return readiness.Errors;
            if (readiness.Value) batch.Status = BatchStatus.Approved;
        }

        if (response.BatchManufacturingRecordId.HasValue)
        {
            if (response.BatchManufacturingRecord is null)
                return Error.NotFound(
                    "Response.BmrNotFound",
                    $"Response BMR {response.BatchManufacturingRecordId} was not found.");

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

            var readiness = await QualityAnalysisReadiness.ProductStageAsync(context, atr);
            if (readiness.IsFailure) return readiness.Errors;
            if (!readiness.Value) return Result.Success();

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
            var hasUnreleasedStage = await context.AnalyticalTestRequests.AnyAsync(item =>
                item.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                && item.Status != AnalyticalTestStatus.Released);
            if (!hasUnreleasedStage)
                productBatchToApprove.Status = BatchManufacturingStatus.Approved;
        }

        return Result.Success();
    }
}
