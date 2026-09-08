using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace INFRASTRUCTURE.Context;

internal static class FormulaMigrationChangeGuard
{
    public static void Validate(EntityEntry entry, FormulaMigrationRun run)
    {
        if (entry.State == EntityState.Deleted)
            throw new InvalidOperationException("Formula migration runs cannot be deleted.");
        if (entry.State != EntityState.Modified)
            return;

        var priorMode = entry.OriginalValues.GetValue<FormulaMigrationRunMode>(
            nameof(FormulaMigrationRun.Mode));
        var priorStatus = entry.OriginalValues.GetValue<FormulaMigrationRunStatus>(
            nameof(FormulaMigrationRun.Status));
        if (priorMode == FormulaMigrationRunMode.DryRun ||
            priorStatus is FormulaMigrationRunStatus.Completed or FormulaMigrationRunStatus.Failed)
            throw new InvalidOperationException(
                "Completed, failed, and dry-run migration evidence is immutable.");

        string[] immutableProperties =
        [
            nameof(FormulaMigrationRun.ReleaseId),
            nameof(FormulaMigrationRun.SourceFingerprint),
            nameof(FormulaMigrationRun.CorpusChecksum),
            nameof(FormulaMigrationRun.CodeVersion),
            nameof(FormulaMigrationRun.ApplyManifestHash),
            nameof(FormulaMigrationRun.SignedReportHash),
            nameof(FormulaMigrationRun.SignedReportLocation),
            nameof(FormulaMigrationRun.Mode),
            nameof(FormulaMigrationRun.InitiatedById),
            nameof(FormulaMigrationRun.StartedAt)
        ];
        if (immutableProperties.Any(name => entry.Property(name).IsModified))
            throw new InvalidOperationException(
                "Formula migration run identity and provenance are immutable.");

        var currentStatus = run.Status;
        if (priorStatus == currentStatus)
            return;
        var allowed = (priorStatus, currentStatus) switch
        {
            (FormulaMigrationRunStatus.Pending, FormulaMigrationRunStatus.Running) => true,
            (FormulaMigrationRunStatus.Running, FormulaMigrationRunStatus.Completed) => true,
            (FormulaMigrationRunStatus.Running, FormulaMigrationRunStatus.Failed) => true,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException(
                $"Illegal formula migration run transition: {priorStatus} -> {currentStatus}.");
        if (currentStatus is FormulaMigrationRunStatus.Completed or FormulaMigrationRunStatus.Failed &&
            !run.CompletedAt.HasValue)
            throw new InvalidOperationException(
                "A terminal formula migration run requires a completion time.");
    }
}
