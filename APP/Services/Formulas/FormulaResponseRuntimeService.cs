using System.Text.Json;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed partial class FormulaResponseRuntimeService(
    ApplicationDbContext context,
    IFormulaCalculationClient calculationClient) : IFormulaResponseRuntimeService
{
    public async Task<Result<IReadOnlyList<FormulaSnapshotDto>>> EnsureInitialSnapshotsAsync(
        Guid responseId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var response = await context.Responses
            .Include(item => item.FormRevision).ThenInclude(item => item.Fields)
                .ThenInclude(item => item.FormulaConfiguration)
                    .ThenInclude(item => item.FormulaRevision)
            .SingleOrDefaultAsync(item => item.Id == responseId, cancellationToken);
        if (response is null) return FormulaResponseRuntimeErrors.NotFound;
        if (response.CreatedById != actorId)
            return FormulaResponseRuntimeErrors.Unauthorized;
        // Retirement prevents new use; it does not revoke a response's pinned revision.
        if (response.FormRevision is null ||
            response.FormRevision.Status is not (FormRevisionStatus.Approved or FormRevisionStatus.Retired) ||
            !response.FormRevision.ApprovedAt.HasValue)
            return FormulaResponseRuntimeErrors.ConfigurationUnavailable;

        var configurations = response.FormRevision.Fields
            .Where(item => item.FormulaConfiguration is not null)
            .OrderBy(item => item.PlacementKey).ToList();
        if (configurations.Any(item =>
                item.FormulaConfiguration.FormulaRevision.Status is not
                    (FormulaRevisionStatus.Approved or FormulaRevisionStatus.Retired) ||
                !item.FormulaConfiguration.FormulaRevision.ApprovedAt.HasValue))
            return FormulaResponseRuntimeErrors.ConfigurationUnavailable;

        var existing = await context.ResponseFormulaSnapshots.AsNoTracking()
            .Where(item => item.ResponseId == responseId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            var latest = existing.GroupBy(item => item.PlacementKey)
                .Select(group => group.OrderByDescending(item => item.Sequence).First())
                .OrderBy(item => item.PlacementKey).ToList();
            var expectedKeys = configurations.Select(item => item.PlacementKey)
                .Order().ToList();
            var actualKeys = latest.Select(item => item.PlacementKey).Order().ToList();
            return expectedKeys.SequenceEqual(actualKeys)
                ? Result.Success<IReadOnlyList<FormulaSnapshotDto>>(
                    latest.Select(ToDto).ToList())
                : FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        }

        var snapshots = configurations.Select(field => CreateSnapshot(
            responseId, field.PlacementKey, field.FormulaConfiguration, actorId)).ToList();
        foreach (var snapshot in snapshots)
        {
            snapshot.Response = response;
            var valid = await VerifySnapshotAsync(snapshot, cancellationToken);
            if (!valid) return FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        }
        context.ResponseFormulaSnapshots.AddRange(snapshots);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<FormulaSnapshotDto>>(
            snapshots.Select(ToDto).ToList());
    }

    public async Task<Result<FormulaExecutionDto>> EvaluateStoredInputsAsync(
        Guid responseId, string placementKey, string idempotencyKey, Guid actorId,
        FormulaExecutionTrigger trigger = FormulaExecutionTrigger.DraftEdit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            !IdempotencyPattern().IsMatch(idempotencyKey))
            return FormulaResponseRuntimeErrors.InvalidIdempotencyKey;
        var snapshot = await context.ResponseFormulaSnapshots
            .Include(item => item.Response).Include(item => item.FormulaRevision)
            .Where(item => item.ResponseId == responseId && item.PlacementKey == placementKey)
            .OrderByDescending(item => item.Sequence).FirstOrDefaultAsync(cancellationToken);
        if (snapshot is null) return FormulaResponseRuntimeErrors.NotFound;
        if (!await IsAuthorizedAsync(snapshot, actorId, trigger, cancellationToken))
            return FormulaResponseRuntimeErrors.Unauthorized;
        var existing = await context.FormulaExecutions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ResponseFormulaSnapshotId == snapshot.Id &&
                item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null) return ToDto(existing, placementKey);

        var resolved = await FormulaResponseInputResolver.ResolveAsync(
            context, snapshot, calculationClient, cancellationToken);
        if (resolved.IsFailure) return Result.Failure<FormulaExecutionDto>(resolved.Errors);
        var configuration = ConfigurationFor(snapshot);
        var serviceResult = await calculationClient.EvaluateAsync(
            FormulaResponseRuntimeJson.EvaluationRequest(
                snapshot, configuration, resolved.Value), cancellationToken);
        if (serviceResult.IsFailure)
            return Result.Failure<FormulaExecutionDto>(serviceResult.Errors);
        var execution = FormulaExecutionFactory.Build(
            snapshot, resolved.Value, serviceResult.Value, idempotencyKey, actorId, trigger);
        if (execution is null) return FormulaCalculationErrors.InvalidResponse;
        execution.SupersedesExecutionId = await context.FormulaExecutions.AsNoTracking()
            .Where(item => item.ResponseFormulaSnapshotId == snapshot.Id)
            .OrderByDescending(item => item.ExecutedAt).ThenByDescending(item => item.Id)
            .Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);
        context.FormulaExecutions.Add(execution);
        await context.SaveChangesAsync(cancellationToken);
        return ToDto(execution, placementKey);
    }

    private async Task<bool> VerifySnapshotAsync(
        ResponseFormulaSnapshot snapshot, CancellationToken cancellationToken)
    {
        using var bindings = JsonDocument.Parse(snapshot.BindingsJson);
        var blank = bindings.RootElement.EnumerateArray().ToDictionary(
            item => item.GetProperty("variableKey").GetString()!, _ => (string?)null);
        var inputs = FormulaCanonicalJson.Canonicalize(JsonSerializer.Serialize(blank), 262_144);
        var result = await calculationClient.EvaluateAsync(
            FormulaResponseRuntimeJson.EvaluationRequest(
                snapshot, ConfigurationFor(snapshot), inputs), cancellationToken);
        return result.IsSuccess && result.Value.Status == 5 &&
            result.Value.ComputedDefinitionHash == snapshot.DefinitionHash &&
            result.Value.ComputedConfigurationHash == snapshot.ConfigurationHash &&
            result.Value.PlacementKey == snapshot.PlacementKey;
    }

    private async Task<bool> IsAuthorizedAsync(
        ResponseFormulaSnapshot snapshot, Guid actorId, FormulaExecutionTrigger trigger,
        CancellationToken cancellationToken)
    {
        if (trigger == FormulaExecutionTrigger.FinalSubmission)
            return snapshot.Response.CreatedById == actorId;
        var fieldId = await context.FormFieldRevisions.AsNoTracking()
            .Where(item => item.FormRevisionId == snapshot.Response.FormRevisionId &&
                item.PlacementKey == snapshot.PlacementKey)
            .Select(item => item.FormFieldId).SingleOrDefaultAsync(cancellationToken);
        var assignment = await context.FormFieldAssignees.AsNoTracking()
            .Where(item => item.FormFieldId == fieldId &&
                item.FormAssignee.MaterialBatchId == snapshot.Response.MaterialBatchId &&
                item.FormAssignee.BatchManufacturingRecordId ==
                    snapshot.Response.BatchManufacturingRecordId &&
                item.FormAssignee.ProductionActivityStepId ==
                    snapshot.Response.ProductionActivityStepId)
            .Select(item => item.AssigneeId).FirstOrDefaultAsync(cancellationToken);
        return assignment.HasValue
            ? assignment == actorId
            : snapshot.Response.CreatedById == actorId;
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9:._-]{15,119}$")]
    private static partial Regex IdempotencyPattern();

    private string ConfigurationFor(ResponseFormulaSnapshot snapshot) =>
        FormulaResponseRuntimeJson.Configuration(snapshot.PlacementKey,
            snapshot.FormulaRevision, new FormFieldFormulaConfiguration
            {
                BindingsJson = snapshot.BindingsJson,
                ResultTargetsJson = snapshot.ResultTargetsJson,
                DisplayPolicyJson = snapshot.DisplayPolicyJson,
                MethodReference = snapshot.MethodReference
            });

    private static ResponseFormulaSnapshot CreateSnapshot(
        Guid responseId, string placementKey,
        FormFieldFormulaConfiguration configuration, Guid actorId) => new()
    {
        Id = Guid.NewGuid(), ResponseId = responseId, PlacementKey = placementKey,
        Sequence = 1, FormulaRevisionId = configuration.FormulaRevisionId,
        FormulaRevision = configuration.FormulaRevision,
        DefinitionHash = configuration.FormulaRevision.DefinitionHash,
        ConfigurationHash = configuration.ConfigurationHash,
        ExecutableDefinitionJson = configuration.FormulaRevision.DefinitionJson,
        BindingsJson = configuration.BindingsJson,
        ResultTargetsJson = configuration.ResultTargetsJson,
        TableShapeJson = "{}",
        CalculationPolicyJson = JsonSerializer.Serialize(new
        {
            numericPolicyVersion = configuration.FormulaRevision.NumericPolicyVersion
        }),
        DisplayPolicyJson = configuration.DisplayPolicyJson,
        MethodReference = configuration.MethodReference,
        Reason = FormulaSnapshotReason.Initial,
        ApprovalReference = $"formula-revision:{configuration.FormulaRevisionId:N}",
        CapturedById = actorId, CapturedAt = DateTime.UtcNow
    };

    private static FormulaSnapshotDto ToDto(ResponseFormulaSnapshot item) => new(
        item.Id, item.ResponseId, item.PlacementKey, item.Sequence,
        item.FormulaRevisionId, item.DefinitionHash, item.ConfigurationHash, item.CapturedAt);

    internal static FormulaExecutionDto ToDto(FormulaExecution item, string placementKey) => new(
        item.Id, item.ResponseFormulaSnapshotId, placementKey, (int)item.Status,
        item.Status.ToString(), (int)item.Authority, item.Authority.ToString(),
        item.EngineVersion, item.EngineBuildHash, item.InputHash, item.ResultHash,
        item.RawResultsJson, item.RoundedResultsJson, item.DisplayResultsJson, item.ExecutedAt);

}
