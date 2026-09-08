using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.StpDocuments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace INFRASTRUCTURE.Context;

internal static class FormulaChangeGuard
{
    private static readonly HashSet<Type> AppendOnlyTypes =
    [
        typeof(FormulaRevisionAudit),
        typeof(FormRevisionAudit),
        typeof(LegacyFormulaArtifact),
        typeof(LegacyKeyMapping),
        typeof(ResponseFormulaSnapshot),
        typeof(FormulaExecution),
        typeof(ResponseFormulaSubmissionSet),
        typeof(ResponseFormulaSubmissionExecution),
        typeof(FormulaMigrationItem),
        typeof(FormulaReconciliationResult),
        typeof(StpDocumentVersion),
        typeof(StpDocumentSignature)
    ];

    public static void Validate(ChangeTracker changeTracker)
    {
        var entries = changeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            if (AppendOnlyTypes.Any(type => type.IsInstanceOfType(entry.Entity)) &&
                entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException(
                    $"{entry.Entity.GetType().Name} is append-only; insert a superseding record."
                );
            }
            if (entry.Entity is FormulaRevision revision)
                ValidateRevision(entry, revision, entries);

            if (entry.Entity is FormRevision formRevision)
                ValidateFormRevision(entry, formRevision, entries);

            if (entry.Entity is ResponseFormulaSnapshot snapshot &&
                entry.State == EntityState.Added)
                ValidateSnapshot(snapshot);

            if (entry.Entity is FormulaExecution execution &&
                entry.State == EntityState.Added &&
                execution.Id == execution.SupersedesExecutionId)
                throw new InvalidOperationException("A formula execution cannot supersede itself.");

            if (entry.Entity is FormulaMigrationRun migrationRun)
                FormulaMigrationChangeGuard.Validate(entry, migrationRun);
        }
    }

    private static void ValidateRevision(
        EntityEntry entry,
        FormulaRevision revision,
        IReadOnlyCollection<EntityEntry> entries
    )
    {
        if (entry.State == EntityState.Deleted)
            throw new InvalidOperationException("Formula revisions must be retired, never deleted.");
        if (entry.State == EntityState.Added)
        {
            if (revision.Status != FormulaRevisionStatus.Draft)
                throw new InvalidOperationException("A formula revision must be created as Draft.");
            return;
        }
        if (entry.State != EntityState.Modified)
            return;

        var prior = entry.OriginalValues.GetValue<FormulaRevisionStatus>(
            nameof(FormulaRevision.Status)
        );
        var current = revision.Status;
        string[] frozenProperties =
        [
            nameof(FormulaRevision.DefinitionJson),
            nameof(FormulaRevision.TestCasesJson),
            nameof(FormulaRevision.DefinitionHash),
            nameof(FormulaRevision.ReleaseEvidenceHash),
            nameof(FormulaRevision.FormulaLanguageVersion),
            nameof(FormulaRevision.NumericPolicyVersion)
        ];
        if (prior != FormulaRevisionStatus.Draft &&
            frozenProperties.Any(name => entry.Property(name).IsModified))
        {
            throw new InvalidOperationException(
                "A formula revision's definition and evidence are immutable after review starts."
            );
        }

        if (prior == current)
            return;
        if (!IsAllowedTransition(prior, current))
            throw new InvalidOperationException($"Illegal formula revision transition: {prior} -> {current}.");
        if (current == FormulaRevisionStatus.Approved &&
            (!revision.ApprovedById.HasValue || !revision.ApprovedAt.HasValue ||
             !revision.EffectiveAt.HasValue))
            throw new InvalidOperationException(
                "An approved formula revision requires approver, approval time, and effective time."
            );
        if (current == FormulaRevisionStatus.Retired && !revision.RetiredAt.HasValue)
            throw new InvalidOperationException(
                "A retired formula revision requires a retirement time."
            );

        var matchingAudit = entries
            .Where(item => item.State == EntityState.Added)
            .Select(item => item.Entity)
            .OfType<FormulaRevisionAudit>()
            .Any(audit =>
                audit.FormulaRevisionId == revision.Id &&
                audit.PriorStatus == prior &&
                audit.NewStatus == current &&
                audit.DefinitionHash == revision.DefinitionHash &&
                audit.ActorId != Guid.Empty &&
                audit.CorrelationId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(audit.Reason));
        if (!matchingAudit)
            throw new InvalidOperationException(
                "A formula revision status transition requires a matching append-only audit record."
            );
    }

    private static bool IsAllowedTransition(
        FormulaRevisionStatus prior,
        FormulaRevisionStatus current
    ) => (prior, current) switch
    {
        (FormulaRevisionStatus.Draft, FormulaRevisionStatus.InReview) => true,
        (FormulaRevisionStatus.InReview, FormulaRevisionStatus.Draft) => true,
        (FormulaRevisionStatus.InReview, FormulaRevisionStatus.Approved) => true,
        (FormulaRevisionStatus.Approved, FormulaRevisionStatus.Retired) => true,
        _ => false
    };

    private static void ValidateFormRevision(
        EntityEntry entry,
        FormRevision revision,
        IReadOnlyCollection<EntityEntry> entries)
    {
        if (entry.State == EntityState.Deleted)
            throw new InvalidOperationException("Form revisions must be retired, never deleted.");
        if (entry.State == EntityState.Added)
        {
            if (revision.Status != FormRevisionStatus.Draft)
                throw new InvalidOperationException("A form revision must be created as Draft.");
            return;
        }
        if (entry.State != EntityState.Modified)
            return;
        var prior = entry.OriginalValues.GetValue<FormRevisionStatus>(nameof(FormRevision.Status));
        var current = revision.Status;
        if (prior != FormRevisionStatus.Draft &&
            entry.Property(nameof(FormRevision.ContentHash)).IsModified)
            throw new InvalidOperationException(
                "A form revision's content is immutable after review starts."
            );
        if (prior == current)
            return;
        if (!IsAllowedFormTransition(prior, current))
            throw new InvalidOperationException($"Illegal form revision transition: {prior} -> {current}.");
        if (current == FormRevisionStatus.Approved &&
            (!revision.ApprovedById.HasValue || !revision.ApprovedAt.HasValue))
            throw new InvalidOperationException(
                "An approved form revision requires approver and approval time."
            );
        if (current == FormRevisionStatus.Retired && !revision.RetiredAt.HasValue)
            throw new InvalidOperationException(
                "A retired form revision requires a retirement time.");
        var matchingAudit = entries.Where(item => item.State == EntityState.Added)
            .Select(item => item.Entity).OfType<FormRevisionAudit>().Any(audit =>
                audit.FormRevisionId == revision.Id && audit.PriorStatus == prior &&
                audit.NewStatus == current && audit.ContentHash == revision.ContentHash &&
                audit.ActorId != Guid.Empty && audit.CorrelationId != Guid.Empty &&
                !string.IsNullOrWhiteSpace(audit.Reason));
        if (!matchingAudit)
            throw new InvalidOperationException(
                "A form revision status transition requires a matching append-only audit record.");
    }

    private static bool IsAllowedFormTransition(
        FormRevisionStatus prior,
        FormRevisionStatus current
    ) => (prior, current) switch
    {
        (FormRevisionStatus.Draft, FormRevisionStatus.InReview) => true,
        (FormRevisionStatus.InReview, FormRevisionStatus.Draft) => true,
        (FormRevisionStatus.InReview, FormRevisionStatus.Approved) => true,
        (FormRevisionStatus.Approved, FormRevisionStatus.Retired) => true,
        _ => false
    };

    private static void ValidateSnapshot(ResponseFormulaSnapshot snapshot)
    {
        if (snapshot.Sequence < 1)
            throw new InvalidOperationException("Formula snapshot sequence must start at one.");
        if (snapshot.Sequence == 1 && snapshot.SupersedesSnapshotId.HasValue)
            throw new InvalidOperationException("The initial formula snapshot cannot supersede another snapshot.");
        if (snapshot.Sequence > 1 && !snapshot.SupersedesSnapshotId.HasValue)
            throw new InvalidOperationException("A later formula snapshot requires its predecessor.");
        if (snapshot.Id == snapshot.SupersedesSnapshotId)
            throw new InvalidOperationException("A formula snapshot cannot supersede itself.");

        var predecessor = snapshot.SupersedesSnapshot;
        if (predecessor is null)
            return;
        if (predecessor.ResponseId != snapshot.ResponseId ||
            predecessor.PlacementKey != snapshot.PlacementKey ||
            predecessor.Sequence != snapshot.Sequence - 1)
            throw new InvalidOperationException(
                "A formula snapshot must supersede the preceding generation in the same response placement."
            );
    }

}
