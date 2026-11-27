using System.Data;
using System.Text.Json;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Formulas;

/// <summary>
/// Recalculates repaired Percentage formula results that were stored as fractions (1.03 for 102.85%) before the percentage
/// factor was applied. Nothing is overwritten: each answer gets a new pin and a new calculation under an audited repair, and
/// a result that had been made final stays final. The earlier pin, calculation and finalization remain in the history.
/// </summary>
public sealed partial class QuestionFormulaRepairService
{
    private sealed record PercentageCandidate(LegacyFormulaConfigurationPin Pin, Response Response, FormField Field, FormResponse Answer,
        LegacyFormulaRepairEvaluation Evaluation, bool Finalized, AnalyticalTestRequest? Atr, string? Product, string? Batch, string? Blocker);

    public async Task<Result<List<PercentageRecalculationItemDto>>> PercentagePreview()
    {
        if (!(await Grants()).Contains(PermissionKeys.CanRecalculatePercentageFormulaResults)) return Denied();
        return (await PercentageCandidates()).Select(item => new PercentageRecalculationItemDto(item.Response.Id, item.Field.Id,
            item.Field.QuestionId, item.Field.Question?.Label ?? "Formula", item.Product, item.Batch, item.Atr?.ArNumber,
            item.Atr?.Status.ToString(), item.Evaluation.ResultsJson, item.Finalized, item.Blocker)).ToList();
    }

