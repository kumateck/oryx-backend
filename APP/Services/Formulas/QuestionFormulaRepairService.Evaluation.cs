using System.Data;
using System.Text.Json;
using APP.Utils;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Formulas;

public sealed partial class QuestionFormulaRepairService
{
    /// <summary>
    /// Recalculates a repaired field from its saved readings on the server. Reads and the engine call run without locks;
    /// a short transaction then rechecks permission, record state, the pin and the readings before recording the result.
    /// </summary>
    public async Task<Result<Guid>> Evaluate(FormulaRepairTarget target, string expectedConfigurationHash)
    {
        var grants = await Grants();
        if (!grants.Contains(PermissionKeys.CanExecuteFormulaResponse)) return Denied();
        var field = await db.FormFields.AsNoTracking().SingleOrDefaultAsync(f => f.Id == target.FormFieldId);
        if (field is null) return Conflict("Formula field not found.");
        var impact = await Load(field.QuestionId); if (impact is null) return Conflict("Formula question unavailable.");
        var selected = await Resolve(impact, target, grants);
        if (selected is null) return Denied();
        var closed = await Closed(selected); if (closed is not null) return Conflict(closed);
        if (selected.Response is not { } response) return Conflict("No readings are saved for this test yet. Enter the readings, save, then recalculate.");
        var pin = impact.Pins.SingleOrDefault(p => p.ResponseId == response.Id && p.FormFieldId == field.Id);
        if (pin is null || !pin.RequiresRecalculation || pin.DefinitionHash != expectedConfigurationHash)
            return Error.Conflict("FormulaRepair.StaleConfiguration", "The repair configuration changed. Reload the test.");
        var answer = LatestAnswer(impact, response.Id, field.Id);
        // Recalculation works from whatever is saved now: every input the formula uses is read from the saved answer and
        // the result is recalculated, replacing any earlier one. It stops only when an input truly has no value, and names it.
        if (answer is null || string.IsNullOrWhiteSpace(answer.Value) || answer.Value.Trim() == "{}")
            return Conflict("No readings are saved for this field yet. Enter the readings, save, then recalculate.");
        var inputHash = LegacyFormulaRepairDefinition.InputHash(answer.Value);
        var existing = await db.LegacyFormulaRepairEvaluations.SingleOrDefaultAsync(e => e.PinId == pin.Id && e.InputHash == inputHash);
        if (existing is not null) return existing.Id;
        var calculated = await Calculate(field.QuestionId, pin, answer);
        if (calculated.IsFailure) return Result.Failure<Guid>(calculated.Errors);
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null;
        // Recheck everything the result depends on; any change means the analyst must reload and retry.
        grants = await Grants();
        if (!grants.Contains(PermissionKeys.CanExecuteFormulaResponse)) return Denied();
        var freshResponse = await db.Responses.AsNoTracking().SingleAsync(r => r.Id == response.Id);
        var freshAtr = await db.AnalyticalTestRequests.AsNoTracking().SingleAsync(a => a.Id == selected.Atr.Id);
        if (!await CanAccess(freshAtr, selected.Field, grants)) return Denied();
        closed = await Closed(selected with { Atr = freshAtr, Response = freshResponse });
        if (closed is not null) return Conflict(closed);
        var latestPin = (await LegacyFormulaConfigurationReader.Latest(db, [freshResponse.Id])).SingleOrDefault(p => p.FormFieldId == field.Id);
        var latestValue = await db.FormResponses.AsNoTracking().Where(a => a.ResponseId == freshResponse.Id && a.FormFieldId == field.Id && !a.DeletedAt.HasValue)
            .OrderByDescending(a => a.CreatedAt).Select(a => a.Value).FirstOrDefaultAsync();
        if (latestPin?.Id != pin.Id || LegacyFormulaConfigurationReader.SafeInputHash(latestValue) != inputHash)
            return Error.Conflict("FormulaRepair.StaleConfiguration", "The readings or formula changed during recalculation. Reload the test and recalculate again.");
        var duplicate = await db.LegacyFormulaRepairEvaluations.AsNoTracking().Where(e => e.PinId == pin.Id && e.InputHash == inputHash).Select(e => (Guid?)e.Id).SingleOrDefaultAsync();
        if (duplicate is { } done) return done;
        var execution = new LegacyFormulaRepairEvaluation { Id = Guid.NewGuid(), PinId = pin.Id, ActorId = identity.UserId!.Value,
            InputHash = inputHash, ResultsJson = calculated.Value.ResultsJson, EngineBuildHash = calculated.Value.EngineBuildHash,
            EvidenceJson = calculated.Value.EvidenceJson, EvaluatedAt = DateTime.UtcNow };
        db.LegacyFormulaRepairEvaluations.Add(execution);
        await db.SaveChangesAsync(); if (transaction is not null) await transaction.CommitAsync();
        return execution.Id;
    }

