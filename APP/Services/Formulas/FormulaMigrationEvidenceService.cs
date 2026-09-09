using System.Data;
using System.Text.Json;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SHARED.Services.Identity;

#nullable enable

namespace APP.Services.Formulas;

public sealed class FormulaMigrationEvidenceService(
    ApplicationDbContext context,
    IFormulaMigrationInventoryService inventory,
    ICurrentUserService currentUser
) : IFormulaMigrationEvidenceService
{
    public async Task<FormulaMigrationEvidenceReceipt> RecordDryRunAsync(
        FormulaMigrationDryRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedAccessException(
            "An authenticated user is required to record migration evidence.");
        IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken)
            : null;

        try
        {
            var report = await inventory.DryRunAsync(request, cancellationToken);
            var runId = FormulaMigrationHashing.DeterministicId(request.ReleaseId,
                "migration-run", $"{report.SourceFingerprint}:dry-run");
            var existing = await context.FormulaMigrationRuns
                .Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == runId, cancellationToken);
            if (existing is not null)
            {
                ValidateExisting(existing, report);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return new FormulaMigrationEvidenceReceipt(runId, true, report);
            }

            var now = DateTime.UtcNow;
            await AddMissingArtifactsAsync(request.ReleaseId, report, now, cancellationToken);
            context.FormulaMigrationRuns.Add(BuildRun(runId, actorId, report, now));
            context.FormulaMigrationItems.AddRange(report.Items.Select(item =>
                BuildMigrationItem(runId, item)));
            context.FormulaReconciliationResults.AddRange(report.Controls.Select(control =>
                BuildControl(runId, control, now)));
            await context.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return new FormulaMigrationEvidenceReceipt(runId, false, report);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private async Task AddMissingArtifactsAsync(string releaseId,
        FormulaMigrationDryRunReport report, DateTime capturedAt,
        CancellationToken cancellationToken)
    {
        var optionIds = report.Items.Where(item => item.QuestionOptionId.HasValue)
            .Select(item => item.QuestionOptionId!.Value).ToList();
        var options = await context.QuestionOptions.IgnoreQueryFilters().AsNoTracking()
            .Where(item => optionIds.Contains(item.Id))
            .Select(item => new EvidenceOptionRow(item.Id, item.Name, item.CreatedAt,
                item.UpdatedAt, item.DeletedAt))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var paths = report.Items.Select(item => item.LegacyPath).ToList();
        var existingIds = await context.LegacyFormulaArtifacts.AsNoTracking()
            .Where(item => item.DatabaseFingerprint == report.SourceFingerprint &&
                paths.Contains(item.SourcePath))
            .Select(item => item.Id)
            .ToHashSetAsync(cancellationToken);

        foreach (var item in report.Items.Where(item => !existingIds.Contains(item.LegacyArtifactId)))
        {
            options.TryGetValue(item.QuestionOptionId ?? Guid.Empty, out var option);
            var rawPayload = option?.Payload;
            if (FormulaMigrationHashing.Sha256(rawPayload ?? "<missing>") != item.SourceHash)
                throw new InvalidOperationException(
                    $"Formula source changed while evidence was captured: {item.LegacyPath}.");
            context.LegacyFormulaArtifacts.Add(new LegacyFormulaArtifact
            {
                Id = item.LegacyArtifactId,
                QuestionId = item.QuestionId,
                QuestionOptionId = item.QuestionOptionId,
                OriginalPayload = SerializePayload(rawPayload, item.LegacyPath),
                SourceHash = item.SourceHash,
                SourcePath = item.LegacyPath,
                SourceWasDeleted = !item.IsActive,
                SourceCreatedAt = option?.CreatedAt,
                SourceUpdatedAt = option?.UpdatedAt,
                SourceDeletedAt = option?.DeletedAt,
                DatabaseFingerprint = report.SourceFingerprint,
                MigrationReleaseId = releaseId,
                CapturedAt = capturedAt
            });
        }
    }

    private static FormulaMigrationRun BuildRun(Guid id, Guid actorId,
        FormulaMigrationDryRunReport report, DateTime now) => new()
    {
        Id = id,
        ReleaseId = report.ReleaseId,
        SourceFingerprint = report.SourceFingerprint,
        CorpusChecksum = report.CorpusChecksum,
        CodeVersion = report.CodeVersion,
        ApplyManifestHash = null,
        SignedReportHash = null,
        Mode = FormulaMigrationRunMode.DryRun,
        Status = FormulaMigrationRunStatus.Completed,
        InitiatedById = actorId,
        StartedAt = now,
        CompletedAt = now,
        TotalCount = report.Items.Count,
        SucceededCount = report.Items.Count(item => item.Ready),
        FailedCount = report.Items.Count(item => !item.Ready),
        SignedReportLocation = string.Empty
    };

    private static FormulaMigrationItem BuildMigrationItem(Guid runId,
        FormulaMigrationDryRunItem item) => new()
    {
        Id = FormulaMigrationHashing.DeterministicId(runId.ToString("N"),
            "migration-item", item.LegacyArtifactId.ToString("N")),
        FormulaMigrationRunId = runId,
        LegacyFormulaArtifactId = item.LegacyArtifactId,
        SourceHash = item.SourceHash,
        PlacementKey = $"question:{item.QuestionId:N}",
        TargetId = item.TargetRevisionId,
        MigrationClass = item.MigrationClass,
        Action = item.Action ?? "Unclassified",
        ApprovalReference = item.ApprovalReference ?? string.Empty,
        ApprovalScope = item.ApprovalScope,
        Status = ItemStatus(item),
        Error = string.Join(',', item.Diagnostics),
        BeforeHash = item.SourceHash,
        AfterHash = item.TargetDefinitionHash
    };

    private static FormulaMigrationItemStatus ItemStatus(FormulaMigrationDryRunItem item) =>
        !item.Ready ? FormulaMigrationItemStatus.Failed :
        item.MigrationClass == FormulaMigrationClass.Unrecoverable
            ? FormulaMigrationItemStatus.Skipped
            : FormulaMigrationItemStatus.Pending;

    private static FormulaReconciliationResult BuildControl(Guid runId,
        FormulaMigrationControl control, DateTime now) => new()
    {
        Id = FormulaMigrationHashing.DeterministicId(runId.ToString("N"),
            "reconciliation", control.Name),
        FormulaMigrationRunId = runId,
        ControlName = control.Name,
        ExpectedValue = control.Expected,
        ActualValue = control.Actual,
        Passed = control.Passed,
        EvidenceJson = JsonSerializer.Serialize(new { control.Name, control.Passed }),
        CheckedAt = now
    };

    private static string SerializePayload(string? payload, string path) =>
        JsonSerializer.Serialize(new { encoding = "utf-8", raw = payload, sourcePath = path });

    private static void ValidateExisting(FormulaMigrationRun existing,
        FormulaMigrationDryRunReport report)
    {
        if (existing.CorpusChecksum != report.CorpusChecksum ||
            existing.CodeVersion != report.CodeVersion ||
            existing.Items.Count != report.Items.Count)
            throw new InvalidOperationException(
                "The recorded release does not match this dry-run request.");

        var actual = existing.Items.OrderBy(item => item.LegacyFormulaArtifactId).ToList();
        var expected = report.Items.OrderBy(item => item.LegacyArtifactId).ToList();
        for (var index = 0; index < expected.Count; index++)
        {
            if (actual[index].LegacyFormulaArtifactId != expected[index].LegacyArtifactId ||
                actual[index].MigrationClass != expected[index].MigrationClass ||
                actual[index].AfterHash != expected[index].TargetDefinitionHash ||
                actual[index].Action != (expected[index].Action ?? "Unclassified") ||
                actual[index].ApprovalReference != (expected[index].ApprovalReference ?? string.Empty) ||
                actual[index].ApprovalScope != expected[index].ApprovalScope)
                throw new InvalidOperationException(
                    "The recorded release has different migration decisions.");
        }
    }
}

internal sealed record EvidenceOptionRow(Guid Id, string Payload, DateTime CreatedAt,
    DateTime? UpdatedAt, DateTime? DeletedAt);
