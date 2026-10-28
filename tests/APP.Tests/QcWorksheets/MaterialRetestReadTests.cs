using APP.Services.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class MaterialRetestReadTests
{
    [Fact]
    public async Task List_and_detail_preserve_batch_scope_fields_and_event_history_without_tracking()
    {
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new NoCurrentUser());
        await VerifyReads(context);
    }

    [LocalMaterialRetestReadFact]
    public async Task PostgreSQL_executes_list_and_detail_with_the_production_auto_include_model()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_RETEST_READ_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing disposable fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost") || parsed.Database != "oryx_material_retest_test")
            throw new InvalidOperationException("Only the dedicated local disposable material retest fixture is permitted.");
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options, new NoCurrentUser());
        await context.Database.EnsureCreatedAsync();
        await VerifyReads(context);
    }

    private static async Task VerifyReads(ApplicationDbContext context)
    {
        var material = new Material { Id = Guid.NewGuid(), Code = $"MAT-{Guid.NewGuid():N}", Name = "Read fixture", Kind = MaterialKind.Raw };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "pc" };
        var first = new MaterialBatch { Id = Guid.NewGuid(), Material = material, UoM = unit, BatchNumber = "BATCH-1", Status = BatchStatus.Frozen };
        var second = new MaterialBatch { Id = Guid.NewGuid(), Material = material, UoM = unit, BatchNumber = "BATCH-2", Status = BatchStatus.Frozen };
        var now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        var actor = Guid.NewGuid();
        var older = new MaterialRetestRequest { Id = Guid.NewGuid(), MaterialBatch = first, SourceKey = $"read-{Guid.NewGuid():N}",
            Reason = "Manual review", Origin = MaterialRetestOrigin.Manual, Status = MaterialRetestStatus.SampleTaken,
            Approved = true, OriginalExpiryDate = now.AddDays(-1), ProposedExpiryDate = now.AddDays(90),
            SampleTakenAt = now, SampleQuantity = 0.125m, SampledContainers = 2, AcknowledgedAt = now.AddMinutes(1),
            Events = [new() { Id = Guid.NewGuid(), Action = "SampleTaken", Detail = "Recorded sample", ActorId = actor, OccurredAt = now.AddMinutes(2) },
                new() { Id = Guid.NewGuid(), Action = "Requested", Detail = "Manual review", ActorId = actor, OccurredAt = now }] };
        var newer = new MaterialRetestRequest { Id = Guid.NewGuid(), MaterialBatch = second, SourceKey = $"read-{Guid.NewGuid():N}",
            Reason = "Scheduled review", Origin = MaterialRetestOrigin.Scheduled, ScheduledFor = now, Status = MaterialRetestStatus.PendingApproval };
        context.AddRange(older, newer);
        await context.SaveChangesAsync();
        // SaveChanges owns creation audit times. Set deterministic order directly in the persisted fixture.
        older.CreatedAt = now;
        newer.CreatedAt = now.AddMinutes(1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new MaterialRetestService(context, null!, null!);
        var all = await service.ListAsync(null);
        Assert.True(all.IsSuccess);
        Assert.Equal(new[] { newer.Id, older.Id }, all.Value.Select(row => row.Id));
        var filtered = await service.ListAsync(first.Id);
        var row = Assert.Single(filtered.Value);
        Assert.Equal(older.Id, row.Id);
        Assert.Equal(first.Id, row.MaterialBatchId);
        Assert.Equal("BATCH-1", row.BatchNumber);
        Assert.Equal(MaterialKind.Raw, row.MaterialKind);
        Assert.True(row.Approved);
        Assert.Equal(MaterialRetestStatus.SampleTaken, row.Status);
        Assert.Equal(MaterialRetestOrigin.Manual, row.Origin);
        Assert.Equal("Manual review", row.Reason);
        Assert.Equal(now.AddDays(-1), row.OriginalExpiryDate);
        Assert.Equal(now.AddDays(90), row.ProposedExpiryDate);
        Assert.Equal(now, row.SampleTakenAt);
        Assert.Equal(0.125m, row.SampleQuantity);
        Assert.Equal(2, row.SampledContainers);
        Assert.Equal(now.AddMinutes(1), row.AcknowledgedAt);
        Assert.Null(row.TestRequestId);
        Assert.Null(row.CoaId);
        Assert.Null(row.CompletedAt);
        Assert.Equal(new[] { "Requested", "SampleTaken" }, row.Events.Select(entry => entry.Action));
        Assert.All(row.Events, entry => Assert.Equal(actor, entry.ActorId));
        Assert.Equal("Recorded sample", row.Events[1].Detail);
        var detail = await service.GetAsync(older.Id);
        Assert.True(detail.IsSuccess);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(row), System.Text.Json.JsonSerializer.Serialize(detail.Value));
        Assert.Equal(now, (await service.GetAsync(newer.Id)).Value.ScheduledFor);
        Assert.Empty((await service.ListAsync(Guid.NewGuid())).Value);
        Assert.Equal("MaterialRetest.NotFound", (await service.GetAsync(Guid.NewGuid())).Error.Code);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(BatchStatus.Frozen, (await context.MaterialBatches.IgnoreAutoIncludes().AsNoTracking().SingleAsync(batch => batch.Id == first.Id)).Status);
    }
}

public sealed class LocalMaterialRetestReadFactAttribute : FactAttribute
{
    public LocalMaterialRetestReadFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_RETEST_READ_TEST_CONNECTION")))
            Skip = "Set ORYX_RETEST_READ_TEST_CONNECTION to the dedicated disposable local PostgreSQL fixture.";
    }
}
