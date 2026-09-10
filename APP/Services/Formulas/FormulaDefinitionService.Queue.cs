using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed partial class FormulaDefinitionService
{
    public Task<Result<IReadOnlyList<FormulaRevisionQueueDto>>> GetReviewQueueAsync(
        CancellationToken cancellationToken = default) =>
        GetQueueAsync(
            revision => revision.Status == FormulaRevisionStatus.InReview &&
                revision.ReviewedById == null,
            cancellationToken);

    public Task<Result<IReadOnlyList<FormulaRevisionQueueDto>>> GetApprovalQueueAsync(
        CancellationToken cancellationToken = default) =>
        GetQueueAsync(
            revision => revision.Status == FormulaRevisionStatus.InReview &&
                revision.ReviewedById != null,
            cancellationToken);

    private async Task<Result<IReadOnlyList<FormulaRevisionQueueDto>>> GetQueueAsync(
        System.Linq.Expressions.Expression<
            Func<FormulaRevision, bool>> predicate,
        CancellationToken cancellationToken)
    {
        var rows = await context.FormulaRevisions.AsNoTracking()
            .Where(predicate)
            .SelectMany(revision => context.QuestionFormulaDefinitions
                .Where(link => link.FormulaDefinitionId == revision.FormulaDefinitionId)
                .Select(link => new
                {
                    revision.Id,
                    link.QuestionId,
                    link.Question.Label,
                    link.Question.Reference,
                    revision.Revision,
                    revision.DefinitionHash,
                    revision.Status,
                    revision.CreatedById,
                    revision.ReviewedById,
                    revision.ReviewedAt,
                    revision.CreatedAt
                }))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var revisions = rows.Select(item => new FormulaRevisionQueueDto(
            item.Id,
            item.QuestionId,
            item.Label,
            item.Reference,
            item.Revision,
            item.DefinitionHash,
            (int)item.Status,
            item.Status.ToString(),
            item.CreatedById,
            item.ReviewedById,
            item.ReviewedAt,
            item.CreatedAt)).ToList();
        return Result.Success<IReadOnlyList<FormulaRevisionQueueDto>>(revisions);
    }
}
