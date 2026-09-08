using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Formulas;

internal sealed record FormulaMigrationWriteResult(
    int DefinitionsCreated,
    int RevisionsCreated,
    int QuestionLinksCreated,
    int KeyMappingsCreated
);

internal sealed class FormulaMigrationApplyWriter(ApplicationDbContext context)
{
    public async Task<FormulaMigrationWriteResult> WriteAsync(
        FormulaMigrationRun applyRun,
        FormulaMigrationRun dryRun,
        ValidatedFormulaApplyPackage package,
        CancellationToken cancellationToken)
    {
        var definitionsCreated = await EnsureDefinitionsAsync(package, cancellationToken);
        var revisionsCreated = await EnsureRevisionsAsync(
            applyRun, package, cancellationToken);
        var questionLinksCreated = await EnsureQuestionLinksAsync(
            dryRun, package, cancellationToken);
        var keyMappingsCreated = await EnsureKeyMappingsAsync(
            dryRun, package, cancellationToken);
        AddApplyItems(applyRun.Id, dryRun.Items);
        return new FormulaMigrationWriteResult(definitionsCreated, revisionsCreated,
            questionLinksCreated, keyMappingsCreated);
    }

    private async Task<int> EnsureDefinitionsAsync(ValidatedFormulaApplyPackage package,
        CancellationToken cancellationToken)
    {
        var targets = package.TargetsByHash.Values.GroupBy(item => item.DefinitionId)
            .Select(group => group.First()).ToList();
        var ids = targets.Select(item => item.DefinitionId).ToList();
        var keys = targets.Select(item => item.Source.DefinitionKey).ToList();
        var existing = await context.FormulaDefinitions.IgnoreQueryFilters()
            .Where(item => ids.Contains(item.Id) || keys.Contains(item.Key))
            .ToListAsync(cancellationToken);
        var created = 0;
        foreach (var target in targets)
        {
            var matches = existing.Where(item => item.Id == target.DefinitionId ||
                item.Key == target.Source.DefinitionKey).ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"Conflicting logical formulas exist for {target.Source.DefinitionKey}.");
            if (matches.SingleOrDefault() is { } definition)
            {
                if (definition.Id != target.DefinitionId || definition.DeletedAt.HasValue ||
                    definition.Key != target.Source.DefinitionKey ||
                    definition.Name != target.Source.Name ||
                    definition.PresentationPreset != (target.Source.PresentationPreset ?? string.Empty))
                    throw new InvalidOperationException(
                        $"Existing logical formula differs for {target.Source.DefinitionKey}.");
                continue;
            }
            context.FormulaDefinitions.Add(new FormulaDefinition
            {
                Id = target.DefinitionId,
                Key = target.Source.DefinitionKey,
                Name = target.Source.Name,
                PresentationPreset = target.Source.PresentationPreset ?? string.Empty
            });
            created++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<int> EnsureRevisionsAsync(FormulaMigrationRun applyRun,
        ValidatedFormulaApplyPackage package, CancellationToken cancellationToken)
    {
        var targets = package.TargetsByHash.Values.OrderBy(item => item.Source.DefinitionKey)
            .ThenBy(item => item.Source.Revision).ToList();
        var ids = targets.Select(item => item.RevisionId).ToList();
        var definitionIds = targets.Select(item => item.DefinitionId).Distinct().ToList();
        var existing = await context.FormulaRevisions.IgnoreQueryFilters()
            .Where(item => ids.Contains(item.Id) || definitionIds.Contains(item.FormulaDefinitionId))
            .ToListAsync(cancellationToken);
        var createTargets = new List<ValidatedFormulaTarget>();
        foreach (var target in targets)
        {
            var matches = existing.Where(item => item.Id == target.RevisionId ||
                (item.FormulaDefinitionId == target.DefinitionId &&
                 item.Revision == target.Source.Revision)).ToList();
            if (matches.Count > 1)
                throw new InvalidOperationException(
                    $"Conflicting formula revisions exist for {target.Source.DefinitionKey}.");
            if (matches.SingleOrDefault() is { } revision)
            {
                if (revision.Id != target.RevisionId)
                    throw new InvalidOperationException(
                        $"Existing revision identity differs for {target.Source.DefinitionKey}.");
                FormulaMigrationApplyFactory.ValidateExistingRevision(
                    revision, target, applyRun.SignedReportHash);
            }
            else
                createTargets.Add(target);
        }
        var created = createTargets.Select(target =>
            FormulaMigrationApplyFactory.CreateDraft(target, applyRun.SignedReportHash)).ToList();
        context.FormulaRevisions.AddRange(created);
        await context.SaveChangesAsync(cancellationToken);
        if (created.Count == 0)
            return 0;

        var targetLookup = package.TargetsByHash.Values.ToDictionary(
            item => item.RevisionId);
        var now = DateTime.UtcNow;
        foreach (var revision in created)
        {
            var target = targetLookup[revision.Id].Source;
            revision.Status = FormulaRevisionStatus.InReview;
            revision.ReviewedById = target.ReviewedById;
            revision.ReviewedAt = now;
            context.FormulaRevisionAudits.Add(FormulaMigrationApplyFactory.CreateAudit(
                applyRun.Id, revision,
                FormulaRevisionStatus.Draft, FormulaRevisionStatus.InReview,
                target.ReviewedById, target.ReviewReason, "MigrationReview", now));
        }
        await context.SaveChangesAsync(cancellationToken);
        now = DateTime.UtcNow;
        foreach (var revision in created)
        {
            var target = targetLookup[revision.Id].Source;
            revision.Status = FormulaRevisionStatus.Approved;
            revision.ApprovedById = target.ApprovedById;
            revision.ApprovedAt = now;
            revision.EffectiveAt = now;
            context.FormulaRevisionAudits.Add(FormulaMigrationApplyFactory.CreateAudit(
                applyRun.Id, revision,
                FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved,
                target.ApprovedById, target.ApprovalReason, "MigrationApproval", now));
        }
        await context.SaveChangesAsync(cancellationToken);
        return created.Count;
    }

    private async Task<int> EnsureQuestionLinksAsync(FormulaMigrationRun dryRun,
        ValidatedFormulaApplyPackage package, CancellationToken cancellationToken)
    {
        var desired = dryRun.Items.Where(item =>
                item.MigrationClass != FormulaMigrationClass.Unrecoverable)
            .Select(item => new
            {
                item.LegacyFormulaArtifact.QuestionId,
                package.TargetsByHash[item.AfterHash!].DefinitionId
            }).Distinct().ToList();
        var questionIds = desired.Select(item => item.QuestionId).Distinct().ToList();
        var existing = await context.QuestionFormulaDefinitions.IgnoreQueryFilters()
            .Where(item => questionIds.Contains(item.QuestionId)).ToListAsync(cancellationToken);
        var created = 0;
        foreach (var link in desired)
        {
            var match = existing.SingleOrDefault(item => item.QuestionId == link.QuestionId &&
                item.FormulaDefinitionId == link.DefinitionId);
            if (match is not null)
            {
                if (match.DeletedAt.HasValue)
                    throw new InvalidOperationException(
                        "A required question-formula link was previously deleted.");
                continue;
            }
            context.QuestionFormulaDefinitions.Add(new QuestionFormulaDefinition
            {
                Id = FormulaMigrationHashing.DeterministicId("canonical-definition-v1",
                    "question-formula", $"{link.QuestionId:N}:{link.DefinitionId:N}"),
                QuestionId = link.QuestionId,
                FormulaDefinitionId = link.DefinitionId
            });
            created++;
        }
        await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    private async Task<int> EnsureKeyMappingsAsync(FormulaMigrationRun dryRun,
        ValidatedFormulaApplyPackage package, CancellationToken cancellationToken)
    {
        var desired = dryRun.Items.Where(item =>
                item.MigrationClass != FormulaMigrationClass.Unrecoverable)
            .SelectMany(item => package.EvidenceByArtifact[item.LegacyFormulaArtifactId]
                .KeyMappings.Select(mapping => new
                {
                    Item = item,
                    Evidence = package.EvidenceByArtifact[item.LegacyFormulaArtifactId],
                    Mapping = mapping
                })).ToList();
        var ids = desired.Select(item => FormulaMigrationApplyFactory.MappingId(
            item.Item.LegacyFormulaArtifactId,
            item.Mapping)).ToList();
        var existing = await context.LegacyKeyMappings.Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var entry in desired)
        {
            var id = FormulaMigrationApplyFactory.MappingId(
                entry.Item.LegacyFormulaArtifactId, entry.Mapping);
            if (existing.TryGetValue(id, out var mapping))
            {
                FormulaMigrationApplyFactory.ValidateExistingMapping(
                    mapping, entry.Mapping, entry.Evidence);
                continue;
            }
            context.LegacyKeyMappings.Add(new LegacyKeyMapping
            {
                Id = id,
                LegacyFormulaArtifactId = entry.Item.LegacyFormulaArtifactId,
                KeyKind = entry.Mapping.KeyKind,
                LegacyPath = entry.Mapping.LegacyPath,
                LegacyKey = entry.Mapping.LegacyKey,
                CanonicalKey = entry.Mapping.CanonicalKey,
                NormalizationReason = entry.Mapping.NormalizationReason,
                ReviewedById = entry.Evidence.ReviewedById,
                ApprovalReference = entry.Evidence.ApprovalReference,
                ReviewedAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        return desired.Count - existing.Count;
    }

    private void AddApplyItems(Guid runId, IReadOnlyList<FormulaMigrationItem> dryItems)
    {
        context.FormulaMigrationItems.AddRange(dryItems.Select(item => new FormulaMigrationItem
        {
            Id = FormulaMigrationHashing.DeterministicId(runId.ToString("N"),
                "migration-item", item.LegacyFormulaArtifactId.ToString("N")),
            FormulaMigrationRunId = runId,
            LegacyFormulaArtifactId = item.LegacyFormulaArtifactId,
            SourceHash = item.SourceHash,
            PlacementKey = item.PlacementKey,
            TargetId = item.TargetId,
            MigrationClass = item.MigrationClass,
            Action = item.Action,
            ApprovalReference = item.ApprovalReference,
            ApprovalScope = item.ApprovalScope,
            Status = item.MigrationClass == FormulaMigrationClass.Unrecoverable
                ? FormulaMigrationItemStatus.Skipped
                : FormulaMigrationItemStatus.Applied,
            Error = string.Empty,
            BeforeHash = item.BeforeHash,
            AfterHash = item.AfterHash
        }));
    }
}