    private sealed record Calculation(string InputHash, string ResultsJson, string EngineBuildHash, string EvidenceJson);

    /// <summary>
    /// Calculates a repaired field's saved readings with the engine: with the approved revision when the pin's formula has one
    /// (named expressions, derived columns and averages included), otherwise with the repair's own compilation. Records nothing.
    /// </summary>
    private async Task<Result<Calculation>> Calculate(Guid questionId, LegacyFormulaConfigurationPin pin, FormResponse answer)
    {
        // A pin set from an approved revision is calculated with that revision's reviewed, compiled definition (named
        // expressions, derived columns and averages included); any other repair with this repair's own compilation.
        var approved = await ApprovedRevisionFor(questionId, pin.Definition);
        var inputHash = LegacyFormulaRepairDefinition.InputHash(answer.Value);
        (JsonElement Definition, string Hash) compiled;
        Dictionary<string, string?> inputs;
        if (approved is not null)
        {
            using (var parsed = JsonDocument.Parse(approved.DefinitionJson)) compiled = (parsed.RootElement.Clone(), approved.DefinitionHash);
            IReadOnlyDictionary<(int Row, int Column), string> cells;
            using (var saved = JsonDocument.Parse(answer.Value))
            {
                var derived = await FormulaWorksheetPreprocessor.ResolveAsync(saved.RootElement, approved.DefinitionJson, calculation, CancellationToken.None);
                if (derived.IsFailure) return Conflict("The derived table columns could not be calculated. Enter every table reading they use, save, then recalculate.");
                cells = derived.Value;
            }
            var keys = compiled.Definition.GetProperty("variables").EnumerateArray().Select(v => v.GetProperty("key").GetString() ?? "").ToList();
            inputs = LegacyFormulaRepairDefinition.ResolveInputs(pin.Definition, answer.Value, keys, cells);
        }
        else
        {
            compiled = LegacyFormulaRepairDefinition.Compile(pin.Definition);
            inputs = LegacyFormulaRepairDefinition.ResolveInputs(pin.Definition, answer.Value);
        }
        // A Percentage formula reports its result in percent. Its own compilation already multiplies by 100; an approved
        // revision from before that fix does not, so the factor is applied here and recorded with the result.
        var percentage = PercentageFormulaScale.Applies(pin.Definition);
        object? percentageScale = null;
        if (percentage && approved is not null && PercentageFormulaScale.Scale(compiled.Definition) is { } scaled)
        {
            var scaledHash = await PercentageFormulaScale.HashAsync(calculation, scaled);
            if (scaledHash.IsFailure) return Result.Failure<Calculation>(scaledHash.Errors);
            percentageScale = new { approvedRevisionId = approved.Id, approvedDefinitionHash = compiled.Hash, scaledDefinitionHash = scaledHash.Value };
            compiled = (scaled, scaledHash.Value);
        }
        var missing = inputs.Where(pair => string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => LegacyFormulaRepairDefinition.InputLabel(pin.Definition, pair.Key)).Distinct().ToList();
        if (missing.Count > 0) return Conflict($"Enter and save these readings, then recalculate: {string.Join("; ", missing)}.");
        var placement = $"repair-{pin.Id:N}";
        // The engine checks each source and target unit against the approved definition. Preserve that contract;
        // older compiled repairs with no unit property retain their unitless (null) placement.
        var configuration = JsonSerializer.SerializeToElement(new
        {
            placementKey = placement, definitionHash = compiled.Hash, methodReference = "legacy-formula-repair",
            displayPolicy = new { policyVersion = "oryx-formula-display-v1", showResult = true, showUnit = true, showTrace = false },
            bindings = compiled.Definition.GetProperty("variables").EnumerateArray().Select(v => new
            { variableKey = v.GetProperty("key").GetString(), source = new { sourceType = 0, reference = "local:" + v.GetProperty("key").GetString(), dataType = 0, valueShape = 0, scope = 0,
                unit = v.TryGetProperty("unit", out var unit) ? unit.GetString() : null } }).ToArray(),
            resultTargets = compiled.Definition.GetProperty("results").EnumerateArray().Select(r => new
            { resultKey = r.GetProperty("key").GetString(), targetKey = placement + ":" + r.GetProperty("key").GetString(), dataType = 0, valueShape = 0,
                unit = r.TryGetProperty("unit", out var unit) ? unit.GetString() : null }).ToArray()
        });
        var projection = System.Text.Json.Nodes.JsonNode.Parse(configuration.GetRawText())!.AsObject();
        projection["configurationVersion"] = "oryx-formula-configuration-v1";
        var targets = projection["resultTargets"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>()
            .OrderBy(t => t["resultKey"]!.ToString(), StringComparer.Ordinal).Select(t => t.DeepClone()).ToArray();
        projection["resultTargets"] = new System.Text.Json.Nodes.JsonArray(targets);
        var configHash = FormulaCanonicalJson.HashJson("oryx:formula-configuration:v1", projection.ToJsonString(), 262144);
        var evaluated = await calculation.EvaluateAsync(JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-evaluate-request-v1", requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = "oryx-formula-v1",
            numericPolicyVersion = compiled.Definition.TryGetProperty("numericPolicyVersion", out var policy) ? policy.GetString() : LegacyFormulaRepairDefinition.Policy,
            definitionHash = compiled.Hash, definition = compiled.Definition, configurationHash = configHash, configuration,
            resolvedInputs = inputs
        }));
        if (evaluated.IsFailure) return Result.Failure<Calculation>(evaluated.Errors);
        var result = evaluated.Value;
        if (result.Status != 5 || result.ComputedDefinitionHash != compiled.Hash || result.ComputedConfigurationHash != configHash ||
            result.PlacementKey != placement || result.ComputedInputHash != FormulaCanonicalJson.HashJson("oryx:formula-resolved-inputs:v1", JsonSerializer.Serialize(inputs), 262144) || result.Evaluation is not { } evaluation ||
            !evaluation.TryGetProperty("status", out var status) || status.GetInt32() != 0 ||
            !evaluation.TryGetProperty("results", out var values) || values.ValueKind != JsonValueKind.Array ||
            values.GetArrayLength() != compiled.Definition.GetProperty("results").GetArrayLength())
            return Conflict(EngineRefusal(result, pin.Definition, inputs));
        return new Calculation(inputHash, FormulaResponseRuntimeJson.ResultProjection(values, "displayedResult"), result.EngineBuildHash,
            JsonSerializer.Serialize(new { savedAnswer = answer.Value, compiled.Definition, definitionHash = compiled.Hash, configuration, configurationHash = configHash,
                resolvedInputs = inputs, engineVersion = result.EvaluatorVersion, evaluation, percentageScaled = percentage, percentageScale }));
    }

    /// <summary>Says why the engine gave no valid result, naming the reading it refused where it can.</summary>
    internal static string EngineRefusal(FormulaServiceResponse result, string definition, IReadOnlyDictionary<string, string?> inputs)
    {
        string Label(string? key) => string.IsNullOrWhiteSpace(key) ? "" : LegacyFormulaRepairDefinition.InputLabel(definition, key)
            + (inputs.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? $" = \"{value}\"" : "");
        if (result.Status != 5 || result.Evaluation is not { } evaluation || !evaluation.TryGetProperty("status", out var status))
        {
            var issues = result.RequestIssues.Select(i => i.Message)
                .Concat(result.DefinitionIssues.Concat(result.ConfigurationIssues).Select(i => i.VariableKey is null ? i.Message : $"{Label(i.VariableKey)}: {i.Message}"))
                .Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Take(3).ToList();
            return $"The calculation service refused the request ({result.StatusName})" + (issues.Count > 0 ? ": " + string.Join("; ", issues) : "") + ". Reload the test and try again; if it repeats, repair the field again.";
        }
        var errors = evaluation.TryGetProperty("errors", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(e =>
            {
                var key = e.TryGetProperty("variableKey", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;
                var message = e.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                return key is null ? message : $"{Label(key)}{(string.IsNullOrWhiteSpace(message) ? "" : $" ({message})")}";
            }).Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Take(3).ToList()
            : [];
        var detail = errors.Count > 0 ? ": " + string.Join("; ", errors) : "";
        return status.GetInt32() switch
        {
            0 => "The calculation service returned results that do not match this repair. Reload the test and recalculate again.",
            1 => $"A reading the formula needs has no value{detail}. Enter and save it, then recalculate.",
            2 => $"A saved reading is not a number the formula can use{detail}. Correct it, save, then recalculate.",
            3 => $"The formula divides by zero with these readings{detail}. Check the readings, save, then recalculate.",
            _ => $"The formula could not be calculated from these readings{detail}. Check them, save, then recalculate."
        };
    }
}
