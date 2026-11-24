using System.Globalization;
using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

#nullable enable
namespace APP.Services.Forms;

internal static class ProductAssayReportingRuntime
{
    internal static AssayReportedResultDto Convert(AssayReportingRule rule, string label, decimal source)
    {
        var value = checked(source / 100m * rule.NominalConcentration);
        var displayed = decimal.Round(value, rule.DecimalPlaces, MidpointRounding.AwayFromZero);
        // A nonzero calculated result must never be printed as zero.
        var text = displayed == 0 && value != 0 ? value.ToString("G29", CultureInfo.InvariantCulture)
            : displayed.ToString("F" + rule.DecimalPlaces, CultureInfo.InvariantCulture);
        return new(rule.ResultKey, label, source, value, text, "% w/v", rule.Minimum, rule.Maximum,
            value >= rule.Minimum && value <= rule.Maximum, rule.NominalConcentration, rule.DefinitionHash, true, rule.DecimalPlaces);
    }

    internal static async Task<List<AssayReportingFieldSnapshot>> Build(ApplicationDbContext db, Response response, IFormulaCalculationClient? client)
    {
        if (response.AssayReportingSnapshotJson is not null)
            return JsonSerializer.Deserialize<AssayReportingSnapshot>(response.AssayReportingSnapshotJson)!.Fields;
        // Existing submitted/issued tests retain their original interpretation, including an explicit absence of conversion.
        if (response.CheckedAt.HasValue || response.Approved || response.BatchManufacturingRecordId is null) return [];
        var atr = await db.AnalyticalTestRequests.AsNoTracking().Where(item => item.BatchManufacturingRecordId == response.BatchManufacturingRecordId
            && item.ProductionActivityStepId == response.ProductionActivityStepId).OrderByDescending(item => item.CreatedAt)
            .Select(item => new { item.Stage, item.ProductionScheduleProduct.ProductId, item.ChemicalArdId }).FirstOrDefaultAsync();
        if (atr is null) return [];
        var chemicalForm = atr.ChemicalArdId.HasValue
            ? await db.ProductAnalyticalRawData.AsNoTracking().Where(ard => ard.Id == atr.ChemicalArdId).Select(ard => (Guid?)ard.FormId).SingleOrDefaultAsync() : null;
        var specifications = await db.ProductSpecifications.AsNoTracking().Where(item => item.ProductId == atr.ProductId && item.TestStage == atr.Stage
            && (item.FormId == response.FormId || chemicalForm == response.FormId) && item.AssayReportingRulesJson != null).ToListAsync();
        var configured = specifications.SelectMany(specification => ProductAssayReportingService.Rules(specification.AssayReportingRulesJson)
            .Select(rule => (Specification: specification, Rule: rule))).ToList();
        if (configured.Count == 0) return [];
        var fieldIds = configured.Select(item => item.Rule.FormFieldId).Distinct().ToList();
        var fields = await db.FormFields.AsNoTracking().Include(field => field.Question).ThenInclude(question => question.Options)
            .Where(field => fieldIds.Contains(field.Id) && field.FormSection.FormId == response.FormId).ToListAsync();
        var answers = await db.FormResponses.AsNoTracking().Where(answer => answer.ResponseId == response.Id && fieldIds.Contains(answer.FormFieldId)).ToListAsync();
        var pins = await LegacyFormulaConfigurationReader.Latest(db, [response.Id]);
        var result = new List<AssayReportingFieldSnapshot>();
        foreach (var group in configured.GroupBy(item => item.Rule.FormFieldId))
        {
            var specification = group.First().Specification;
            var field = fields.SingleOrDefault(field => field.Id == group.Key);
            var answer = answers.Where(answer => answer.FormFieldId == group.Key).OrderByDescending(answer => answer.CreatedAt).ThenByDescending(answer => answer.Id).FirstOrDefault();
            var answerHash = answer is null ? "" : LegacyFormulaRepairDefinition.Hash(answer.Value ?? "");
            var definitionEvidence = "";
            AssayReportingFieldSnapshot Snapshot(List<AssayReportedResultDto> values, string? error = null) =>
                new(group.Key, specification.Id, atr.ProductId, specification.RevisionNumber, values, error!, answer?.Id, answerHash, definitionEvidence);
            try
            {
                if (group.Select(item => item.Specification.Id).Distinct().Count() != 1) throw new ArgumentException("More than one specification configures this assay. Resolve the product's specification before generating its COA.");
                if (specification.FormId != response.FormId) throw new ArgumentException("The product specification now selects a different template. Review this test and its reporting settings before generating the COA.");
                if (!specification.IsVerified) throw new ArgumentException("Approve this product specification's reporting settings before generating its COA.");
                if (field is null || answer is null) throw new ArgumentException("Save the selected assay's readings before generating its COA.");
                var pin = pins.SingleOrDefault(pin => pin.FormFieldId == field.Id);
                var authoring = pin?.Definition;
                if (response.FormRevisionId.HasValue)
                    authoring = await db.FormFieldRevisions.AsNoTracking().Where(revision => revision.FormRevisionId == response.FormRevisionId && revision.FormFieldId == field.Id)
                        .Select(revision => revision.FormulaConfiguration.FormulaRevision.AuthoringPayloadJson).SingleOrDefaultAsync();
                var source = await AssayFormulaSource.Load(db, field, authoring);
                definitionEvidence = source.Authoring;
                if (group.Any(item => item.Rule.DefinitionHash != source.Hash || !source.Outputs.Any(output => output.Key == item.Rule.ResultKey && output.CanConvert))) throw new ArgumentException("The selected assay formula changed. Review its COA reporting settings for this product.");
                Dictionary<string, decimal> values;
                if (response.FormRevisionId.HasValue)
                {
                    var placement = await db.FormFieldRevisions.AsNoTracking().Where(revision => revision.FormRevisionId == response.FormRevisionId && revision.FormFieldId == field.Id)
                        .Select(revision => revision.PlacementKey).SingleAsync();
                    var snapshot = await db.ResponseFormulaSnapshots.AsNoTracking().Where(snapshot => snapshot.ResponseId == response.Id && snapshot.PlacementKey == placement)
                        .OrderByDescending(snapshot => snapshot.Sequence).FirstOrDefaultAsync();
                    var execution = snapshot is null ? null : await db.FormulaExecutions.AsNoTracking().Where(execution => execution.ResponseFormulaSnapshotId == snapshot.Id
                        && execution.Authority == FormulaExecutionAuthority.AuthoritativeServer && execution.Status == FormulaExecutionStatus.Valid)
                        .OrderByDescending(execution => execution.ExecutedAt).ThenByDescending(execution => execution.Id).FirstOrDefaultAsync();
                    if (execution is null || snapshot!.DefinitionHash != source.DefinitionHash) throw new ArgumentException("Recalculate and save this assay with its approved formula before generating the COA.");
                    using var raw = JsonDocument.Parse(execution.RawResultsJson);
                    values = AssayFormulaSource.ReadExact(raw.RootElement, "value");
                }
                else
                {
                    // The same server engine calculates legacy worksheets from saved readings, including their average.
                    var evaluated = await source.Evaluate(db, client, answer.Value);
                    if (evaluated.IsFailure) throw new ArgumentException(evaluated.Errors.First().Description);
                    values = evaluated.Value;
                }
                var rules = group.ToDictionary(item => item.Rule.ResultKey, item => item.Rule);
                if (rules.Keys.Any(key => !values.ContainsKey(key))) throw new ArgumentException("A selected assay output is unavailable. Recalculate it before generating the COA.");
                var reportable = source.Average ? values.Where(pair => pair.Key == "average" || rules.ContainsKey(pair.Key)) : values;
                var reported = reportable.Select(pair => rules.TryGetValue(pair.Key, out var rule)
                    ? Convert(rule, source.Outputs.Single(output => output.Key == pair.Key).Label, pair.Value)
                    : new AssayReportedResultDto(pair.Key, source.Outputs.Single(output => output.Key == pair.Key).Label, pair.Value, pair.Value,
                        pair.Value.ToString("G29", CultureInfo.InvariantCulture), source.Outputs.Single(output => output.Key == pair.Key).Unit ?? "%", 0, 0, null, 0, source.Hash, false)).ToList();
                result.Add(Snapshot(reported));
            }
            catch (Exception error) when (error is ArgumentException or JsonException or InvalidOperationException or FormatException or OverflowException or DivideByZeroException)
            { result.Add(Snapshot([], error.Message)); }
        }
        return result;
    }

