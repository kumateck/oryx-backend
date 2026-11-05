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
        if (await Resolve(impact, target, grants) is not { } found) return Denied();
        var (selected, atr) = found;
        var closed = await Closed(selected.Response, atr); if (closed is not null) return Conflict(closed);
        var pin = impact.Pins.SingleOrDefault(p => p.ResponseId == selected.Response.Id && p.FormFieldId == field.Id);
        if (pin is null || !pin.RequiresRecalculation || pin.DefinitionHash != expectedConfigurationHash)
            return Error.Conflict("FormulaRepair.StaleConfiguration", "The repair configuration changed. Reload the test.");
        var answer = LatestAnswer(impact, selected.Response.Id, field.Id);
        if (answer is null || !APP.Services.Forms.FormAnswerCompleteness.IsComplete(answer.Value, QuestionType.Formula, pin.Definition))
            return Conflict("Complete and save all required readings before recalculating.");
        var inputHash = LegacyFormulaRepairDefinition.InputHash(answer.Value);
        var existing = await db.LegacyFormulaRepairEvaluations.SingleOrDefaultAsync(e => e.PinId == pin.Id && e.InputHash == inputHash);
        if (existing is not null) return existing.Id;
        var compiled = LegacyFormulaRepairDefinition.Compile(pin.Definition);
        var placement = $"repair-{pin.Id:N}";
        var configuration = JsonSerializer.SerializeToElement(new
        {
            placementKey = placement, definitionHash = compiled.Hash, methodReference = "legacy-formula-repair",
            displayPolicy = new { policyVersion = "oryx-formula-display-v1", showResult = true, showUnit = true, showTrace = false },
            bindings = compiled.Definition.GetProperty("variables").EnumerateArray().Select(v => new
            { variableKey = v.GetProperty("key").GetString(), source = new { sourceType = 0, reference = "local:" + v.GetProperty("key").GetString(), dataType = 0, valueShape = 0, scope = 0, unit = (string?)null } }).ToArray(),
            resultTargets = compiled.Definition.GetProperty("results").EnumerateArray().Select(r => new
            { resultKey = r.GetProperty("key").GetString(), targetKey = placement + ":" + r.GetProperty("key").GetString(), dataType = 0, valueShape = 0, unit = (string?)null }).ToArray()
        });
        var projection = System.Text.Json.Nodes.JsonNode.Parse(configuration.GetRawText())!.AsObject();
        projection["configurationVersion"] = "oryx-formula-configuration-v1";
        var targets = projection["resultTargets"]!.AsArray().OfType<System.Text.Json.Nodes.JsonObject>()
            .OrderBy(t => t["resultKey"]!.ToString(), StringComparer.Ordinal).Select(t => t.DeepClone()).ToArray();
        projection["resultTargets"] = new System.Text.Json.Nodes.JsonArray(targets);
        var configHash = FormulaCanonicalJson.HashJson("oryx:formula-configuration:v1", projection.ToJsonString(), 262144);
        var inputs = LegacyFormulaRepairDefinition.ResolveInputs(pin.Definition, answer.Value);
        var evaluated = await calculation.EvaluateAsync(JsonSerializer.SerializeToElement(new
        {
            schemaVersion = "oryx-formula-service-evaluate-request-v1", requestId = Guid.NewGuid().ToString("N"),
            formulaLanguageVersion = "oryx-formula-v1", numericPolicyVersion = LegacyFormulaRepairDefinition.Policy,
            definitionHash = compiled.Hash, definition = compiled.Definition, configurationHash = configHash, configuration,
            resolvedInputs = inputs
        }));
        if (evaluated.IsFailure) return Result.Failure<Guid>(evaluated.Errors);
        var result = evaluated.Value;
        if (result.Status != 5 || result.ComputedDefinitionHash != compiled.Hash || result.ComputedConfigurationHash != configHash ||
            result.PlacementKey != placement || result.ComputedInputHash != FormulaCanonicalJson.HashJson("oryx:formula-resolved-inputs:v1", JsonSerializer.Serialize(inputs), 262144) || result.Evaluation is not { } evaluation ||
            !evaluation.TryGetProperty("status", out var status) || status.GetInt32() != 0 ||
            !evaluation.TryGetProperty("results", out var values) || values.ValueKind != JsonValueKind.Array ||
            values.GetArrayLength() != compiled.Definition.GetProperty("results").GetArrayLength())
            return Conflict("The server could not calculate valid results from these saved readings. Check missing inputs and zero denominators, then retry.");
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null;
        // Recheck everything the result depends on; any change means the analyst must reload and retry.
        grants = await Grants();
        if (!grants.Contains(PermissionKeys.CanExecuteFormulaResponse)) return Denied();
        var freshResponse = await db.Responses.AsNoTracking().SingleAsync(r => r.Id == selected.Response.Id);
        var freshAtr = await db.AnalyticalTestRequests.AsNoTracking().SingleAsync(a => a.Id == atr.Id);
        if (!await CanAccess(freshAtr, selected.Field, freshResponse, grants)) return Denied();
        closed = await Closed(freshResponse, freshAtr); if (closed is not null) return Conflict(closed);
        var latestPin = (await LegacyFormulaConfigurationReader.Latest(db, [freshResponse.Id])).SingleOrDefault(p => p.FormFieldId == field.Id);
        var latestValue = await db.FormResponses.AsNoTracking().Where(a => a.ResponseId == freshResponse.Id && a.FormFieldId == field.Id && !a.DeletedAt.HasValue)
            .OrderByDescending(a => a.CreatedAt).Select(a => a.Value).FirstOrDefaultAsync();
        if (latestPin?.Id != pin.Id || LegacyFormulaConfigurationReader.SafeInputHash(latestValue) != inputHash)
            return Error.Conflict("FormulaRepair.StaleConfiguration", "The readings or formula changed during recalculation. Reload the test and recalculate again.");
        var duplicate = await db.LegacyFormulaRepairEvaluations.AsNoTracking().Where(e => e.PinId == pin.Id && e.InputHash == inputHash).Select(e => (Guid?)e.Id).SingleOrDefaultAsync();
        if (duplicate is { } done) return done;
        var execution = new LegacyFormulaRepairEvaluation { Id = Guid.NewGuid(), PinId = pin.Id, ActorId = identity.UserId!.Value,
            InputHash = inputHash, ResultsJson = FormulaResponseRuntimeJson.ResultProjection(values, "displayedResult"),
            EngineBuildHash = result.EngineBuildHash, EvidenceJson = JsonSerializer.Serialize(new { savedAnswer = answer.Value, compiled.Definition, definitionHash = compiled.Hash, configuration, configurationHash = configHash, resolvedInputs = inputs, engineVersion = result.EvaluatorVersion, evaluation }), EvaluatedAt = DateTime.UtcNow };
        db.LegacyFormulaRepairEvaluations.Add(execution);
        await db.SaveChangesAsync(); if (transaction is not null) await transaction.CommitAsync();
        return execution.Id;
    }
}
