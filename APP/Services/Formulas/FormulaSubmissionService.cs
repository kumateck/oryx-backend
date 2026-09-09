using System.Text.Json;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed class FormulaSubmissionService(
    ApplicationDbContext context,
    IFormulaResponseRuntimeService runtime,
    IFormulaCalculationClient calculationClient) : IFormulaSubmissionService
{
    public async Task<Result<FormulaSubmissionSetDto?>> FinalizeAsync(
        Guid responseId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var response = await context.Responses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == responseId, cancellationToken);
        if (response is null) return FormulaResponseRuntimeErrors.NotFound;
        if (!response.FormRevisionId.HasValue)
            return Result.Success<FormulaSubmissionSetDto?>(null);
        var snapshots = await runtime.EnsureInitialSnapshotsAsync(
            responseId, actorId, cancellationToken);
        if (snapshots.IsFailure)
            return Result.Failure<FormulaSubmissionSetDto?>(snapshots.Errors);
        var orderedSnapshots = await FormulaSnapshotExecutionOrder.OrderAsync(
            context, responseId, snapshots.Value, cancellationToken);
        if (orderedSnapshots is null)
            return FormulaResponseRuntimeErrors.ConfigurationUnavailable;
        var executions = new List<FormulaExecutionDto>();
        foreach (var snapshot in orderedSnapshots)
        {
            var inputFingerprint = await InputFingerprintAsync(
                responseId, snapshot.PlacementKey, cancellationToken);
            if (inputFingerprint is null)
                return FormulaResponseRuntimeErrors.SubmissionBlocked;
            var key = $"final:{snapshot.Id:N}:{inputFingerprint[..16]}";
            var evaluated = await runtime.EvaluateStoredInputsAsync(
                responseId, snapshot.PlacementKey, key, actorId,
                FormulaExecutionTrigger.FinalSubmission, cancellationToken);
            if (evaluated.IsFailure)
                return Result.Failure<FormulaSubmissionSetDto?>(evaluated.Errors);
            if (evaluated.Value.Status != (int)FormulaExecutionStatus.Valid ||
                evaluated.Value.Authority != (int)FormulaExecutionAuthority.AuthoritativeServer)
                return FormulaResponseRuntimeErrors.SubmissionBlocked;
            executions.Add(evaluated.Value);
        }
        foreach (var execution in executions)
        {
            var current = await InputFingerprintAsync(
                responseId, execution.PlacementKey, cancellationToken);
            if (!string.Equals(current, execution.InputHash, StringComparison.Ordinal))
                return FormulaResponseRuntimeErrors.SubmissionBlocked;
        }
        var aggregate = FormulaCanonicalJson.Canonicalize(JsonSerializer.Serialize(
            executions.Select(item => new
            {
                item.PlacementKey, executionId = item.Id,
                item.InputHash, item.ResultHash
            }).OrderBy(item => item.PlacementKey)), 1_048_576);
        var setHash = FormulaCanonicalJson.HashCanonical(
            "oryx:formula-submission-set:v1", aggregate);
        var existing = await context.ResponseFormulaSubmissionSets.AsNoTracking()
            .Include(item => item.Executions)
            .SingleOrDefaultAsync(item => item.ResponseId == responseId &&
                item.SetHash == setHash, cancellationToken);
        if (existing is not null) return ToDto(existing);

        var sequence = await context.ResponseFormulaSubmissionSets
            .Where(item => item.ResponseId == responseId)
            .Select(item => (int?)item.Sequence).MaxAsync(cancellationToken) ?? 0;
        var submission = new ResponseFormulaSubmissionSet
        {
            Id = Guid.NewGuid(), ResponseId = responseId, Sequence = sequence + 1,
            SetHash = setHash,
            InputAggregateHash = FormulaCanonicalJson.HashCanonical(
                "oryx:formula-submission-inputs:v1", aggregate),
            ConfigurationAggregateHash = FormulaCanonicalJson.HashJson(
                "oryx:formula-submission-configurations:v1",
                JsonSerializer.Serialize(snapshots.Value.Select(item => new
                {
                    item.PlacementKey, item.ConfigurationHash
                }).OrderBy(item => item.PlacementKey)), 1_048_576),
            SubmittedById = actorId, SubmittedAt = DateTime.UtcNow
        };
        submission.Executions.AddRange(executions.Select((item, index) =>
            new ResponseFormulaSubmissionExecution
            {
                Id = Guid.NewGuid(), ResponseFormulaSubmissionSetId = submission.Id,
                FormulaExecutionId = item.Id, PlacementKey = item.PlacementKey,
                Ordinal = index
            }));
        context.ResponseFormulaSubmissionSets.Add(submission);
        await context.SaveChangesAsync(cancellationToken);
        return ToDto(submission);
    }

    private async Task<string?> InputFingerprintAsync(
        Guid responseId, string placementKey, CancellationToken cancellationToken)
    {
        var snapshot = await context.ResponseFormulaSnapshots
            .Include(item => item.Response).Where(item => item.ResponseId == responseId &&
                item.PlacementKey == placementKey).OrderByDescending(item => item.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (snapshot is null) return null;
        var resolved = await FormulaResponseInputResolver.ResolveAsync(
            context, snapshot, calculationClient, cancellationToken);
        return resolved.IsSuccess
            ? FormulaCanonicalJson.HashCanonical(
                "oryx:formula-resolved-inputs:v1", resolved.Value)
            : null;
    }

    private static FormulaSubmissionSetDto ToDto(ResponseFormulaSubmissionSet item) => new(
        item.Id, item.ResponseId, item.Sequence, item.SetHash, item.SubmittedAt,
        item.Executions.OrderBy(execution => execution.Ordinal)
            .Select(execution => execution.FormulaExecutionId).ToList());
}
