using System.Text.Json;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaResponseRuntimeJson
{
    public static string Configuration(
        string placementKey,
        FormulaRevision revision,
        FormFieldFormulaConfiguration configuration)
    {
        using var bindings = JsonDocument.Parse(configuration.BindingsJson);
        using var targets = JsonDocument.Parse(configuration.ResultTargetsJson);
        using var display = JsonDocument.Parse(configuration.DisplayPolicyJson);
        return FormulaCanonicalJson.Canonicalize(JsonSerializer.Serialize(new
        {
            placementKey,
            definitionHash = revision.DefinitionHash,
            methodReference = NullIfWhiteSpace(configuration.MethodReference),
            displayPolicy = display.RootElement,
            bindings = bindings.RootElement,
            resultTargets = targets.RootElement
        }), 1_048_576);
    }

    public static JsonElement EvaluationRequest(
        ResponseFormulaSnapshot snapshot,
        string configurationJson,
        string resolvedInputsJson)
    {
        using var definition = JsonDocument.Parse(snapshot.ExecutableDefinitionJson);
        using var configuration = JsonDocument.Parse(configurationJson);
        using var inputs = JsonDocument.Parse(resolvedInputsJson);
        return JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-evaluate-request-v1",
            requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = snapshot.FormulaRevision.FormulaLanguageVersion,
            numericPolicyVersion = snapshot.FormulaRevision.NumericPolicyVersion,
            definitionHash = snapshot.DefinitionHash,
            definition = definition.RootElement,
            configurationHash = snapshot.ConfigurationHash,
            configuration = configuration.RootElement,
            resolvedInputs = inputs.RootElement
        });
    }

    public static string ResultProjection(JsonElement results, string property) =>
        JsonSerializer.Serialize(results.EnumerateArray().Select(item => new
        {
            key = item.GetProperty("key").GetString(),
            value = item.GetProperty(property).GetString(),
            unit = item.TryGetProperty("unit", out var unit) && unit.ValueKind != JsonValueKind.Null
                ? unit.GetString()
                : null
        }));

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
