using System.Text.Json.Nodes;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Formulas;

public static class LegacyFormulaConfigurationReader
{
    public static async Task<List<LegacyFormulaConfigurationPin>> Latest(ApplicationDbContext db, IEnumerable<Guid> responseIds)
    {
        var ids = responseIds.Distinct().ToList();
        var rows = await db.LegacyFormulaConfigurationPins.AsNoTracking().Where(p => ids.Contains(p.ResponseId)).ToListAsync();
        return rows.GroupBy(p => (p.ResponseId, p.FormFieldId)).Select(g => g.MaxBy(p => p.Sequence)!).ToList();
    }

    public static void Apply(FormDto form, IEnumerable<LegacyFormulaConfigurationPin> pins)
    {
        var byField = pins.ToDictionary(p => p.FormFieldId);
        foreach (var field in form.Sections.SelectMany(s => s.Fields))
        {
            if (field.Question?.Type != QuestionType.Formula) continue;
            if (byField.TryGetValue(field.Id, out var pin)) SetDefinition(field, pin.Definition, pin.DefinitionHash);
            else field.FormulaConfigurationHash = LegacyFormulaRepairDefinition.Hash(field.Question.Options.FirstOrDefault()?.Name ?? "");
        }
    }

    private static void SetDefinition(FormFieldDto field, string definition, string hash)
    {
        field.Question = new QuestionDto { Id = field.Question.Id, Label = field.Question.Label, Type = field.Question.Type,
            Validation = field.Question.Validation, Description = field.Question.Description, Reference = field.Question.Reference,
            IsMultiSelect = field.Question.IsMultiSelect, Options = [new QuestionOptionDto { Name = definition }] };
        field.FormulaConfigurationHash = hash;
    }

    public static async Task Attach(ApplicationDbContext db, IReadOnlyCollection<FormResponse> sources, IReadOnlyCollection<FormResponseDto> destinations)
    {
        var pins = await Latest(db, sources.Select(a => a.ResponseId));
        var byField = pins.ToDictionary(p => (p.ResponseId, p.FormFieldId));
        var sourceById = sources.ToDictionary(a => a.Id);
        var pinIds = pins.Select(p => p.Id).ToList();
        var evaluations = await db.LegacyFormulaRepairEvaluations.AsNoTracking().Where(e => pinIds.Contains(e.PinId)).ToListAsync();
        foreach (var dto in destinations)
        {
            if (!sourceById.TryGetValue(dto.Id, out var answer) || !byField.TryGetValue((answer.ResponseId, answer.FormFieldId), out var pin)) continue;
            SetDefinition(dto.FormField, pin.Definition, pin.DefinitionHash);
            var inputHash = SafeInputHash(answer.Value);
            var execution = evaluations.FirstOrDefault(e => e.PinId == pin.Id && e.InputHash == inputHash);
            dto.FormulaRepairPending = pin.RequiresRecalculation && execution is null;
            dto.FormulaRepairEvaluated = pin.RequiresRecalculation && execution is not null;
            if (pin.RequiresRecalculation)
            {
                dto.FormulaDisplayResultsJson = execution?.ResultsJson;
                // Original answer bytes remain in storage and audit. Readers never reuse old calculated results.
                var value = JsonNode.Parse(answer.Value) as JsonObject;
                if (value is not null)
                {
                    foreach (var key in new[] { "results", "result", "averageResult", "stats" }) value.Remove(key);
                    if (LegacyFormulaRepairDefinition.Number(LegacyFormulaRepairDefinition.Payload(pin.Definition), "type", 0) == 3)
                        value["settings"] = pin.Definition;
                    dto.Value = value.ToJsonString();
                }
            }
        }
    }

    public static string? SafeInputHash(string? value)
    {
        try { return value is null ? null : LegacyFormulaRepairDefinition.InputHash(value); }
        catch (Exception error) when (error is System.Text.Json.JsonException or ArgumentException) { return null; }
    }

    public static async Task<bool> Pending(ApplicationDbContext db, Response response)
    {
        var pins = (await Latest(db, [response.Id])).Where(p => p.RequiresRecalculation).ToList();
        var ids = pins.Select(p => p.Id).ToList();
        var evaluations = await db.LegacyFormulaRepairEvaluations.AsNoTracking().Where(e => ids.Contains(e.PinId)).ToListAsync();
        var answers = await db.FormResponses.AsNoTracking().Where(a => a.ResponseId == response.Id).ToListAsync();
        return pins.Any(pin => !evaluations.Any(e => e.PinId == pin.Id && e.InputHash == SafeInputHash(answers
            .Where(a => a.FormFieldId == pin.FormFieldId).OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.Value)));
    }

    /// <summary>
    /// Transaction-scoped lock per formula question. Draft saves take it shared so their configuration check and write
    /// cannot interleave with a repair, which takes it exclusively. Both take it before any table lock.
    /// </summary>
    public static Task LockQuestion(ApplicationDbContext db, Guid questionId, bool shared)
    {
        var key = "formula-question:" + questionId;
        return shared
            ? db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock_shared(hashtext({key}))")
            : db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({key}))");
    }

    public static async Task<Error?> ValidateWrite(ApplicationDbContext db, Response response, FormField field, string? observedHash)
    {
        if (field.Question.Type != QuestionType.Formula) return null;
        var pin = (await Latest(db, [response.Id])).SingleOrDefault(p => p.FormFieldId == field.Id);
        var repaired = pin is not null || await db.QuestionFormulaRepairs.AnyAsync(r => r.QuestionId == field.QuestionId);
        if (!repaired) return null;
        var expected = pin?.DefinitionHash ?? LegacyFormulaRepairDefinition.Hash(await db.QuestionOptions
            .Where(o => o.QuestionId == field.QuestionId).Select(o => o.Name).SingleAsync());
        return expected == observedHash ? null : Error.Conflict("FormulaRepair.StaleConfiguration", "This formula changed. Reload the worksheet before saving its readings.");
    }
}
