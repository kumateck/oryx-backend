using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

public sealed class FormulaMigrationInventoryService(ApplicationDbContext context)
    : IFormulaMigrationInventoryService
{
    public async Task<FormulaMigrationDryRunReport> DryRunAsync(
        FormulaMigrationDryRunRequest request,
        CancellationToken cancellationToken = default
    )
    {
        FormulaMigrationRequestValidator.Validate(request);
        var source = await ReadSourceAsync(cancellationToken);
        var fingerprint = BuildFingerprint(source);
        var decisionLookup = BuildDecisionLookup(request.Decisions);
        var items = source.Artifacts
            .Select(artifact => BuildItem(fingerprint, artifact, decisionLookup))
            .ToList();
        var artifactKeys = source.Artifacts.Select(item => item.Key).ToHashSet();
        var unexpected = decisionLookup.Keys.Count(key => !artifactKeys.Contains(key));
        bool? fingerprintMatches = string.IsNullOrWhiteSpace(request.ExpectedSourceFingerprint)
            ? null
            : string.Equals(
                request.ExpectedSourceFingerprint,
                fingerprint,
                StringComparison.Ordinal
            );
        var controls = BuildControls(source, items, request.Decisions.Count, unexpected,
            request.ExpectedSourceFingerprint, fingerprint);
        return new FormulaMigrationDryRunReport(
            request.ReleaseId,
            fingerprint,
            request.CorpusChecksum,
            request.CodeVersion,
            fingerprintMatches,
            fingerprintMatches == true && unexpected == 0 && items.All(item => item.Ready),
            items,
            controls
        );
    }

    private async Task<SourceInventory> ReadSourceAsync(CancellationToken cancellationToken)
    {
        var questions = await context.Questions.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.Type == QuestionType.Formula)
            .Select(item => new QuestionRow(item.Id, item.DeletedAt))
            .ToListAsync(cancellationToken);
        var questionIds = questions.Select(item => item.Id).ToList();
        var options = await context.QuestionOptions.IgnoreQueryFilters().AsNoTracking()
            .Where(item => questionIds.Contains(item.QuestionId))
            .Select(item => new OptionRow(item.Id, item.QuestionId, item.Name, item.CreatedAt,
                item.UpdatedAt, item.DeletedAt))
            .ToListAsync(cancellationToken);
        var fields = await context.FormFields.IgnoreQueryFilters().AsNoTracking()
            .Where(item => questionIds.Contains(item.QuestionId))
            .Select(item => new FieldRow(item.Id, item.QuestionId, item.FormSectionId, item.DeletedAt))
            .ToListAsync(cancellationToken);
        var sectionIds = fields.Select(item => item.FormSectionId).Distinct().ToList();
        var sections = await context.FormSections.IgnoreQueryFilters().AsNoTracking()
            .Where(item => sectionIds.Contains(item.Id))
            .Select(item => new SectionRow(item.Id, item.FormId, item.DeletedAt))
            .ToListAsync(cancellationToken);
        var formIds = sections.Select(item => item.FormId).Distinct().ToList();
        var forms = await context.Forms.IgnoreQueryFilters().AsNoTracking()
            .Where(item => formIds.Contains(item.Id))
            .Select(item => new FormRow(item.Id, item.DeletedAt))
            .ToListAsync(cancellationToken);
        var fieldIds = fields.Select(item => item.Id).ToList();
        var responses = await context.FormResponses.IgnoreQueryFilters().AsNoTracking()
            .Where(item => fieldIds.Contains(item.FormFieldId))
            .Select(item => new ResponseRow(item.Id, item.ResponseId, item.FormFieldId,
                item.Value, item.CreatedAt, item.DeletedAt))
            .ToListAsync(cancellationToken);

        return CreateInventory(questions, options, fields, sections, forms, responses);
    }

    private static SourceInventory CreateInventory(
        IReadOnlyList<QuestionRow> questions,
        IReadOnlyList<OptionRow> options,
        IReadOnlyList<FieldRow> fields,
        IReadOnlyList<SectionRow> sections,
        IReadOnlyList<FormRow> forms,
        IReadOnlyList<ResponseRow> responses)
    {
        var sectionLookup = sections.ToDictionary(item => item.Id);
        var formLookup = forms.ToDictionary(item => item.Id);
        var activeFields = fields.Where(field => field.DeletedAt is null &&
            sectionLookup.TryGetValue(field.FormSectionId, out var section) &&
            section.DeletedAt is null && formLookup.TryGetValue(section.FormId, out var form) &&
            form.DeletedAt is null).ToList();
        var placementCounts = activeFields.GroupBy(item => item.QuestionId)
            .ToDictionary(group => group.Key, group => group.Count());
        var responseCounts = responses.Where(item => item.DeletedAt is null &&
                !string.IsNullOrEmpty(item.Value))
            .GroupBy(item => item.FormFieldId)
            .ToDictionary(group => group.Key, group => group.Count());
        var artifacts = new List<SourceArtifact>();

        foreach (var question in questions.OrderBy(item => item.Id))
        {
            var questionOptions = options.Where(item => item.QuestionId == question.Id).ToList();
            foreach (var option in questionOptions)
            {
                var path = $"questions/{question.Id:N}/options/{option.Id:N}";
                artifacts.Add(new SourceArtifact(question.Id, option.Id, path,
                    FormulaMigrationHashing.Sha256(option.Name),
                    question.DeletedAt is null && option.DeletedAt is null,
                    placementCounts.GetValueOrDefault(question.Id),
                    activeFields.Where(field => field.QuestionId == question.Id)
                        .Sum(field => responseCounts.GetValueOrDefault(field.Id))));
            }
            if (question.DeletedAt is null && questionOptions.All(item => item.DeletedAt is not null))
            {
                var path = $"questions/{question.Id:N}/active-option:missing";
                artifacts.Add(new SourceArtifact(question.Id, null, path,
                    FormulaMigrationHashing.Sha256("<missing>"), true,
                    placementCounts.GetValueOrDefault(question.Id),
                    activeFields.Where(field => field.QuestionId == question.Id)
                        .Sum(field => responseCounts.GetValueOrDefault(field.Id))));
            }
        }

        return new SourceInventory(artifacts.OrderBy(item => item.LegacyPath).ToList(),
            questions, options, fields, sections, forms, responses);
    }

    private static string BuildFingerprint(SourceInventory source)
    {
        var lines = new List<string>();
        lines.AddRange(source.Questions.Select(item =>
            $"Q|{item.Id:N}|{FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)}"));
        lines.AddRange(source.Options.Select(item =>
            $"O|{item.Id:N}|{item.QuestionId:N}|{FormulaMigrationHashing.Sha256(item.Name)}|" +
            $"{FormulaMigrationHashing.FormatTimestamp(item.CreatedAt)}|" +
            $"{FormulaMigrationHashing.FormatTimestamp(item.UpdatedAt)}|" +
            $"{FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)}"));
        lines.AddRange(source.Fields.Select(item =>
            $"F|{item.Id:N}|{item.QuestionId:N}|{item.FormSectionId:N}|" +
            FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)));
        lines.AddRange(source.Sections.Select(item =>
            $"S|{item.Id:N}|{item.FormId:N}|" +
            FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)));
        lines.AddRange(source.Forms.Select(item => $"T|{item.Id:N}|" +
            FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)));
        lines.AddRange(source.Responses.Select(item =>
            $"R|{item.Id:N}|{item.ResponseId:N}|{item.FormFieldId:N}|" +
            $"{FormulaMigrationHashing.Sha256(item.Value)}|" +
            $"{FormulaMigrationHashing.FormatTimestamp(item.CreatedAt)}|" +
            FormulaMigrationHashing.FormatTimestamp(item.DeletedAt)));
        return FormulaMigrationHashing.Sha256(string.Join('\n', lines.Order(StringComparer.Ordinal)));
    }

    private static FormulaMigrationDryRunItem BuildItem(string sourceFingerprint,
        SourceArtifact artifact,
        IReadOnlyDictionary<ArtifactKey, FormulaMigrationDecision> decisions)
    {
        var diagnostics = new List<string>();
        decisions.TryGetValue(artifact.Key, out var decision);
        if (decision is null)
            diagnostics.Add("MISSING_DECISION");
        else
        {
            if (!string.Equals(decision.SourceHash, artifact.SourceHash, StringComparison.Ordinal))
                diagnostics.Add("SOURCE_HASH_MISMATCH");
            if (string.IsNullOrWhiteSpace(decision.Action))
                diagnostics.Add("ACTION_REQUIRED");
            if (decision.MigrationClass != FormulaMigrationClass.Unrecoverable &&
                !FormulaMigrationHashing.IsSha256(decision.TargetDefinitionHash))
                diagnostics.Add("TARGET_DEFINITION_HASH_REQUIRED");
            if (decision.MigrationClass != FormulaMigrationClass.Unrecoverable &&
                string.IsNullOrWhiteSpace(decision.ApprovalReference))
                diagnostics.Add("APPROVAL_REFERENCE_REQUIRED");
            if (decision.MigrationClass != FormulaMigrationClass.Unrecoverable &&
                !decision.ApprovalScope.HasValue)
                diagnostics.Add("APPROVAL_SCOPE_REQUIRED");
            if (decision.MigrationClass == FormulaMigrationClass.Corrective &&
                decision.ApprovalScope != FormulaMigrationApprovalScope.Individual)
                diagnostics.Add("INDIVIDUAL_APPROVAL_REQUIRED");
            if (decision.MigrationClass == FormulaMigrationClass.Unrecoverable &&
                artifact.ActivePlacements > 0)
                diagnostics.Add("ACTIVE_PLACEMENT_UNRECOVERABLE");
        }
        var sourceIdentity = $"{artifact.QuestionId:N}:{artifact.QuestionOptionId?.ToString("N") ?? "missing"}:{artifact.LegacyPath}";
        return new FormulaMigrationDryRunItem(
            FormulaMigrationHashing.DeterministicId("legacy-evidence-v1", "legacy-artifact",
                $"{sourceFingerprint}:{sourceIdentity}:{artifact.SourceHash}"),
            artifact.QuestionId, artifact.QuestionOptionId, artifact.LegacyPath,
            artifact.SourceHash, artifact.IsActive, artifact.ActivePlacements, artifact.ResponseRows,
            decision?.MigrationClass,
            decision?.TargetDefinitionHash,
            decision is null || decision.MigrationClass == FormulaMigrationClass.Unrecoverable ||
                !FormulaMigrationHashing.IsSha256(decision.TargetDefinitionHash)
                ? null
                : FormulaMigrationHashing.DeterministicId("canonical-definition-v1",
                    "formula-revision",
                    decision.TargetDefinitionHash!),
            decision?.Action,
            decision?.ApprovalReference,
            decision?.ApprovalScope,
            diagnostics.Count == 0,
            diagnostics
        );
    }

    private static IReadOnlyList<FormulaMigrationControl> BuildControls(SourceInventory source,
        IReadOnlyList<FormulaMigrationDryRunItem> items, int decisionCount, int unexpected,
        string? expectedFingerprint, string actualFingerprint) =>
    [
        new("source-fingerprint", expectedFingerprint ?? string.Empty, actualFingerprint,
            !string.IsNullOrWhiteSpace(expectedFingerprint) && expectedFingerprint == actualFingerprint),
        new("artifact-disposition-count", source.Artifacts.Count.ToString(),
            items.Count(item => item.MigrationClass.HasValue).ToString(),
            source.Artifacts.Count == items.Count(item => item.MigrationClass.HasValue)),
        new("manifest-unexpected-count", "0", unexpected.ToString(), unexpected == 0),
        new("manifest-decision-count", source.Artifacts.Count.ToString(), decisionCount.ToString(),
            source.Artifacts.Count == decisionCount)
    ];

    private static Dictionary<ArtifactKey, FormulaMigrationDecision> BuildDecisionLookup(
        IReadOnlyList<FormulaMigrationDecision> decisions)
    {
        var result = new Dictionary<ArtifactKey, FormulaMigrationDecision>();
        foreach (var decision in decisions)
        {
            var key = new ArtifactKey(decision.QuestionId, decision.QuestionOptionId,
                decision.LegacyPath);
            if (!result.TryAdd(key, decision))
                throw new ArgumentException($"Duplicate migration decision for {decision.LegacyPath}.");
        }
        return result;
    }

}
