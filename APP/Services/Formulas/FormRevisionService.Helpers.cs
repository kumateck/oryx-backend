using System.Text.Json;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

public sealed partial class FormRevisionService
{
    private async Task<Dictionary<Guid, (FormulaPlacementDraftRequest Request,
        FormulaRevision Revision)>?> ResolvePlacementsAsync(
        IReadOnlyList<FormulaPlacementDraftRequest> requests,
        CancellationToken cancellationToken)
    {
        var revisionIds = requests.Select(item => item.FormulaRevisionId).Distinct().ToList();
        var revisions = await context.FormulaRevisions.Include(item => item.FormulaDefinition)
            .Where(item => revisionIds.Contains(item.Id) &&
                item.Status == FormulaRevisionStatus.Approved)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        if (revisions.Count != revisionIds.Count) return null;
        var definitionIds = revisions.Values.Select(item => item.FormulaDefinitionId)
            .Distinct().ToList();
        var links = await context.QuestionFormulaDefinitions.AsNoTracking()
            .Where(item => definitionIds.Contains(item.FormulaDefinitionId))
            .Select(item => new { item.QuestionId, item.FormulaDefinitionId })
            .ToListAsync(cancellationToken);
        var fieldQuestions = await context.FormFields.AsNoTracking()
            .Where(item => requests.Select(request => request.FormFieldId).Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.QuestionId, cancellationToken);
        var result = new Dictionary<Guid, (FormulaPlacementDraftRequest, FormulaRevision)>();
        foreach (var request in requests)
        {
            var revision = revisions[request.FormulaRevisionId];
            if (!fieldQuestions.TryGetValue(request.FormFieldId, out var questionId) ||
                !links.Any(item => item.QuestionId == questionId &&
                    item.FormulaDefinitionId == revision.FormulaDefinitionId)) return null;
            result[request.FormFieldId] = (request, revision);
        }
        return result;
    }

    private async Task<bool> VerifyConfigurationAsync(
        FormFieldRevision field, CancellationToken cancellationToken)
    {
        var configuration = field.FormulaConfiguration;
        var snapshot = new ResponseFormulaSnapshot
        {
            PlacementKey = field.PlacementKey, FormulaRevision = configuration.FormulaRevision,
            DefinitionHash = configuration.FormulaRevision.DefinitionHash,
            ConfigurationHash = configuration.ConfigurationHash,
            ExecutableDefinitionJson = configuration.FormulaRevision.DefinitionJson,
            BindingsJson = configuration.BindingsJson,
            ResultTargetsJson = configuration.ResultTargetsJson,
            DisplayPolicyJson = configuration.DisplayPolicyJson,
            MethodReference = configuration.MethodReference
        };
        using var bindings = JsonDocument.Parse(configuration.BindingsJson);
        var blank = bindings.RootElement.EnumerateArray().ToDictionary(
            item => item.GetProperty("variableKey").GetString()!, _ => (string?)null);
        var response = await calculationClient.EvaluateAsync(
            FormulaResponseRuntimeJson.EvaluationRequest(snapshot,
                FormulaResponseRuntimeJson.Configuration(
                    field.PlacementKey, configuration.FormulaRevision, configuration),
                JsonSerializer.Serialize(blank)), cancellationToken);
        return response.IsSuccess && response.Value.Status == 5 &&
            response.Value.ComputedDefinitionHash == snapshot.DefinitionHash &&
            response.Value.ComputedConfigurationHash == snapshot.ConfigurationHash;
    }

    private IQueryable<FormRevision> Query() => context.FormRevisions.AsSplitQuery()
        .Include(item => item.Form)
        .Include(item => item.Fields).ThenInclude(item => item.FormulaConfiguration)
            .ThenInclude(item => item.FormulaRevision);

    private Task<FormRevision?> LoadAsync(Guid id, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

    private static bool CanTransition(FormRevision? revision, FormRevisionStatus expected,
        FormRevisionTransitionRequest request) => revision is not null &&
        revision.Status == expected && revision.ContentHash == request.ExpectedContentHash &&
        request.Reason.Trim().Length is >= 10 and <= 4000;

    private static FormulaPlacementDraftRequest ToDraftRequest(FormFieldRevision field) => new(
        field.FormFieldId, field.PlacementKey, field.FormulaConfiguration.FormulaRevisionId,
        field.FormulaConfiguration.ConfigurationHash,
        ParseClone(field.FormulaConfiguration.BindingsJson),
        ParseClone(field.FormulaConfiguration.ResultTargetsJson),
        ParseClone(field.FormulaConfiguration.DisplayPolicyJson),
        field.FormulaConfiguration.MethodReference);

    private static JsonElement ParseClone(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static bool ValidPlacements(IReadOnlyList<FormulaPlacementDraftRequest>? items) =>
        items is not null && items.Count <= 500 &&
        items.Select(item => item.FormFieldId).Distinct().Count() == items.Count &&
        items.Select(item => item.PlacementKey).Distinct(StringComparer.Ordinal).Count() == items.Count &&
        items.All(item => PlacementPattern().IsMatch(item.PlacementKey ?? string.Empty) &&
            HashPattern().IsMatch(item.ConfigurationHash ?? string.Empty) &&
            item.Bindings.ValueKind == JsonValueKind.Array &&
            item.ResultTargets.ValueKind == JsonValueKind.Array &&
            item.DisplayPolicy.ValueKind == JsonValueKind.Object &&
            (item.MethodReference?.Length ?? 0) <= 250) && FormulaPlacementGraph.IsAcyclic(items);

    [GeneratedRegex("^[a-z][a-z0-9._-]{0,119}$")]
    private static partial Regex PlacementPattern();
    [GeneratedRegex("^[a-f0-9]{64}$")]
    private static partial Regex HashPattern();
}
