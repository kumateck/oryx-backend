using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class MigrationGuardTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class FormulaMigrationGuardTests
{
    [Fact]
    public async Task CompletedDryRun_CannotBeChanged()
    {
        await using var context = CreateContext();
        var run = CreateRun();
        context.FormulaMigrationRuns.Add(run);
        await context.SaveChangesAsync();

        run.FailedCount = 2;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("immutable", error.Message);
    }

    [Fact]
    public async Task MigrationItem_CannotBeChangedAfterInsertion()
    {
        await using var context = CreateContext();
        var item = new FormulaMigrationItem
        {
            Id = Guid.NewGuid(),
            FormulaMigrationRunId = Guid.NewGuid(),
            LegacyFormulaArtifactId = Guid.NewGuid(),
            SourceHash = new string('a', 64),
            PlacementKey = "question:test",
            Action = "Unclassified",
            ApprovalReference = string.Empty,
            Status = FormulaMigrationItemStatus.Failed,
            Error = "MISSING_DECISION",
            BeforeHash = new string('a', 64)
        };
        context.FormulaMigrationItems.Add(item);
        await context.SaveChangesAsync();

        item.Error = "changed";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("append-only", error.Message);
    }

    [Fact]
    public async Task RunningApply_ReportLocationCannotBeChanged()
    {
        await using var context = CreateContext();
        var run = CreateRun();
        run.Mode = FormulaMigrationRunMode.Apply;
        run.Status = FormulaMigrationRunStatus.Running;
        run.CompletedAt = null;
        run.ApplyManifestHash = new string('e', 64);
        run.SignedReportHash = new string('d', 64);
        run.SignedReportLocation = "validated://original";
        context.FormulaMigrationRuns.Add(run);
        await context.SaveChangesAsync();

        run.SignedReportLocation = "validated://changed";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync());
        Assert.Contains("provenance", error.Message);
    }

    private static FormulaMigrationRun CreateRun() => new()
    {
        Id = Guid.NewGuid(),
        ReleaseId = "formula-v1",
        SourceFingerprint = new string('b', 64),
        CorpusChecksum = new string('c', 64),
        CodeVersion = "test",
        Mode = FormulaMigrationRunMode.DryRun,
        Status = FormulaMigrationRunStatus.Completed,
        InitiatedById = Guid.NewGuid(),
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
        TotalCount = 1,
        SucceededCount = 0,
        FailedCount = 1,
        SignedReportLocation = string.Empty
    };

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new MigrationGuardTestUser()
    );
}
