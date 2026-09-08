using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using DOMAIN.Entities.Forms;
using SHARED;

namespace APP.Services.Formulas;

internal static class FormRevisionSelection
{
    public static async Task<Result<Guid?>> ForNewResponseAsync(
        ApplicationDbContext context, Guid formId,
        CancellationToken cancellationToken = default)
    {
        var revisionId = await EffectiveIdAsync(context, formId, cancellationToken);
        if (!revisionId.HasValue && await context.FormFields.AsNoTracking().AnyAsync(field =>
                field.FormSection.FormId == formId &&
                field.Question.Type == QuestionType.Formula, cancellationToken))
            return FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        return Result.Success(revisionId);
    }

    public static async Task<IReadOnlyList<Guid>> MissingRequiredFieldsAsync(
        ApplicationDbContext context, Response response,
        CancellationToken cancellationToken = default)
    {
        var requiredIds = response.FormRevisionId.HasValue
            ? await context.FormFieldRevisions.AsNoTracking()
                .Where(field => field.FormRevisionId == response.FormRevisionId && field.Required)
                .Select(field => field.FormFieldId).ToListAsync(cancellationToken)
            : await context.FormFields.AsNoTracking()
                .Where(field => field.FormSection.FormId == response.FormId && field.Required)
                .Select(field => field.Id).ToListAsync(cancellationToken);
        var answeredIds = response.FormResponses.Select(item => item.FormFieldId).ToHashSet();
        return requiredIds.Where(id => !answeredIds.Contains(id)).ToList();
    }

    public static Task<Guid?> EffectiveIdAsync(
        ApplicationDbContext context,
        Guid formId,
        CancellationToken cancellationToken = default) =>
        context.FormRevisions.AsNoTracking()
            .Where(item => item.FormId == formId &&
                item.Status == DOMAIN.Entities.Formulas.FormRevisionStatus.Approved)
            .OrderByDescending(item => item.Sequence)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<Guid?> FieldIdAsync(
        ApplicationDbContext context,
        Guid? formRevisionId,
        Guid formFieldId,
        CancellationToken cancellationToken = default) =>
        formRevisionId.HasValue
            ? context.FormFieldRevisions.AsNoTracking()
                .Where(item => item.FormRevisionId == formRevisionId &&
                    item.FormFieldId == formFieldId)
                .Select(item => (Guid?)item.Id)
                .SingleOrDefaultAsync(cancellationToken)
            : Task.FromResult<Guid?>(null);
}
