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
        if (!revisionId.HasValue)
        {
            var hasGovernedQuestion = await context.FormFields.AsNoTracking().AnyAsync(field =>
                field.FormSection.FormId == formId &&
                field.Question.Type == QuestionType.Formula &&
                context.QuestionFormulaDefinitions.Any(link =>
                    link.QuestionId == field.QuestionId), cancellationToken);
            var hasGovernedPlacement = await context.FormFieldFormulaConfigurations
                .AsNoTracking().AnyAsync(configuration =>
                    configuration.FormFieldRevision.FormRevision.FormId == formId,
                    cancellationToken);
            if (hasGovernedQuestion || hasGovernedPlacement)
                return FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        }
        return Result.Success(revisionId);
    }

    public static async Task<IReadOnlyList<Guid>> MissingRequiredFieldsAsync(
        ApplicationDbContext context, Response response,
        CancellationToken cancellationToken = default, bool requireAllFields = false)
    {
        var fields = response.FormRevisionId.HasValue
            ? await context.FormFieldRevisions.AsNoTracking()
                .Where(field => field.FormRevisionId == response.FormRevisionId)
                .Select(field => new { Id = field.FormFieldId, field.Required, field.Question.Type,
                    Definition = field.Question.Options.Select(option => option.Name).FirstOrDefault() })
                .ToListAsync(cancellationToken)
            : await context.FormFields.AsNoTracking()
                .Where(field => field.FormSection.FormId == response.FormId)
                .Select(field => new { field.Id, field.Required, field.Question.Type,
                    Definition = field.Question.Options.Select(option => option.Name).FirstOrDefault() })
                .ToListAsync(cancellationToken);
        var answers = response.FormResponses.GroupBy(item => item.FormFieldId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.CreatedAt).First());
        return fields.Where(field => requireAllFields || field.Required).Where(field =>
        {
            var hasDraft = answers.TryGetValue(field.Id, out var answer);
            if (!hasDraft) return requireAllFields || field.Required;
            return !APP.Services.Forms.FormAnswerCompleteness.IsComplete(answer?.Value, field.Type, field.Definition);
        }).Select(field => field.Id).ToList();
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
