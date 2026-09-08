using System.Text.Json;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaExecutionFactory
{
    public static FormulaExecution? Build(
        ResponseFormulaSnapshot snapshot,
        string inputsJson,
        FormulaServiceResponse service,
        string idempotencyKey,
        Guid actorId,
        FormulaExecutionTrigger trigger)
    {
        if (service.Status != 5 || service.Evaluation is null ||
            service.ComputedDefinitionHash != snapshot.DefinitionHash ||
            service.ComputedConfigurationHash != snapshot.ConfigurationHash ||
            service.PlacementKey != snapshot.PlacementKey ||
            service.ComputedInputHash?.Length != 64) return null;
        var evaluation = service.Evaluation.Value;
        if (!evaluation.TryGetProperty("status", out var status) ||
            !evaluation.TryGetProperty("results", out var results) ||
            !evaluation.TryGetProperty("trace", out var trace) ||
            results.ValueKind != JsonValueKind.Array || trace.ValueKind != JsonValueKind.Array)
            return null;
        var resultJson = FormulaCanonicalJson.Canonicalize(
            results.GetRawText(), 1_048_576);
        return new FormulaExecution
        {
            Id = Guid.NewGuid(),
            ResponseFormulaSnapshotId = snapshot.Id,
            Trigger = trigger,
            Authority = FormulaExecutionAuthority.AuthoritativeServer,
            Status = MapStatus(status.GetInt32()),
            EngineVersion = service.EvaluatorVersion,
            EngineBuildHash = service.EngineBuildHash,
            FormulaLanguageVersion = snapshot.FormulaRevision.FormulaLanguageVersion,
            NumericPolicyVersion = snapshot.FormulaRevision.NumericPolicyVersion,
            InputHash = service.ComputedInputHash,
            ResultHash = FormulaCanonicalJson.HashCanonical(
                "oryx:formula-results:v1", resultJson),
            ResolvedInputsJson = inputsJson,
            RawResultsJson = FormulaResponseRuntimeJson.ResultProjection(results, "exactResult"),
            RoundedResultsJson = FormulaResponseRuntimeJson.ResultProjection(
                results, "roundedResult"),
            DisplayResultsJson = FormulaResponseRuntimeJson.ResultProjection(
                results, "displayedResult"),
            CalculationTraceJson = trace.GetRawText(),
            IdempotencyKey = idempotencyKey,
            ActorId = actorId,
            ExecutedAt = DateTime.UtcNow
        };
    }

    private static FormulaExecutionStatus MapStatus(int status) => status switch
    {
        0 => FormulaExecutionStatus.Valid,
        1 => FormulaExecutionStatus.MissingInput,
        2 => FormulaExecutionStatus.InvalidInput,
        3 => FormulaExecutionStatus.DivideByZero,
        5 => FormulaExecutionStatus.ResourceLimit,
        6 => FormulaExecutionStatus.PolicyMismatch,
        _ => FormulaExecutionStatus.DomainError
    };
}
