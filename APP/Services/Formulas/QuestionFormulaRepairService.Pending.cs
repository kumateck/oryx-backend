using DOMAIN.Entities.AnalyticalTestRequests;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace APP.Services.Formulas;

public sealed partial class QuestionFormulaRepairService
{
    /// <summary>
    /// Recalculates, from the saved readings, every repaired field on a test response that still needs it (or only
    /// <paramref name="fieldId"/>). Saving a draft and submitting call this, so the analyst never recalculates by hand.
    /// Each field goes through <see cref="Evaluate"/>, with its permission, assignment and record-state checks; a field already
    /// calculated for its current readings returns at once. Returns, per field, why it could not be calculated.
    /// </summary>
    public async Task<List<string>> RecalculatePending(Guid responseId, Guid? fieldId = null)
    {
        var response = await db.Responses.AsNoTracking().SingleOrDefaultAsync(r => r.Id == responseId);
        if (response?.BatchManufacturingRecordId is null) return [];
        var pins = (await LegacyFormulaConfigurationReader.Latest(db, [responseId]))
            .Where(p => p.RequiresRecalculation && (fieldId is null || p.FormFieldId == fieldId)).ToList();
        if (pins.Count == 0) return [];
        var atr = await db.AnalyticalTestRequests.AsNoTracking()
            .Where(a => a.BatchManufacturingRecordId == response.BatchManufacturingRecordId && a.ProductionActivityStepId == response.ProductionActivityStepId)
            .OrderByDescending(a => a.Status == AnalyticalTestStatus.Testing || a.Status == AnalyticalTestStatus.Assigned).ThenByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();
        if (atr is null) return [];
        var fieldIds = pins.Select(p => p.FormFieldId).ToList();
        var labels = await db.FormFields.AsNoTracking().Where(f => fieldIds.Contains(f.Id)).Select(f => new { f.Id, f.Question.Label }).ToDictionaryAsync(f => f.Id, f => f.Label);
        var problems = new List<string>();
        foreach (var pin in pins)
        {
            var result = await Evaluate(new(atr.Id, pin.FormFieldId), pin.DefinitionHash);
            if (result.IsFailure)
                problems.Add($"{labels.GetValueOrDefault(pin.FormFieldId) ?? "Formula"}: {string.Join(" ", result.Errors.Select(e => e.Description))}");
        }
        return problems;
    }
}
