using System.Text.Json;
using System.Text.Json.Nodes;
using SHARED;

#nullable enable
namespace APP.Services.Formulas;

/// <summary>
/// "Percentage" on a formula means its expression gives a fraction that is reported in percent, as Oryx Next showed it.
/// Revisions approved before 2026-11-27 labelled those results "%" without multiplying by 100, so a 102.85% assay was
/// stored as 1.03. This applies the missing factor to such a definition at calculation time and records that it did.
/// Revisions built since then already carry <c>(expression) * 100</c> and are left as they are.
/// </summary>
internal static class PercentageFormulaScale
{
    private const string Suffix = " * 100";

    /// <summary>Whether a formula's saved settings report its results in percent. Uniformity of Weight is excluded:
    /// its deviations always include the factor, whatever the setting says.</summary>
    public static bool Applies(string authoring)
    {
        try
        {
            var config = LegacyFormulaRepairDefinition.Payload(authoring);
            return LegacyFormulaRepairDefinition.Percentage(config)
                && config["mode"]?.ToString() is not ("capsule" or "tablet" or "tube");
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException) { return false; }
    }

    /// <summary>The definition with every percentage-point result multiplied by 100, or null when none needs it.</summary>
    public static JsonElement? Scale(JsonElement definition)
    {
        if (JsonNode.Parse(definition.GetRawText()) is not JsonObject root || root["results"] is not JsonArray results) return null;
        var changed = false;
        foreach (var result in results.OfType<JsonObject>())
        {
            if (result["numericScale"] is not JsonValue scale || !scale.TryGetValue<int>(out var value) || value != 2) continue;
            if (result["expression"] is not JsonValue text || !text.TryGetValue<string>(out var expression)
                || string.IsNullOrWhiteSpace(expression) || IsScaled(expression)) continue;
            result["expression"] = $"({expression.Trim()}){Suffix}";
            changed = true;
        }
        return changed ? JsonSerializer.SerializeToElement(root) : null;
    }

    /// <summary>True for <c>(X) * 100</c> where the opening parenthesis closes just before the factor.</summary>
    internal static bool IsScaled(string expression)
    {
        var text = expression.Trim();
        if (!text.StartsWith('(') || !text.EndsWith(")" + Suffix, StringComparison.Ordinal)) return false;
        var inner = text[..^Suffix.Length];
        var depth = 0;
        for (var index = 0; index < inner.Length; index++)
        {
            depth += inner[index] switch { '(' => 1, ')' => -1, _ => 0 };
            if (depth == 0 && index < inner.Length - 1) return false;
        }
        return depth == 0;
    }

    /// <summary>Asks the calculation service for the hash of a definition, so the scaled definition is verified like any other.</summary>
    public static async Task<Result<string>> HashAsync(IFormulaCalculationClient client, JsonElement definition)
    {
        var request = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-validate-request-v1", requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = definition.TryGetProperty("formulaLanguageVersion", out var language) ? language.GetString() : "oryx-formula-v1",
            numericPolicyVersion = definition.TryGetProperty("numericPolicyVersion", out var policy) ? policy.GetString() : LegacyFormulaRepairDefinition.Policy,
            definitionHash = new string('0', 64), definition, testCases = Array.Empty<object>()
        });
        var validated = await client.ValidateAsync(request);
        if (validated.IsFailure) return Result.Failure<string>(validated.Errors);
        var response = validated.Value;
        return response.ComputedDefinitionHash is { Length: 64 } hash && response.DefinitionIssues.Count == 0
            ? Result.Success(hash)
            : Error.Conflict("FormulaRepair.PercentageScale", "The calculation service did not accept the formula with its percentage factor. Repair the formula and try again.");
    }

    /// <summary>Whether a recorded calculation already reports this formula in percent.</summary>
    public static bool Recorded(string evidenceJson)
    {
        try
        {
            using var evidence = JsonDocument.Parse(evidenceJson);
            return evidence.RootElement.TryGetProperty("percentageScaled", out var scaled) && scaled.ValueKind == JsonValueKind.True;
        }
        catch (JsonException) { return false; }
    }
}
