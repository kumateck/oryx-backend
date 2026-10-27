using System.Data;
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
    public Task<Result<Guid>> InitiateOosInvestigation(InitiateOosInvestigationRequest request, Guid userId) =>
        OosInvestigationInitiation.Run(context, mapper, request, userId);

    public async Task<Result> UpdateOosInvestigation(Guid investigationId, UpdateOosInvestigationRequest request, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
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
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> SubmitToQa(Guid investigationId, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var investigation = await context.OosInvestigations.FirstOrDefaultAsync(o => o.Id == investigationId);
        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        if (investigation.Status != OosInvestigationStatus.Initiated)
            return Error.Conflict("OosInvestigation.State", "Only an initiated investigation may be submitted.");
        if (!CompleteFindings(investigation))
            return Error.Validation("OosInvestigation.Incomplete", "Complete root cause, corrective actions, preventive actions and investigation details first.");

        investigation.Status = OosInvestigationStatus.SubmittedToQa;
        investigation.SubmittedToQaAt = DateTime.UtcNow;
        investigation.SubmittedById = userId;
        investigation.LastUpdatedById = userId;

        context.OosInvestigations.Update(investigation);
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> ReviewByQa(Guid investigationId, ReviewOosInvestigationRequest request, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var investigation = await context.OosInvestigations.FirstOrDefaultAsync(o => o.Id == investigationId);
        if (investigation == null)
            return Error.NotFound("OosInvestigation.NotFound", "OOS investigation not found.");

        if (investigation.Status != OosInvestigationStatus.SubmittedToQa)
            return Error.Conflict("OosInvestigation.State", "Only an investigation submitted to QA may be decided.");
        if (investigation.CreatedById == userId || investigation.SubmittedById == userId)
            return Error.Forbidden("OosInvestigation.Separation", "The author or submitter cannot review their own investigation.");
        if (string.IsNullOrWhiteSpace(request.Comments) || request.Comments.Length > 2000)
            return Error.Validation("OosInvestigation.Comments", "Record a QA decision reason of at most 2000 characters.");

        investigation.QaReviewedAt = DateTime.UtcNow;
        investigation.QaReviewerId = userId;
        investigation.QaReviewComments = request.Comments;
        investigation.LastUpdatedById = userId;

        if (request.Approve)
        {
            if (!CompleteFindings(investigation))
                return Error.Validation("OosInvestigation.Incomplete", "Complete findings are required before QA can approve an investigation.");
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
        if (transaction is not null) await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result<OosInvestigationDto>> GetOosInvestigation(Guid id)
    {
        var investigation = await context.OosInvestigations
            .AsSplitQuery()
            .Include(o => o.CreatedBy)
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

    private static bool CompleteFindings(OosInvestigation investigation) =>
        new[] { investigation.RootCauseAnalysis, investigation.CorrectiveActions,
            investigation.PreventiveActions, investigation.InvestigationDetails }.All(value =>
            !string.IsNullOrWhiteSpace(System.Net.WebUtility.HtmlDecode(
                System.Text.RegularExpressions.Regex.Replace(value ?? "", "<[^>]*>", " ")).Replace('\u00a0', ' ')));
}
