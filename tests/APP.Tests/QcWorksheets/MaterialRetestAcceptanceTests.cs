using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class MaterialRetestAcceptanceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Issued_coa_applies_expiry_only_when_compliant(bool complies)
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch($"RETEST-COA-{complies}", BatchStatus.Available);
        batch.TotalQuantity = 10;
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(-1);
        await harness.Db.SaveChangesAsync();
        var originalExpiry = batch.ExpiryDate;
        var proposedExpiry = DateTime.UtcNow.Date.AddDays(90);
        var requested = await service.RequestAsync(batch.Id, "Expiry review", Guid.NewGuid(), false);
        Assert.True(requested.IsSuccess, requested.Error?.Description);
        Assert.True((await service.RecordSampleAsync(requested.Value, new RecordMaterialRetestSample
        {
            SampleQuantity = 1, SampledContainers = 1, ProposedExpiryDate = proposedExpiry
        }, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.AcknowledgeAsync(requested.Value, Guid.NewGuid())).IsSuccess);

        var testRequest = new TestRequest
        {
            Id = Guid.NewGuid(), Type = TestRequestType.RawMaterial, ArNumber = $"ATR-{Guid.NewGuid():N}",
            Subjects = [new TestRequestSubject { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, SubjectRef = batch.BatchNumber }]
        };
        harness.Db.QcTestRequests.Add(testRequest);
        await harness.Db.SaveChangesAsync();
        Assert.True((await service.LinkTestRequestAsync(requested.Value, testRequest.Id, Guid.NewGuid())).IsSuccess);
        var coa = new Coa
        {
            Id = Guid.NewGuid(), TestRequestId = testRequest.Id,
            CertificateCode = $"COA-RETEST-{complies}", OverallComplies = complies
        };
        harness.Db.Coas.Add(coa);
        await harness.Db.SaveChangesAsync();

        var issued = await harness.Coas.Issue(coa.Id, Guid.NewGuid());
        Assert.True(issued.IsSuccess, issued.Error?.Description);
        Assert.Equal(CoaStatus.Issued, coa.Status);
        Assert.Equal(complies ? proposedExpiry : originalExpiry, batch.ExpiryDate);
        Assert.Equal(complies ? proposedExpiry : null, coa.ExpiryDate);
        Assert.Equal(complies ? BatchStatus.Available : BatchStatus.Frozen, batch.Status);
        Assert.Equal(complies ? MaterialRetestStatus.Completed : MaterialRetestStatus.Rejected,
            (await harness.Db.MaterialRetestRequests.SingleAsync()).Status);
    }

    [Fact]
    public async Task Due_date_creates_one_autoapproved_request_and_preserves_schedule()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch("RETEST-DUE", BatchStatus.Available);
        batch.RetestDate = DateTime.UtcNow.Date.AddDays(1);
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(30);
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
    public async Task Configured_approval_gates_sampling_and_expiry_remains_unchanged()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.MaterialRetest);
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService);
        var batch = await harness.SeedMaterialBatch("RETEST-MANUAL", BatchStatus.Available);
        batch.TotalQuantity = 10;
        batch.ExpiryDate = DateTime.UtcNow.Date.AddDays(-1);
        await harness.Db.SaveChangesAsync();

        var created = await service.RequestAsync(batch.Id, "Expiry investigation", harness.Approver.Id, false);
        Assert.True(created.IsSuccess, created.Error?.Description);
        var sample = new RecordMaterialRetestSample
        {
            SampleQuantity = 1, SampledContainers = 1,
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
