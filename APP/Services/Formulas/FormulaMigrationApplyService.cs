using System.Data;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SHARED.Services.Identity;

#nullable enable

namespace APP.Services.Formulas;

public sealed class FormulaMigrationApplyService(
    ApplicationDbContext context,
    IFormulaMigrationInventoryService inventory,
    ICurrentUserService currentUser
) : IFormulaMigrationApplyService
{
    public async Task<FormulaMigrationApplyReceipt> ApplyApprovedDefinitionsAsync(
        FormulaMigrationApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = currentUser.UserId ?? throw new UnauthorizedAccessException(
            "An authenticated user is required to apply formula migrations.");
        IDbContextTransaction? transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken)
            : null;
        try
        {
            var dryRun = await LoadDryRunAsync(request.DryRunId, cancellationToken);
            var controls = await context.FormulaReconciliationResults.AsNoTracking()
                .Where(item => item.FormulaMigrationRunId == dryRun.Id)
                .ToListAsync(cancellationToken);
            var package = FormulaMigrationApplyValidation.Validate(
                request, dryRun, controls, actorId);
            await ValidateUsersAsync(request, actorId, cancellationToken);
            await ValidateLiveSourceAsync(dryRun, cancellationToken);

            var runId = FormulaMigrationHashing.DeterministicId(dryRun.ReleaseId,
                "migration-run", $"{dryRun.SourceFingerprint}:apply");
            var existing = await context.FormulaMigrationRuns.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == runId, cancellationToken);
            if (existing is not null)
            {
                ValidateExistingApply(existing, request, dryRun);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
                return new FormulaMigrationApplyReceipt(runId, true, 0, 0, 0, 0);
            }

            var now = DateTime.UtcNow;
            var applyRun = BuildApplyRun(runId, dryRun, request, actorId, now);
            context.FormulaMigrationRuns.Add(applyRun);
            await context.SaveChangesAsync(cancellationToken);
            applyRun.Status = FormulaMigrationRunStatus.Running;
            await context.SaveChangesAsync(cancellationToken);

            var writer = new FormulaMigrationApplyWriter(context);
            var written = await writer.WriteAsync(
                applyRun, dryRun, package, cancellationToken);
            await AddReconciliationAsync(
                applyRun, dryRun, package, written, cancellationToken);
            applyRun.Status = FormulaMigrationRunStatus.Completed;
            applyRun.CompletedAt = DateTime.UtcNow;
            applyRun.TotalCount = dryRun.Items.Count;
            applyRun.SucceededCount = dryRun.Items.Count;
            applyRun.FailedCount = 0;
            await context.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
            return new FormulaMigrationApplyReceipt(runId, false,
                written.DefinitionsCreated, written.RevisionsCreated,
                written.QuestionLinksCreated, written.KeyMappingsCreated);
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

    private async Task<FormulaMigrationRun> LoadDryRunAsync(Guid id,
        CancellationToken cancellationToken) =>
        await context.FormulaMigrationRuns.AsNoTracking()
            .Include(item => item.Items)
            .ThenInclude(item => item.LegacyFormulaArtifact)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new KeyNotFoundException("The formula migration dry run was not found.");

    private async Task ValidateUsersAsync(FormulaMigrationApplyRequest request,
        Guid actorId, CancellationToken cancellationToken)
    {
        var ids = request.Targets.SelectMany(item =>
                new[] { item.ReviewedById, item.ApprovedById })
            .Concat(request.DecisionEvidence.Select(item => item.ReviewedById))
            .Append(actorId).Distinct().ToList();
        var active = await context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(item => ids.Contains(item.Id) && !item.IsDisabled && item.DeletedAt == null)
            .Select(item => item.Id).ToListAsync(cancellationToken);
        if (active.Count != ids.Count)
            throw new InvalidOperationException(
                "Every importer, reviewer, and approver must be an active user.");
        if (request.DecisionEvidence.Any(item => item.ReviewedById == actorId))
            throw new InvalidOperationException(
                "The importer cannot review migration decision evidence.");
    }

    private async Task ValidateLiveSourceAsync(FormulaMigrationRun dryRun,
        CancellationToken cancellationToken)
    {
        var request = FormulaMigrationApplyEvidence.RebuildDryRunRequest(dryRun);
        var report = await inventory.DryRunAsync(request, cancellationToken);
        if (!report.CanApply || report.SourceFingerprint != dryRun.SourceFingerprint ||
            report.Items.Count != dryRun.Items.Count)
            throw new InvalidOperationException(
                "The legacy formula source changed after the approved dry run.");
    }

    private async Task AddReconciliationAsync(FormulaMigrationRun applyRun,
        FormulaMigrationRun dryRun, ValidatedFormulaApplyPackage package,
        FormulaMigrationWriteResult written,
        CancellationToken cancellationToken)
    {
        await ValidateLiveSourceAsync(dryRun, cancellationToken);
        var desired = dryRun.Items.Where(item =>
                item.MigrationClass != FormulaMigrationClass.Unrecoverable)
            .Select(item => new
            {
                item.LegacyFormulaArtifact.QuestionId,
                package.TargetsByHash[item.AfterHash!].DefinitionId
            }).Distinct().ToList();
        var questionIds = desired.Select(item => item.QuestionId).Distinct().ToList();
        var definitionIds = desired.Select(item => item.DefinitionId).Distinct().ToList();
        var actual = await context.QuestionFormulaDefinitions.AsNoTracking()
            .Where(item => questionIds.Contains(item.QuestionId) &&
                definitionIds.Contains(item.FormulaDefinitionId))
            .Select(item => new { item.QuestionId, item.FormulaDefinitionId })
            .ToListAsync(cancellationToken);
        var actualLinks = desired.Count(link => actual.Any(item =>
            item.QuestionId == link.QuestionId &&
            item.FormulaDefinitionId == link.DefinitionId));
        var controls = FormulaMigrationApplyEvidence.BuildApplyControls(
            applyRun.Id, dryRun, desired.Count, actualLinks, written);
        if (controls.Any(item => !item.Passed))
            throw new InvalidOperationException("Formula apply reconciliation failed.");
        context.FormulaReconciliationResults.AddRange(controls);
    }

    private static FormulaMigrationRun BuildApplyRun(Guid id, FormulaMigrationRun dryRun,
        FormulaMigrationApplyRequest request, Guid actorId, DateTime now) => new()
    {
        Id = id,
        ReleaseId = dryRun.ReleaseId,
        SourceFingerprint = dryRun.SourceFingerprint,
        CorpusChecksum = dryRun.CorpusChecksum,
        CodeVersion = dryRun.CodeVersion,
        ApplyManifestHash = request.ManifestHash,
        SignedReportHash = request.SignedReportHash,
        Mode = FormulaMigrationRunMode.Apply,
        Status = FormulaMigrationRunStatus.Pending,
        InitiatedById = actorId,
        StartedAt = now,
        SignedReportLocation = request.SignedReportLocation
    };

    private static void ValidateExistingApply(FormulaMigrationRun existing,
        FormulaMigrationApplyRequest request, FormulaMigrationRun dryRun)
    {
        if (existing.Status != FormulaMigrationRunStatus.Completed ||
            existing.ApplyManifestHash != request.ManifestHash ||
            existing.SignedReportHash != request.SignedReportHash ||
            existing.SignedReportLocation != request.SignedReportLocation ||
            existing.CorpusChecksum != dryRun.CorpusChecksum ||
            existing.CodeVersion != dryRun.CodeVersion ||
            existing.TotalCount != dryRun.Items.Count || existing.FailedCount != 0)
            throw new InvalidOperationException(
                "An existing apply run is incomplete or has different provenance.");
    }
}
