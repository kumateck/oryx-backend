using DOMAIN.Entities.Forms;
using System.Data;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed partial class FormRevisionService(
    ApplicationDbContext context,
    IFormulaCalculationClient calculationClient) : IFormRevisionService
{
    public async Task<Result<IReadOnlyList<FormRevisionDto>>> GetAsync(
        Guid formId, CancellationToken cancellationToken = default)
    {
        var revisions = await Query().AsNoTracking().Where(item => item.FormId == formId)
            .OrderByDescending(item => item.Sequence).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FormRevisionDto>>(
            revisions.Select(FormRevisionBuilder.ToDto).ToList());
    }

    public async Task<Result<FormRevisionDto>> CreateDraftAsync(
        Guid formId, FormRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (!ValidPlacements(request.FormulaPlacements)) return FormRevisionErrors.Invalid;
        var form = await context.Forms.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == formId, cancellationToken);
        if (form is null) return FormRevisionErrors.NotFound;
        if (await context.FormRevisions.AnyAsync(item => item.FormId == formId &&
                item.Status == FormRevisionStatus.Draft, cancellationToken))
            return FormRevisionErrors.Conflict;
        var fields = await context.FormFields.AsNoTracking()
            .Include(item => item.FormSection).Include(item => item.Question)
            .Where(item => item.FormSection.FormId == formId).ToListAsync(cancellationToken);
        var formulaFields = fields.Where(item => item.Question.Type == QuestionType.Formula)
            .Select(item => item.Id).ToHashSet();
        if (request.FormulaPlacements.Any(item => !formulaFields.Contains(item.FormFieldId)))
            return FormRevisionErrors.Invalid;
        var placements = await ResolvePlacementsAsync(
            request.FormulaPlacements, cancellationToken);
        if (placements is null) return FormRevisionErrors.Invalid;

        var sequence = await context.FormRevisions.Where(item => item.FormId == formId)
            .Select(item => (int?)item.Sequence).MaxAsync(cancellationToken) ?? 0;
        var revision = FormRevisionBuilder.Build(
            form, fields, placements, sequence + 1, actorId);
        context.FormRevisions.Add(revision);
        context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
            revision, null, "DraftCreated", "Template revision draft created.",
            actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return FormRevisionBuilder.ToDto(revision);
    }

    public async Task<Result<FormRevisionDto>> UpdateDraftAsync(
        Guid id, FormRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (!ValidPlacements(request.FormulaPlacements)) return FormRevisionErrors.Invalid;
        var revision = await Query().SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (revision is null) return FormRevisionErrors.NotFound;
        if (revision.Status != FormRevisionStatus.Draft) return FormRevisionErrors.Conflict;
        var fields = await context.FormFields.AsNoTracking()
            .Include(item => item.FormSection).Include(item => item.Question)
            .Where(item => item.FormSection.FormId == revision.FormId)
            .ToListAsync(cancellationToken);
        var formulaFields = fields.Where(item => item.Question.Type == QuestionType.Formula)
            .Select(item => item.Id).ToHashSet();
        if (request.FormulaPlacements.Any(item => !formulaFields.Contains(item.FormFieldId)))
            return FormRevisionErrors.Invalid;
        var placements = await ResolvePlacementsAsync(
            request.FormulaPlacements, cancellationToken);
        if (placements is null) return FormRevisionErrors.Invalid;

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var priorFields = revision.Fields.ToList();
        context.FormFieldFormulaConfigurations.RemoveRange(priorFields
            .Where(item => item.FormulaConfiguration is not null)
            .Select(item => item.FormulaConfiguration));
        context.FormFieldRevisions.RemoveRange(priorFields);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
        revision = await Query().SingleAsync(item => item.Id == id, cancellationToken);
        var refreshedFields = FormRevisionBuilder.RefreshDraft(
            revision, fields, placements, actorId);
        context.FormFieldRevisions.AddRange(refreshedFields);
        context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
            revision, FormRevisionStatus.Draft, "DraftUpdated",
            "Template revision draft configuration updated.", actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        context.ChangeTracker.Clear();
        var refreshed = await LoadAsync(id, cancellationToken);
        return FormRevisionBuilder.ToDto(refreshed!);
    }

    public async Task<Result<FormRevisionDto>> SubmitForReviewAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var revision = await LoadAsync(id, cancellationToken);
        if (!CanTransition(revision, FormRevisionStatus.Draft, request))
            return revision is null ? FormRevisionErrors.NotFound : FormRevisionErrors.Conflict;
        var formulaQuestionIds = await context.Questions.AsNoTracking()
            .Where(item => item.Type == QuestionType.Formula &&
                revision!.Fields.Select(field => field.QuestionId).Contains(item.Id))
            .Select(item => item.Id).ToListAsync(cancellationToken);
        if (revision!.Fields.Any(item => formulaQuestionIds.Contains(item.QuestionId) &&
                item.FormulaConfiguration is null)) return FormRevisionErrors.Invalid;
        var placementRequests = revision!.Fields
            .Where(item => item.FormulaConfiguration is not null)
            .Select(ToDraftRequest).ToList();
        if (!FormulaPlacementGraph.IsAcyclic(placementRequests))
            return FormRevisionErrors.Invalid;
        foreach (var field in revision.Fields.Where(item => item.FormulaConfiguration is not null))
            if (!await VerifyConfigurationAsync(field, cancellationToken))
                return FormRevisionErrors.Invalid;
        revision.Status = FormRevisionStatus.InReview;
        context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
            revision, FormRevisionStatus.Draft, "SubmittedForReview", request.Reason.Trim(),
            actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return FormRevisionBuilder.ToDto(revision);
    }

    public async Task<Result<FormRevisionDto>> RecordReviewAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        var revision = await LoadAsync(id, cancellationToken);
        if (!CanTransition(revision, FormRevisionStatus.InReview, request))
            return revision is null ? FormRevisionErrors.NotFound : FormRevisionErrors.Conflict;
        if (revision!.CreatedById == actorId) return FormRevisionErrors.SegregationOfDuties;
        if (revision.ReviewedById.HasValue) return FormRevisionErrors.Conflict;
        revision.ReviewedById = actorId;
        revision.ReviewedAt = DateTime.UtcNow;
        context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
            revision, revision.Status, "ReviewCompleted", request.Reason.Trim(),
            actorId, correlationId));
        await context.SaveChangesAsync(cancellationToken);
        return FormRevisionBuilder.ToDto(revision);
    }

    public async Task<Result<FormRevisionDto>> ApproveAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken)
            : null;
        var revision = await LoadAsync(id, cancellationToken);
        if (!CanTransition(revision, FormRevisionStatus.InReview, request))
            return revision is null ? FormRevisionErrors.NotFound : FormRevisionErrors.Conflict;
        if (!revision!.ReviewedById.HasValue || revision.CreatedById == actorId ||
            revision.ReviewedById == actorId) return FormRevisionErrors.SegregationOfDuties;
        var now = DateTime.UtcNow;
        var active = await context.FormRevisions.SingleOrDefaultAsync(item =>
            item.FormId == revision.FormId && item.Status == FormRevisionStatus.Approved,
            cancellationToken);
        if (active is not null)
        {
            active.Status = FormRevisionStatus.Retired;
            active.RetiredAt = now;
            context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
                active, FormRevisionStatus.Approved, "Superseded", request.Reason.Trim(),
                actorId, correlationId));
        }
        revision.Status = FormRevisionStatus.Approved;
        revision.ApprovedById = actorId;
        revision.ApprovedAt = now;
        context.FormRevisionAudits.Add(FormRevisionBuilder.Audit(
            revision, FormRevisionStatus.InReview, "Approved", request.Reason.Trim(),
            actorId, correlationId));
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
            return FormRevisionErrors.Conflict;
        }
        return FormRevisionBuilder.ToDto(revision);
    }

}
