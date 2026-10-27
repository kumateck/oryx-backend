using System.Data;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OosInvestigations;
using DOMAIN.Entities.Forms;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class OosInvestigationInitiation
{
    public static async Task<Result<Guid>> Run(ApplicationDbContext context, IMapper mapper,
        InitiateOosInvestigationRequest request, Guid actor)
    {
        if (actor == Guid.Empty || request.MaterialBatchId.HasValue == request.AnalyticalTestRequestId.HasValue)
            return Error.Validation("OosInvestigation.Subject", "Choose exactly one material batch or analytical test request.");
        if (string.IsNullOrWhiteSpace(request.CoaNumber) || request.CoaNumber.Length > 100 ||
            string.IsNullOrWhiteSpace(request.RejectionReason) || request.RejectionReason.Length > 2000 ||
            request.ProductOrMaterialName?.Length > 500 || request.BatchNumber?.Length > 200)
            return Error.Validation("OosInvestigation.Invalid", "A valid certificate reference and rejection reason are required.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var existing = context.OosInvestigations.Where(item =>
            (request.MaterialBatchId.HasValue && item.MaterialBatchId == request.MaterialBatchId) ||
            (request.AnalyticalTestRequestId.HasValue && item.AnalyticalTestRequestId == request.AnalyticalTestRequestId));
        if (await existing.AnyAsync(item => item.Status == OosInvestigationStatus.Initiated ||
                item.Status == OosInvestigationStatus.SubmittedToQa || item.Status == OosInvestigationStatus.PermanentlyRejected || item.CoaNumber == request.CoaNumber.Trim()))
            return Error.Conflict("OosInvestigation.Duplicate", "An investigation already exists for these results. Open the investigation register before trying again.");

        var responses = context.Responses.AsQueryable();
        if (request.MaterialBatchId.HasValue)
        {
            var batch = await context.MaterialBatches.SingleOrDefaultAsync(item => item.Id == request.MaterialBatchId);
            if (batch is null) return Error.NotFound("OosInvestigation.Batch", "The material batch was not found.");
            if (batch.Status is not (BatchStatus.Rejected or BatchStatus.Frozen or BatchStatus.Quarantine))
                return Error.Conflict("OosInvestigation.BatchState", "Only rejected or held material may enter an investigation.");
            responses = responses.Where(item => item.MaterialBatchId == batch.Id);
            var rejection = await ValidateResponse(responses, request.ResponseId);
            if (rejection.IsFailure) return rejection.Errors;
            batch.Status = BatchStatus.Quarantine;
            batch.UpdatedAt = DateTime.UtcNow;
            batch.LastUpdatedById = actor;
        }
        else
        {
            var atr = await context.AnalyticalTestRequests.SingleOrDefaultAsync(item => item.Id == request.AnalyticalTestRequestId);
            if (atr is null) return Error.NotFound("OosInvestigation.Atr", "The analytical test request was not found.");
            if (atr.Status is not (AnalyticalTestStatus.Testing or AnalyticalTestStatus.TestTaken or AnalyticalTestStatus.Rejected))
                return Error.Conflict("OosInvestigation.AtrState", "Only a rejected or tested stage may enter investigation.");
            responses = responses.Where(item => item.BatchManufacturingRecordId == atr.BatchManufacturingRecordId &&
                    item.ProductionActivityStepId == atr.ProductionActivityStepId);
            var rejection = await ValidateResponse(responses, request.ResponseId);
            if (rejection.IsFailure) return rejection.Errors;
            atr.Status = AnalyticalTestStatus.Testing;
            atr.UpdatedAt = DateTime.UtcNow;
            atr.LastUpdatedById = actor;
        }

        var investigation = mapper.Map<OosInvestigation>(request);
        investigation.Id = Guid.NewGuid();
        investigation.CoaNumber = request.CoaNumber.Trim();
        investigation.RejectionReason = request.RejectionReason.Trim();
        investigation.CreatedAt = DateTime.UtcNow;
        investigation.CreatedById = actor;
        investigation.Status = OosInvestigationStatus.Initiated;
        context.OosInvestigations.Add(investigation);
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return investigation.Id;
    }

    private static async Task<Result> ValidateResponse(IQueryable<Response> responses, Guid? responseId)
    {
        var candidates = responseId.HasValue ? responses.Where(item => item.Id == responseId) : responses.Where(item => item.Rejected);
        var candidate = await candidates.OrderByDescending(item => item.ReworkSequence).ThenByDescending(item => item.CreatedAt).FirstOrDefaultAsync();
        if (candidate is null || !candidate.Rejected || candidate.Approved || candidate.RejectionOutcome == ResponseRejectionOutcome.Disposed)
            return Error.Conflict("OosInvestigation.Results", "The selected test must be rejected for rework. Final disposal cannot be reopened.");
        var currentId = await responses.Where(item => item.FormId == candidate.FormId)
            .OrderByDescending(item => item.ReworkSequence).ThenByDescending(item => item.CreatedAt)
            .Select(item => item.Id).FirstOrDefaultAsync();
        return currentId == candidate.Id ? Result.Success()
            : Error.Conflict("OosInvestigation.ResultsChanged", "A later cycle exists for this test. Review its current results first.");
    }
}