    internal static async Task<Result> Capture(ApplicationDbContext db, Response response, Guid actorId, IFormulaCalculationClient? client)
    {
        if (response.AssayReportingSnapshotJson is not null) return Result.Success();
        var fields = await Build(db, response, client);
        var error = fields.FirstOrDefault(field => !string.IsNullOrWhiteSpace(field.Error));
        if (error is not null) return Error.Conflict("AssayReporting.Unresolved", error.Error);
        response.AssayReportingSnapshotJson = JsonSerializer.Serialize(new AssayReportingSnapshot(actorId, DateTime.UtcNow, fields));
        return Result.Success();
    }

    internal static async Task Attach(ApplicationDbContext db, IReadOnlyCollection<FormResponse> sources,
        IReadOnlyCollection<FormResponseDto> destinations, IFormulaCalculationClient? client)
    {
        var ids = sources.Select(answer => answer.ResponseId).Distinct().ToList();
        var responses = await db.Responses.AsNoTracking().Where(response => ids.Contains(response.Id) && response.BatchManufacturingRecordId != null).ToListAsync();
        foreach (var response in responses)
        {
            var fields = await Build(db, response, client);
            foreach (var destination in destinations.Where(destination => destination.ResponseId == response.Id))
            {
                var source = sources.Single(answer => answer.Id == destination.Id);
                var field = fields.SingleOrDefault(field => field.FormFieldId == source.FormFieldId);
                if (field is null) continue;
                destination.CoaReportingError = field.Error;
                destination.CoaReportedResults = field.Results;
            }
        }
    }
}
