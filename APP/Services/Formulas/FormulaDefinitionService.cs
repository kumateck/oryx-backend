using System.Text.Json;
using System.Data;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed partial class FormulaDefinitionService(
    ApplicationDbContext context,
    IFormulaCalculationClient calculationClient) : IFormulaDefinitionService
{
    public async Task<Result<IReadOnlyList<FormulaRevisionDto>>> GetByQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default)
    {
        var link = await context.QuestionFormulaDefinitions.AsNoTracking()
            .Include(item => item.FormulaDefinition).ThenInclude(item => item.Revisions)
            .SingleOrDefaultAsync(item => item.QuestionId == questionId, cancellationToken);
        if (link is null) return Result.Success<IReadOnlyList<FormulaRevisionDto>>([]);
        return Result.Success<IReadOnlyList<FormulaRevisionDto>>(link.FormulaDefinition.Revisions
            .OrderByDescending(item => item.Revision)
            .Select(item => FormulaDefinitionMapping.ToDto(
                item, questionId, link.FormulaDefinition.PresentationPreset)).ToList());
    }

    public async Task<Result<FormulaRevisionDto>> CreateDraftAsync(
        Guid questionId, FormulaRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var normalized = FormulaDefinitionDraft.Normalize(request);
        if (normalized is null) return FormulaDefinitionErrors.InvalidDraft;
        var question = await context.Questions.SingleOrDefaultAsync(
            item => item.Id == questionId && item.Type == QuestionType.Formula,
            cancellationToken);
        if (question is null) return FormulaDefinitionErrors.NotFound;

        var link = await context.QuestionFormulaDefinitions
            .Include(item => item.FormulaDefinition).ThenInclude(item => item.Revisions)
            .SingleOrDefaultAsync(item => item.QuestionId == questionId, cancellationToken);
        var definition = link?.FormulaDefinition ?? new FormulaDefinition
        {
            Id = Guid.NewGuid(),
            Key = $"question-{questionId:N}",
            Name = question.Label,
            PresentationPreset = request.PresentationPreset.Trim(),
            CreatedById = actorId
        };
        if (definition.Revisions.Any(item => item.Status == FormulaRevisionStatus.Draft))
            return FormulaDefinitionErrors.Conflict;
        if (link is null)
        {
            context.FormulaDefinitions.Add(definition);
            context.QuestionFormulaDefinitions.Add(new QuestionFormulaDefinition
            {
                Id = Guid.NewGuid(), QuestionId = questionId,
                FormulaDefinitionId = definition.Id, CreatedById = actorId
            });
        }

        var revision = FormulaDefinitionDraft.Build(
            definition, normalized, request, actorId);
        context.FormulaRevisions.Add(revision);
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, null, FormulaRevisionStatus.Draft, "DraftCreated",
            "Formula draft created.", actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return FormulaDefinitionMapping.ToDto(
            revision, questionId, definition.PresentationPreset);
    }

    public async Task<Result<FormulaRevisionDto>> UpdateDraftAsync(
        Guid revisionId, FormulaRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var normalized = FormulaDefinitionDraft.Normalize(request);
        if (normalized is null) return FormulaDefinitionErrors.InvalidDraft;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return FormulaDefinitionErrors.NotFound;
        if (revision.Status != FormulaRevisionStatus.Draft)
            return revision.Status == FormulaRevisionStatus.InReview
                ? FormulaDefinitionErrors.RevisionInReview
                : FormulaDefinitionErrors.RevisionNotEditable;
        FormulaDefinitionDraft.Apply(revision, normalized, request, actorId);
        revision.FormulaDefinition.PresentationPreset = request.PresentationPreset.Trim();
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, FormulaRevisionStatus.Draft, FormulaRevisionStatus.Draft,
            "DraftUpdated", "Formula draft updated.", actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(revision, cancellationToken);
    }

    public async Task<Result<FormulaDefinitionValidationDto>> ValidateAsync(
        Guid revisionId, Guid actorId, Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (revision is null) return FormulaDefinitionErrors.NotFound;
        var serviceRequest = FormulaDefinitionDraft.ValidationRequest(revision);
        var result = await calculationClient.ValidateAsync(serviceRequest, cancellationToken);
        if (result.IsFailure)
            return Result.Failure<FormulaDefinitionValidationDto>(result.Errors);
        var passed = result.Value.TestOutcomes.Count(item => item.Passed);
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, revision.Status, revision.Status, "ValidationExecuted",
            $"Status={result.Value.StatusName}; Tests={passed}/{result.Value.TestOutcomes.Count}; " +
            $"Evaluator={result.Value.EvaluatorVersion}; Engine={result.Value.EngineBuildHash}",
            actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(new FormulaDefinitionValidationDto(
            await ToDtoAsync(revision, cancellationToken), result.Value));
    }

    public async Task<Result<FormulaRevisionDto>> SubmitForReviewAsync(
        Guid revisionId, FormulaRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (!FormulaDefinitionDraft.CanTransition(
                revision, FormulaRevisionStatus.Draft, request))
            return revision is null ? FormulaDefinitionErrors.NotFound : FormulaDefinitionErrors.Conflict;
        var validation = await calculationClient.ValidateAsync(
            FormulaDefinitionDraft.ValidationRequest(revision!), cancellationToken);
        if (validation.IsFailure)
            return Result.Failure<FormulaRevisionDto>(validation.Errors);
        if (!FormulaDefinitionDraft.IsValid(validation.Value, revision!))
            return FormulaDefinitionErrors.ValidationFailed;
        revision!.Status = FormulaRevisionStatus.InReview;
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, FormulaRevisionStatus.Draft, revision.Status, "SubmittedForReview",
            request.Reason.Trim(), actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(revision, cancellationToken);
    }

    public async Task<Result<FormulaRevisionDto>> RecordReviewAsync(
        Guid revisionId, FormulaRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (!FormulaDefinitionDraft.CanTransition(
                revision, FormulaRevisionStatus.InReview, request))
            return revision is null ? FormulaDefinitionErrors.NotFound : FormulaDefinitionErrors.Conflict;
        if (revision!.CreatedById == actorId) return FormulaDefinitionErrors.SegregationOfDuties;
        if (revision.ReviewedById.HasValue) return FormulaDefinitionErrors.Conflict;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, revision.Status, revision.Status, "ReviewCompleted",
            request.Reason.Trim(), actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return await ToDtoAsync(revision, cancellationToken);
    }

    public async Task<Result<FormulaRevisionDto>> ApproveAsync(
        Guid revisionId, FormulaRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken)
            : null;
        var revision = await LoadRevisionAsync(revisionId, cancellationToken);
        if (!FormulaDefinitionDraft.CanTransition(
                revision, FormulaRevisionStatus.InReview, request))
            return revision is null ? FormulaDefinitionErrors.NotFound : FormulaDefinitionErrors.Conflict;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            revision.ReviewedById == actorId)
            return FormulaDefinitionErrors.SegregationOfDuties;
        var now = DateTime.UtcNow;
        var active = await context.FormulaRevisions.SingleOrDefaultAsync(item =>
            item.FormulaDefinitionId == revision.FormulaDefinitionId &&
            item.Status == FormulaRevisionStatus.Approved && item.Id != revision.Id,
            cancellationToken);
        if (active is not null)
        {
            active.Status = FormulaRevisionStatus.Retired;
            active.RetiredAt = now;
            context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
                active, FormulaRevisionStatus.Approved, active.Status, "Superseded",
                request.Reason.Trim(), actorId, correlationId));
        }
        revision.Status = FormulaRevisionStatus.Approved;
        revision.ApprovedById = actorId;
        revision.ApprovedAt = now;
        revision.EffectiveAt = now;
        context.FormulaRevisionAudits.Add(FormulaDefinitionMapping.Audit(
            revision, FormulaRevisionStatus.InReview, revision.Status, "Approved",
            request.Reason.Trim(), actorId, correlationId));
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (
            error.InnerException is Npgsql.PostgresException postgres &&
            postgres.SqlState is Npgsql.PostgresErrorCodes.UniqueViolation or
                Npgsql.PostgresErrorCodes.SerializationFailure)
        {
            return FormulaDefinitionErrors.Conflict;
        }
        return await ToDtoAsync(revision, cancellationToken);
    }

    private async Task<FormulaRevision?> LoadRevisionAsync(
        Guid revisionId, CancellationToken cancellationToken) =>
        await context.FormulaRevisions.Include(item => item.FormulaDefinition)
            .ThenInclude(item => item.Revisions)
            .SingleOrDefaultAsync(item => item.Id == revisionId, cancellationToken);

    private async Task<FormulaRevisionDto> ToDtoAsync(
        FormulaRevision revision,
        CancellationToken cancellationToken)
    {
        var questionId = await context.QuestionFormulaDefinitions.AsNoTracking()
            .Where(item => item.FormulaDefinitionId == revision.FormulaDefinitionId)
            .Select(item => item.QuestionId).SingleAsync(cancellationToken);
        return FormulaDefinitionMapping.ToDto(
            revision, questionId, revision.FormulaDefinition.PresentationPreset);
    }

}
