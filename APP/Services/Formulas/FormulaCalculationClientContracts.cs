using System.Text.Json;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaServiceRequestIssue(string Path, string Message);

public sealed record FormulaServiceIssue(
    int Code,
    int Severity,
    string Path,
    string Message,
    int? Position,
    string? VariableKey);

public sealed record FormulaServiceDependency(string ResultKey, IReadOnlyList<string> VariableKeys);

public sealed record FormulaServiceTestOutcome(
    string Id,
    string Name,
    int Category,
    bool Passed,
    int ExpectedStatus,
    int ActualStatus,
    JsonElement ExpectedResults,
    JsonElement ActualResults,
    IReadOnlyList<FormulaServiceEvaluationError> Errors);

public sealed record FormulaServiceEvaluationError(
    int Status,
    string Path,
    string Message,
    string? VariableKey);

public sealed record FormulaServiceResponse(
    string SchemaVersion,
    int Operation,
    string? RequestId,
    int Status,
    string StatusName,
    string EvaluatorVersion,
    string EngineBuildHash,
    string? ClaimedDefinitionHash,
    string? ComputedDefinitionHash,
    string? ClaimedConfigurationHash,
    string? ComputedConfigurationHash,
    string? ComputedInputHash,
    string? PlacementKey,
    string? CanonicalDefinition,
    IReadOnlyList<FormulaServiceDependency> Dependencies,
    IReadOnlyList<FormulaServiceIssue> DefinitionIssues,
    IReadOnlyList<FormulaServiceIssue> ConfigurationIssues,
    IReadOnlyList<FormulaServiceRequestIssue> RequestIssues,
    JsonElement? Evaluation,
    IReadOnlyList<FormulaServiceTestOutcome> TestOutcomes);

public interface IFormulaCalculationClient
{
    Task<Result<FormulaServiceResponse>> ValidateAsync(
        JsonElement request,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaServiceResponse>> EvaluateAsync(
        JsonElement request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsReadyAsync(CancellationToken cancellationToken = default);
}

public static class FormulaCalculationErrors
{
    public static readonly Error Disabled = Error.Conflict(
        "FormulaCalculation.Disabled",
        "The authoritative formula calculation service is disabled.");

    public static readonly Error Unavailable = Error.Failure(
        "FormulaCalculation.Unavailable",
        "The authoritative formula calculation service is unavailable.");

    public static readonly Error InvalidResponse = Error.Failure(
        "FormulaCalculation.InvalidResponse",
        "The formula calculation service returned an invalid response.");
}