    public async Task<Result<List<PercentageRecalculationResultDto>>> PercentageApply(ApplyPercentageRecalculationRequest request)
    {
        var reason = request.Reason?.Trim() ?? "";
        var targets = (request.Targets ?? []).Distinct().ToList();
        if (reason.Length is < 10 or > 2000 || targets.Count is 0 or > 500)
            return Conflict("Enter a reason of 10–2000 characters and choose between 1 and 500 results to recalculate.");
        if (!(await Grants()).Contains(PermissionKeys.CanRecalculatePercentageFormulaResults)) return Denied();

        var results = new List<PercentageRecalculationResultDto>();
        var candidates = await PercentageCandidates(targets);
        var prepared = new List<(PercentageCandidate Candidate, Calculation Calculation)>();
        foreach (var target in targets)
        {
            var candidate = candidates.SingleOrDefault(item => item.Response.Id == target.ResponseId && item.Field.Id == target.FormFieldId);
            if (candidate is null)
            { results.Add(new(target.ResponseId, target.FormFieldId, false, null, null, "This result no longer needs recalculating. Reload the list.")); continue; }
            if (candidate.Blocker is not null)
            { results.Add(new(target.ResponseId, target.FormFieldId, false, candidate.Evaluation.ResultsJson, null, candidate.Blocker)); continue; }
            // The engine call runs before any lock, as every repair calculation does.
            var calculated = await Calculate(candidate.Field.QuestionId, candidate.Pin, candidate.Answer);
            if (calculated.IsFailure)
            { results.Add(new(target.ResponseId, target.FormFieldId, false, candidate.Evaluation.ResultsJson, null, (calculated.Error is { } error && error != Error.None ? error : calculated.Errors.First()).Description)); continue; }
            if (!PercentageFormulaScale.Recorded(calculated.Value.EvidenceJson))
            { results.Add(new(target.ResponseId, target.FormFieldId, false, candidate.Evaluation.ResultsJson, null, "This formula is not reported in percent.")); continue; }
            prepared.Add((candidate, calculated.Value));
        }
        if (prepared.Count == 0) return results;

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        try
        {
            if (transaction is not null)
                foreach (var question in prepared.Select(item => item.Candidate.Field.QuestionId).Distinct().Order()) await Lock(question);
            // Permission, pin, readings and record state are rechecked inside the locked transaction, immediately before the write.
            if (!(await Grants()).Contains(PermissionKeys.CanRecalculatePercentageFormulaResults))
            { db.ChangeTracker.Clear(); return Denied(); }
            var fresh = await PercentageCandidates(prepared.Select(item => new PercentageRecalculationTarget(item.Candidate.Response.Id, item.Candidate.Field.Id)).ToList());
            var now = DateTime.UtcNow;
            var written = new List<(PercentageCandidate Candidate, Calculation Calculation)>();
            foreach (var (candidate, calculation) in prepared)
            {
                var current = fresh.SingleOrDefault(item => item.Response.Id == candidate.Response.Id && item.Field.Id == candidate.Field.Id);
                var problem = current is null || current.Pin.Id != candidate.Pin.Id || current.Evaluation.Id != candidate.Evaluation.Id
                    || current.Evaluation.InputHash != calculation.InputHash
                    ? "The readings or formula changed during recalculation. Reload the list and try again."
                    : current.Blocker;
                if (problem is not null)
                { results.Add(new(candidate.Response.Id, candidate.Field.Id, false, candidate.Evaluation.ResultsJson, null, problem)); continue; }
                written.Add((candidate, calculation));
            }
            foreach (var group in written.GroupBy(item => item.Candidate.Field.QuestionId))
            {
                var repairId = Guid.NewGuid();
                var definition = group.First().Candidate.Pin.Definition;
                var evidence = JsonSerializer.Serialize(new
                {
                    kind = "percentage-recalculation",
                    reason,
                    results = group.Select(item => new
                    {
                        responseId = item.Candidate.Response.Id, formFieldId = item.Candidate.Field.Id, atrId = item.Candidate.Atr?.Id,
                        previousPinId = item.Candidate.Pin.Id, previousEvaluationId = item.Candidate.Evaluation.Id,
                        previousResults = item.Candidate.Evaluation.ResultsJson, results = item.Calculation.ResultsJson,
                        inputHash = item.Calculation.InputHash, wasFinal = item.Candidate.Finalized,
                    }),
                });
                db.QuestionFormulaRepairs.Add(new QuestionFormulaRepair { Id = repairId, QuestionId = group.Key, ActorId = identity.UserId!.Value,
                    UpdatedQuestion = false, Reason = reason, PreviewToken = "percentage-recalculation",
                    RequestHash = LegacyFormulaRepairDefinition.Hash(JsonSerializer.Serialize(new { reason, targets = group.Select(item => new { item.Candidate.Response.Id, FieldId = item.Candidate.Field.Id }) })),
                    DefinitionBefore = definition, DefinitionAfter = definition, EvidenceJson = evidence, AppliedAt = now });
                foreach (var (candidate, calculation) in group)
                {
                    var pin = Pin(candidate.Response.Id, candidate.Field.Id, candidate.Pin.Sequence + 1, repairId, candidate.Pin.Definition, true, now);
                    db.LegacyFormulaConfigurationPins.Add(pin);
                    var evaluation = new LegacyFormulaRepairEvaluation { Id = Guid.NewGuid(), PinId = pin.Id, ActorId = identity.UserId!.Value,
                        InputHash = calculation.InputHash, ResultsJson = calculation.ResultsJson, EngineBuildHash = calculation.EngineBuildHash,
                        EvidenceJson = calculation.EvidenceJson, EvaluatedAt = now };
                    db.LegacyFormulaRepairEvaluations.Add(evaluation);
                    // A result the analyst's submission had made final stays final with its corrected value.
                    if (candidate.Finalized)
                        db.LegacyFormulaRepairFinalizations.Add(new LegacyFormulaRepairFinalization { Id = Guid.NewGuid(), EvaluationId = evaluation.Id,
                            ResponseId = candidate.Response.Id, ActorId = identity.UserId!.Value, FinalizedAt = now });
                    results.Add(new(candidate.Response.Id, candidate.Field.Id, true, candidate.Evaluation.ResultsJson, calculation.ResultsJson, null));
                }
            }
            await db.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
            return results;
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync();
            db.ChangeTracker.Clear();
            throw;
        }
    }

