using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaResultProjectionService
{
    public static async Task AttachAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<FormResponse> sources,
        IReadOnlyCollection<FormResponseDto> destinations,
        CancellationToken cancellationToken = default)
    {
        if (sources.Count == 0 || destinations.Count == 0) return;
        var responseIds = sources.Select(item => item.ResponseId).Distinct().ToList();
        var governed = await context.Responses.AsNoTracking()
            .Where(item => responseIds.Contains(item.Id) && item.FormRevisionId.HasValue)
            .Select(item => new { item.Id, RevisionId = item.FormRevisionId!.Value })
            .ToListAsync(cancellationToken);
        if (governed.Count == 0) return;

        var governedIds = governed.Select(item => item.Id).ToHashSet();
        foreach (var dto in destinations.Where(item => governedIds.Contains(item.ResponseId)))
            dto.FormulaGoverned = true;

        var revisionIds = governed.Select(item => item.RevisionId).Distinct().ToList();
        var placements = await context.FormFieldRevisions.AsNoTracking()
            .Where(item => revisionIds.Contains(item.FormRevisionId))
            .Select(item => new
            {
                item.FormRevisionId, item.FormFieldId, item.PlacementKey
            }).ToListAsync(cancellationToken);
        var placementByResponseField = governed.SelectMany(response => placements
                .Where(field => field.FormRevisionId == response.RevisionId)
                .Select(field => new
                {
                    response.Id, field.FormFieldId, field.PlacementKey
                }))
            .ToDictionary(item => (item.Id, item.FormFieldId), item => item.PlacementKey);

        var finalized = await FinalizedExecutionsAsync(
            context, governedIds, cancellationToken);
        var draft = await LatestAuthoritativeExecutionsAsync(
            context, governedIds, cancellationToken);
        var sourceById = sources.ToDictionary(item => item.Id);
        foreach (var dto in destinations)
        {
            if (!dto.FormulaGoverned || !sourceById.TryGetValue(dto.Id, out var source) ||
                !placementByResponseField.TryGetValue(
                    (source.ResponseId, source.FormFieldId), out var placementKey)) continue;
            var key = (source.ResponseId, placementKey);
            var execution = finalized.GetValueOrDefault(key) ?? draft.GetValueOrDefault(key);
            if (execution is null) continue;
            dto.FormulaExecutionId = execution.Execution.Id;
            dto.FormulaDisplayResultsJson = execution.Execution.DisplayResultsJson;
            dto.FormulaResultFinalized = execution.Finalized;
        }
    }

    private static async Task<Dictionary<(Guid, string), ProjectedExecution>>
        FinalizedExecutionsAsync(
            ApplicationDbContext context,
            IReadOnlySet<Guid> responseIds,
            CancellationToken cancellationToken)
    {
        var sets = await context.ResponseFormulaSubmissionSets.AsNoTracking()
            .Where(item => responseIds.Contains(item.ResponseId))
            .Include(item => item.Executions)
                .ThenInclude(item => item.FormulaExecution)
            .ToListAsync(cancellationToken);
        return sets.GroupBy(item => item.ResponseId)
            .Select(group => group.OrderByDescending(item => item.Sequence).First())
            .SelectMany(set => set.Executions.Select(link => new
            {
                set.ResponseId, link.PlacementKey, link.FormulaExecution
            }))
            .ToDictionary(item => (item.ResponseId, item.PlacementKey), item =>
                new ProjectedExecution(item.FormulaExecution, true));
    }

    private static async Task<Dictionary<(Guid, string), ProjectedExecution>>
        LatestAuthoritativeExecutionsAsync(
            ApplicationDbContext context,
            IReadOnlySet<Guid> responseIds,
            CancellationToken cancellationToken)
    {
        var rows = await context.FormulaExecutions.AsNoTracking()
            .Where(item => responseIds.Contains(item.ResponseFormulaSnapshot.ResponseId) &&
                item.Authority == FormulaExecutionAuthority.AuthoritativeServer &&
                item.Status == FormulaExecutionStatus.Valid)
            .Include(item => item.ResponseFormulaSnapshot)
            .ToListAsync(cancellationToken);
        return rows.GroupBy(item => new
            {
                item.ResponseFormulaSnapshot.ResponseId,
                item.ResponseFormulaSnapshot.PlacementKey
            })
            .Select(group => group.OrderByDescending(item =>
                    item.ResponseFormulaSnapshot.Sequence)
                .ThenByDescending(item => item.ExecutedAt)
                .ThenByDescending(item => item.Id).First())
            .ToDictionary(item =>
                    (item.ResponseFormulaSnapshot.ResponseId,
                        item.ResponseFormulaSnapshot.PlacementKey),
                item => new ProjectedExecution(item, false));
    }

    private sealed record ProjectedExecution(FormulaExecution Execution, bool Finalized);
}
