using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class MaterialRetestAcceptanceTests
{
    /// <summary>The verified chemical analysis method (ARD) a material needs before its sample can be tested.</summary>
    internal static async Task SeedArd(QcWorksheetTestContext harness, MaterialBatch batch)
    {
        var stp = new DOMAIN.Entities.MaterialStandardTestProcedures.MaterialStandardTestProcedure { Id = Guid.NewGuid(), MaterialId = batch.MaterialId, StpNumber = "STP-RT" };
        var form = new DOMAIN.Entities.Forms.Form { Id = Guid.NewGuid(), Name = "Retest worksheet" };
        harness.Db.AddRange(stp, form, new DOMAIN.Entities.MaterialARD.MaterialAnalyticalRawData { Id = Guid.NewGuid(), StpId = stp.Id,
            FormId = form.Id, SpecNumber = "SPEC-RT", AnalysisType = DOMAIN.Entities.QualityRoutines.AnalysisType.Chemical, IsVerified = true });
        await harness.Db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Acknowledged_retest_runs_on_the_material_test_and_its_outcome_extends_expiry_only_when_compliant(bool complies)
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch($"RETEST-{complies}", BatchStatus.Available);
        batch.TotalQuantity = 10;
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(-1);
        await harness.Db.SaveChangesAsync();
        await SeedArd(harness, batch);
        var originalExpiry = batch.ExpiryDate;
        var proposedExpiry = DateTime.UtcNow.Date.AddDays(90);
        var requested = await service.RequestAsync(batch.Id, "Expiry review", Guid.NewGuid(), false);
        Assert.True(requested.IsSuccess, requested.Error?.Description);
        var sample = new RecordMaterialRetestSample { SampleQuantity = 1, SampledContainers = 1, ProposedExpiryDate = proposedExpiry };
        // QA gives the sample its AR number, as on a goods receipt sample.
        Assert.Equal("MaterialRetest.ArNumber", (await service.RecordSampleAsync(requested.Value, sample, Guid.NewGuid())).Error.Code);
        sample.ArNumber = "QCD/RM/26/900";
        Assert.True((await service.RecordSampleAsync(requested.Value, sample, Guid.NewGuid())).IsSuccess);
        var qc = Guid.NewGuid();
        Assert.True((await service.AcknowledgeAsync(requested.Value, qc)).IsSuccess);

        // Like an analytical test request: the retest records the test's progress, the batch stays held, and the answers
        // bind to a sample taken for this retest (no goods receipt note).
        var sampling = await harness.Db.MaterialSamplings.SingleAsync(item => item.MaterialBatchId == batch.Id);
        var retest = await harness.Db.MaterialRetestRequests.SingleAsync();
        Assert.Equal((retest.Id, "QCD/RM/26/900", 1m, (Guid?)null), (sampling.MaterialRetestRequestId!.Value, sampling.ArNumber, sampling.SampleQuantity, sampling.GrnId));
        Assert.Equal(BatchStatus.Frozen, batch.Status);
        Assert.Equal(MaterialRetestStatus.Acknowledged, retest.Status);
        var read = (await service.GetAsync(retest.Id)).Value;
        Assert.Equal((sampling.Id, "QCD/RM/26/900", BatchStatus.Frozen), (read.MaterialSamplingId!.Value, read.ArNumber, read.BatchStatus));
        foreach (var step in new[] { MaterialRetestStatus.Assigned, MaterialRetestStatus.Testing, MaterialRetestStatus.TestTaken, MaterialRetestStatus.Checked })
        {
            Assert.True(await MaterialRetestTesting.AdvanceAsync(harness.Db, batch.Id, step, qc, step.ToString()));
            await harness.Db.SaveChangesAsync();
            Assert.Equal((step, BatchStatus.Frozen), (retest.Status, batch.Status));
        }

        // The material test's outcome closes the retest.
        if (!complies) batch.Status = BatchStatus.Rejected;
        Assert.True(await MaterialRetestCompletion.ApplyAsync(harness.Db, batch, complies, qc));
        await harness.Db.SaveChangesAsync();
        Assert.Equal(complies ? proposedExpiry : originalExpiry, batch.ExpiryDate);
        Assert.Equal(complies ? BatchStatus.Available : BatchStatus.Rejected, batch.Status);
        Assert.Equal(complies ? MaterialRetestStatus.Completed : MaterialRetestStatus.Rejected, retest.Status);
        // Nothing closes twice.
        Assert.False(await MaterialRetestCompletion.ApplyAsync(harness.Db, batch, complies, qc));
    }

    [Fact]
    public async Task Due_date_creates_one_autoapproved_request_and_preserves_schedule()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch("RETEST-DUE", BatchStatus.Available);
        batch.RetestDate = DateTime.UtcNow.Date.AddDays(1);
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(-30);
        await harness.Db.SaveChangesAsync();

        Assert.Equal(0, await service.ScanDueAsync(DateTime.UtcNow.Date, CancellationToken.None));
        Assert.Equal(1, await service.ScanDueAsync(batch.RetestDate.Value, CancellationToken.None));
        Assert.Equal(0, await service.ScanDueAsync(batch.RetestDate.Value, CancellationToken.None));

        var request = await harness.Db.MaterialRetestRequests.SingleAsync();
        Assert.Equal(MaterialRetestOrigin.Scheduled, request.Origin);
        Assert.Equal(MaterialRetestStatus.Authorized, request.Status);
        Assert.True(request.Approved);
        Assert.Equal(batch.RetestDate, request.ScheduledFor);
        Assert.Equal(BatchStatus.Frozen, batch.Status);
        Assert.Contains(await harness.Db.MaterialRetestEvents.ToListAsync(), item => item.Action == "AutoApproved");
    }

    [Fact]
    public async Task Active_expiry_prevents_an_earlier_retest_schedule_from_freezing_raw_stock()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch("ACTIVE-EXPIRY", BatchStatus.Available);
        batch.RetestDate = DateTime.UtcNow.AddDays(-1);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(30);
        await harness.Db.SaveChangesAsync();
        Assert.Equal(0, await service.ScanDueAsync(DateTime.UtcNow, CancellationToken.None));
        Assert.Equal(BatchStatus.Available, batch.Status);
        Assert.Empty(await harness.Db.MaterialRetestRequests.ToListAsync());
    }

    [Fact]
    public async Task Configured_approval_gates_sampling_and_expiry_remains_unchanged()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.MaterialRetest);
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch("RETEST-MANUAL", BatchStatus.Available);
        batch.TotalQuantity = 10;
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(-1);
        await harness.Db.SaveChangesAsync();

        await SeedArd(harness, batch);
        var created = await service.RequestAsync(batch.Id, "Expiry investigation", harness.Approver.Id, false);
        Assert.True(created.IsSuccess, created.Error?.Description);
        var sample = new RecordMaterialRetestSample
        {
            ArNumber = "QCD/RM/26/901", SampleQuantity = 1, SampledContainers = 1,
            ProposedExpiryDate = DateTime.UtcNow.Date.AddDays(90)
        };
        Assert.False((await service.RecordSampleAsync(created.Value, sample, harness.Approver.Id)).IsSuccess);
        Assert.Equal(MaterialRetestStatus.PendingApproval,
            (await harness.Db.MaterialRetestRequests.SingleAsync()).Status);

        var approved = await service.DecideAsync(created.Value, harness.Approver.Id, [],
            new MaterialRetestDecision
            {
                Password = QcWorksheetTestContext.CorrectPassword,
                Comments = "Sampling authorized"
            }, true);
        Assert.True(approved.IsSuccess, approved.Error?.Description);
        Assert.True((await service.RecordSampleAsync(created.Value, sample, harness.Approver.Id)).IsSuccess);
        Assert.True((await service.AcknowledgeAsync(created.Value, harness.Approver.Id)).IsSuccess);
        Assert.Equal(MaterialRetestStatus.Acknowledged,
            (await harness.Db.MaterialRetestRequests.SingleAsync()).Status);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(-1), batch.ExpiryDate);
    }
}