    /// <summary>
    /// Repaired Percentage results whose calculation for the saved readings was made without the percentage factor. A field not
    /// yet calculated is left out: its next calculation already applies the factor.
    /// </summary>
    private async Task<List<PercentageCandidate>> PercentageCandidates(IReadOnlyCollection<PercentageRecalculationTarget>? only = null)
    {
        var responseFilter = only?.Select(target => target.ResponseId).Distinct().ToList();
        var allPins = await db.LegacyFormulaConfigurationPins.AsNoTracking()
            .Where(pin => responseFilter == null || responseFilter.Contains(pin.ResponseId)).ToListAsync();
        var pins = allPins.GroupBy(pin => (pin.ResponseId, pin.FormFieldId)).Select(group => group.MaxBy(pin => pin.Sequence)!)
            .Where(pin => pin.RequiresRecalculation && PercentageFormulaScale.Applies(pin.Definition))
            .Where(pin => only is null || only.Any(target => target.ResponseId == pin.ResponseId && target.FormFieldId == pin.FormFieldId))
            .ToList();
        if (pins.Count == 0) return [];
        var responseIds = pins.Select(pin => pin.ResponseId).Distinct().ToList();
        var fieldIds = pins.Select(pin => pin.FormFieldId).Distinct().ToList();
        var pinIds = pins.Select(pin => pin.Id).ToList();
        var responses = await db.Responses.IgnoreQueryFilters().AsNoTracking().Where(response => responseIds.Contains(response.Id)).ToDictionaryAsync(response => response.Id);
        var fields = await db.FormFields.IgnoreQueryFilters().AsNoTracking().Include(field => field.Question)
            .Where(field => fieldIds.Contains(field.Id)).ToDictionaryAsync(field => field.Id);
        var answers = await db.FormResponses.IgnoreQueryFilters().AsNoTracking()
            .Where(answer => responseIds.Contains(answer.ResponseId) && fieldIds.Contains(answer.FormFieldId) && !answer.DeletedAt.HasValue).ToListAsync();
        var evaluations = await db.LegacyFormulaRepairEvaluations.AsNoTracking().Where(evaluation => pinIds.Contains(evaluation.PinId)).ToListAsync();
        var evaluationIds = evaluations.Select(evaluation => evaluation.Id).ToList();
        var finalized = (await db.LegacyFormulaRepairFinalizations.AsNoTracking().Where(item => evaluationIds.Contains(item.EvaluationId))
            .Select(item => item.EvaluationId).ToListAsync()).ToHashSet();
        var bmrIds = responses.Values.Select(response => response.BatchManufacturingRecordId).OfType<Guid>().Distinct().ToList();
        var atrs = await db.AnalyticalTestRequests.IgnoreQueryFilters().AsNoTracking().Where(atr => bmrIds.Contains(atr.BatchManufacturingRecordId)).ToListAsync();
        var batches = await db.BatchManufacturingRecords.IgnoreQueryFilters().AsNoTracking().Where(bmr => bmrIds.Contains(bmr.Id))
            .Select(bmr => new { bmr.Id, bmr.BatchNumber, Product = bmr.ProductionScheduleProduct.Product.Name }).ToDictionaryAsync(bmr => bmr.Id);
        var inApproval = (await db.ResponseApprovals.AsNoTracking().Where(approval => responseIds.Contains(approval.ResponseId))
            .Select(approval => approval.ResponseId).Distinct().ToListAsync()).ToHashSet();

        var candidates = new List<PercentageCandidate>();
        foreach (var pin in pins)
        {
            if (!responses.TryGetValue(pin.ResponseId, out var response) || !fields.TryGetValue(pin.FormFieldId, out var field)) continue;
            var answer = answers.Where(item => item.ResponseId == response.Id && item.FormFieldId == field.Id)
                .OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            if (answer is null) continue;
            var inputHash = LegacyFormulaConfigurationReader.SafeInputHash(answer.Value);
            var evaluation = evaluations.FirstOrDefault(item => item.PinId == pin.Id && item.InputHash == inputHash);
            if (evaluation is null || PercentageFormulaScale.Recorded(evaluation.EvidenceJson)) continue;
            var atr = atrs.Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId
                    && item.ProductionActivityStepId == response.ProductionActivityStepId)
                .OrderByDescending(item => item.CreatedAt).FirstOrDefault();
            var batch = response.BatchManufacturingRecordId is { } bmr && batches.TryGetValue(bmr, out var found) ? found : null;
            candidates.Add(new(pin, response, field, answer, evaluation, finalized.Contains(evaluation.Id), atr, batch?.Product, batch?.BatchNumber,
                PercentageBlocker(response, atr, inApproval.Contains(response.Id))));
        }
        return candidates.OrderBy(item => item.Batch).ThenBy(item => item.Field.Question?.Label).ToList();
    }

    /// <summary>A result can be corrected while its test is open or taken; once the certificate moves on, it cannot.</summary>
    internal static string? PercentageBlocker(Response response, AnalyticalTestRequest? atr, bool inApproval) =>
        response.DeletedAt.HasValue ? "The test response was deleted."
        : response.FormRevisionId.HasValue ? "This test uses a governed worksheet, which this recalculation does not change."
        : response.Approved || response.Rejected || response.CheckedAt.HasValue ? "The test's certificate has already been checked, approved or rejected."
        : inApproval ? "The test's results are in approval. Finish or withdraw that approval first."
        : atr is null ? "No analytical test request matches this test."
        : atr.ReleasedAt.HasValue || atr.Status is not (AnalyticalTestStatus.Assigned or AnalyticalTestStatus.Testing or AnalyticalTestStatus.TestTaken)
            ? $"The test is {atr.Status}; only assigned, testing or test-taken tests can be recalculated."
        : null;
}
