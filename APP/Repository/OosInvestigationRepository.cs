using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OosInvestigations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class OosInvestigationRepository(ApplicationDbContext context, IMapper mapper)
    : IOosInvestigationRepository
{
    public async Task<Result<Guid>> InitiateOosInvestigation(InitiateOosInvestigationRequest request, Guid userId)
    {
        var investigation = mapper.Map<OosInvestigation>(request);
        investigation.CreatedById = userId;
        investigation.Status = OosInvestigationStatus.Initiated;

        if (request.AnalyticalTestRequestId.HasValue)
        {
            var atr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(a => a.Id == request.AnalyticalTestRequestId.Value);
            if (atr != null)
            {
                atr.Status = AnalyticalTestStatus.Testing;
                context.AnalyticalTestRequests.Update(atr);
            }
        }

        if (request.MaterialBatchId.HasValue)
        {
            var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == request.MaterialBatchId.Value);
            if (batch != null)
            {
                batch.Status = BatchStatus.Quarantine;
                context.MaterialBatches.Update(batch);
            }
        }

        await context.OosInvestigations.AddAsync(investigation);
        await context.SaveChangesAsync();
        return investigation.Id;
    }

    public async Task<Result> UpdateOosInvestigation(Guid investigationId, UpdateOosInvestigationRequest request, Guid userId)
    {
        var investigation = await context.OosInvestigations.FirstOrDefaultAsync(o => o.Id == investigationId);
        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        if (investigation.Status != OosInvestigationStatus.Initiated)
            return Error.Validation(
                "OosInvestigation.NotEditable",
                "Only an investigation that has not yet been submitted to QA can be edited."
            );

        investigation.ProductOrMaterialName = request.ProductOrMaterialName;
        investigation.BatchNumber = request.BatchNumber;
        investigation.RootCauseAnalysis = request.RootCauseAnalysis;
        investigation.CorrectiveActions = request.CorrectiveActions;
        investigation.PreventiveActions = request.PreventiveActions;
        investigation.InvestigationDetails = request.InvestigationDetails;
        investigation.LastUpdatedById = userId;

        context.OosInvestigations.Update(investigation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SubmitToQa(Guid investigationId, Guid userId)
    {
        var investigation = await context.OosInvestigations.FirstOrDefaultAsync(o => o.Id == investigationId);
        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        investigation.Status = OosInvestigationStatus.SubmittedToQa;
        investigation.SubmittedToQaAt = DateTime.UtcNow;
        investigation.SubmittedById = userId;
        investigation.LastUpdatedById = userId;

        context.OosInvestigations.Update(investigation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReviewByQa(Guid investigationId, ReviewOosInvestigationRequest request, Guid userId)
    {
        var investigation = await context.OosInvestigations.FirstOrDefaultAsync(o => o.Id == investigationId);
        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        investigation.QaReviewedAt = DateTime.UtcNow;
        investigation.QaReviewerId = userId;
        investigation.QaReviewComments = request.Comments;
        investigation.LastUpdatedById = userId;

        if (request.Approve)
        {
            if (investigation.AnalyticalTestRequestId.HasValue)
            {
                var targetAtr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(
                    item => item.Id == investigation.AnalyticalTestRequestId);
                if (targetAtr is null)
                    return Error.NotFound("OosInvestigation.Atr", "Linked ATR was not found.");
                var readiness = await QualityAnalysisReadiness.ProductStageAsync(context, targetAtr);
                if (readiness.IsFailure) return readiness.Errors;
                if (!readiness.Value)
                    return Error.Conflict("OosInvestigation.QcPending",
                        "Required analysis remains incomplete.");
            }
            if (investigation.MaterialBatchId.HasValue)
            {
                var readiness = await QualityAnalysisReadiness.MaterialAsync(
                    context, investigation.MaterialBatchId.Value);
                if (readiness.IsFailure) return readiness.Errors;
                if (!readiness.Value)
                    return Error.Conflict("OosInvestigation.QcPending",
                        "Required analysis remains incomplete.");
            }
            investigation.Status = OosInvestigationStatus.QaApproved;

            if (investigation.AnalyticalTestRequestId.HasValue)
            {
                var atr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(a => a.Id == investigation.AnalyticalTestRequestId.Value);
                if (atr != null)
                {
                    atr.Status = AnalyticalTestStatus.Released;
                    context.AnalyticalTestRequests.Update(atr);
                }
            }

            if (investigation.MaterialBatchId.HasValue)
            {
                var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == investigation.MaterialBatchId.Value);
                if (batch != null)
                {
                    batch.Status = BatchStatus.Available;
                    context.MaterialBatches.Update(batch);
                }
            }
        }
        else
        {
            investigation.Status = OosInvestigationStatus.PermanentlyRejected;

            if (investigation.AnalyticalTestRequestId.HasValue)
            {
                var atr = await context.AnalyticalTestRequests.FirstOrDefaultAsync(a => a.Id == investigation.AnalyticalTestRequestId.Value);
                if (atr != null)
                {
                    atr.Status = AnalyticalTestStatus.Rejected;
                    context.AnalyticalTestRequests.Update(atr);
                }
            }

            if (investigation.MaterialBatchId.HasValue)
            {
                var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == investigation.MaterialBatchId.Value);
                if (batch != null)
                {
                    batch.Status = BatchStatus.Rejected;
                    batch.DateRejected = DateTime.UtcNow;
                    context.MaterialBatches.Update(batch);
                }
            }
        }

        context.OosInvestigations.Update(investigation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<OosInvestigationDto>> GetOosInvestigation(Guid id)
    {
        var investigation = await context.OosInvestigations
            .AsSplitQuery()
            .Include(o => o.SubmittedBy)
            .Include(o => o.QaReviewer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        return mapper.Map<OosInvestigationDto>(investigation);
    }

    public async Task<Result<Paginateable<IEnumerable<OosInvestigationDto>>>> GetOosInvestigations(
        int page,
        int pageSize,
        string searchQuery,
        OosInvestigationStatus? status
    )
    {
        var query = context.OosInvestigations
            .AsSplitQuery()
            .Include(o => o.SubmittedBy)
            .Include(o => o.QaReviewer)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                o => o.CoaNumber,
                o => o.ProductOrMaterialName,
                o => o.BatchNumber,
                o => o.RejectionReason
            );
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<OosInvestigationDto>
        );
    }
}
