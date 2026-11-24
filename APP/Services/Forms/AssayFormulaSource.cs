using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms;

/// <summary>Reuses the server formula engine; reporting never trusts a result posted by the browser.</summary>
internal sealed record AssayFormulaSource(string Authoring, string Hash, JsonElement Definition, string DefinitionHash,
    bool Legacy, bool Percentage, bool Average, List<AssayOutputDto> Outputs)
{
    internal static async Task<AssayFormulaSource> Load(ApplicationDbContext db, FormField field, string? authoring = null)
    {
        var raw = authoring ?? field.Question.Options.FirstOrDefault()?.Name ?? "";
        var config = LegacyFormulaRepairDefinition.Payload(raw);
        var approved = await db.QuestionFormulaDefinitions.AsNoTracking().Where(link => link.QuestionId == field.QuestionId)
            .SelectMany(link => db.FormulaRevisions.Where(revision => revision.FormulaDefinitionId == link.FormulaDefinitionId
                && revision.ApprovedAt != null && revision.AuthoringPayloadJson == raw && revision.DefinitionJson != null))
            .OrderByDescending(revision => revision.Revision).FirstOrDefaultAsync();
        var average = approved is null && config["averageAllResults"]?.ToString() == "true";
        JsonElement definition; string definitionHash;
        if (approved is not null)
        {
            using var document = JsonDocument.Parse(approved.DefinitionJson);
            definition = document.RootElement.Clone(); definitionHash = approved.DefinitionHash;
        }
        else
        {
            var check = (JsonObject)config.DeepClone(); check["averageAllResults"] = false;
            var invalid = LegacyFormulaRepairDefinition.Validate(check.ToJsonString(), check.ToJsonString());
            if (invalid is not null) throw new ArgumentException("Approve this formula's revision before configuring COA reporting: " + invalid);
            (definition, definitionHash) = LegacyFormulaRepairDefinition.Compile(raw);
        }
        var outputs = definition.GetProperty("results").EnumerateArray().Select(result =>
        {
            var unit = result.TryGetProperty("unit", out var storedUnit) ? storedUnit.GetString() : null;
            var normalized = unit?.Trim().ToLowerInvariant();
            var compatible = normalized is null or "" or "%" or "percent" or "% of labelled strength" or "% of label claim";
            var label = result.TryGetProperty("label", out var storedLabel) ? storedLabel.GetString()! : result.GetProperty("key").GetString()!;
            return new AssayOutputDto(result.GetProperty("key").GetString()!, label, unit!, compatible,
                compatible ? null! : "This output already has a different reporting unit. Only a percentage of labelled strength can be converted to % w/v.");
        }).ToList();
        if (average) outputs.Insert(0, new("average", "Average content"));
        if (outputs.Count is 0 or > 100) throw new ArgumentException("This formula has no supported reportable outputs.");
        return new(raw, LegacyFormulaRepairDefinition.Hash(raw), definition, definitionHash, approved is null,
            config["percentageResults"]?.ToString() == "true" || config["percent"]?.ToString() == "true", average, outputs);
    }

    internal async Task<Result<Dictionary<string, decimal>>> Evaluate(ApplicationDbContext db, IFormulaCalculationClient? client, string answer)
    {
        if (client is null) return FormulaCalculationErrors.Unavailable;
        try
        {
            IReadOnlyDictionary<(int Row, int Column), string>? cells = null;
            if (!Legacy)
            {
                using var saved = JsonDocument.Parse(answer);
                var derived = await FormulaWorksheetPreprocessor.ResolveAsync(saved.RootElement, Definition.GetRawText(), client, CancellationToken.None);
                if (derived.IsFailure) return derived.Errors;
                cells = derived.Value;
            }
            var keys = Definition.GetProperty("variables").EnumerateArray().Select(variable => variable.GetProperty("key").GetString()!).ToList();
            var inputs = LegacyFormulaRepairDefinition.ResolveInputs(Authoring, answer, keys, cells);
            if (inputs.Any(input => string.IsNullOrWhiteSpace(input.Value)))
                return Error.Conflict("AssayReporting.Inputs", "Save all assay readings before calculating the COA result.");
            var placement = "coa-assay";
            var configuration = JsonSerializer.SerializeToNode(new
            {
                placementKey = placement, definitionHash = DefinitionHash, methodReference = "product-assay-reporting",
                displayPolicy = new { policyVersion = "oryx-formula-display-v1", showResult = true, showUnit = true, showTrace = false },
                bindings = Definition.GetProperty("variables").EnumerateArray().Select(variable => new
                { variableKey = variable.GetProperty("key").GetString(), source = new { sourceType = 0, reference = "local:" + variable.GetProperty("key").GetString(), dataType = 0, valueShape = 0, scope = 0,
                    unit = variable.TryGetProperty("unit", out var unit) ? unit.GetString() : null } }).ToArray(),
                resultTargets = Definition.GetProperty("results").EnumerateArray().Select(result => new
                { resultKey = result.GetProperty("key").GetString(), targetKey = placement + ":" + result.GetProperty("key").GetString(), dataType = 0, valueShape = 0,
                    unit = result.TryGetProperty("unit", out var unit) ? unit.GetString() : null }).ToArray()
            })!.AsObject();
            var projection = (JsonObject)configuration.DeepClone(); projection["configurationVersion"] = "oryx-formula-configuration-v1";
            projection["resultTargets"] = new JsonArray(projection["resultTargets"]!.AsArray().OfType<JsonObject>()
                .OrderBy(target => target["resultKey"]!.ToString(), StringComparer.Ordinal).Select(target => target.DeepClone()).ToArray());
            var configurationHash = FormulaCanonicalJson.HashJson("oryx:formula-configuration:v1", projection.ToJsonString(), 262144);
            var evaluated = await client.EvaluateAsync(JsonSerializer.SerializeToElement(new
            {
                schemaVersion = "oryx-formula-service-evaluate-request-v1", requestId = Guid.NewGuid().ToString("N"),
                formulaLanguageVersion = "oryx-formula-v1", numericPolicyVersion = Definition.TryGetProperty("numericPolicyVersion", out var policy) ? policy.GetString() : LegacyFormulaRepairDefinition.Policy,
                definitionHash = DefinitionHash, definition = Definition, configurationHash, configuration, resolvedInputs = inputs
            }));
            if (evaluated.IsFailure) return evaluated.Errors;
            var value = evaluated.Value;
            if (value.Status != 5 || value.ComputedDefinitionHash != DefinitionHash || value.ComputedConfigurationHash != configurationHash
                || value.PlacementKey != placement || value.ComputedInputHash != FormulaCanonicalJson.HashJson("oryx:formula-resolved-inputs:v1", JsonSerializer.Serialize(inputs), 262144)
                || value.Evaluation is not { } evaluation || evaluation.GetProperty("status").GetInt32() != 0)
                return Error.Conflict("AssayReporting.Calculation", "The server could not verify the assay calculation. Recalculate the assay before generating its COA.");
            var results = ReadExact(evaluation.GetProperty("results"), "exactResult");
            if (results.Keys.Order().SequenceEqual(Definition.GetProperty("results").EnumerateArray().Select(result => result.GetProperty("key").GetString()!).Order()) == false)
                return Error.Conflict("AssayReporting.Outputs", "The calculated outputs do not match the selected assay.");
            if (Legacy && Percentage) results = results.ToDictionary(pair => pair.Key, pair => checked(pair.Value * 100m));
            if (Average) results.Add("average", results.Values.Average());
            return results;
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or OverflowException or FormatException or DivideByZeroException)
        { return Error.Conflict("AssayReporting.Calculation", "The assay has invalid or incomplete readings. Review and recalculate it before generating the COA."); }
    }

    internal static Dictionary<string, decimal> ReadExact(JsonElement results, string property) => results.EnumerateArray().ToDictionary(
        result => result.GetProperty("key").GetString()!, result => Exact(result.GetProperty(property).GetString()!));

    internal static decimal Exact(string text)
    {
        var parts = text.Split('/');
        if (parts.Length is < 1 or > 2) throw new FormatException("Invalid exact result.");
        var numerator = decimal.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture);
        return parts.Length == 1 ? numerator : numerator / decimal.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
