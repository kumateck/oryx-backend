using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class MaterialRetestCreationTests
{
    [Fact]
    public async Task Creation_and_linking_pin_the_version_and_repeated_confirmation_creates_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService, harness.TestRequests);
        var batch = await harness.SeedMaterialBatch("RETEST-CREATE", BatchStatus.Available);
        batch.TotalQuantity = 10;
        await harness.Db.SaveChangesAsync();
        var requested = await service.RequestAsync(batch.Id, "Expiry investigation", Guid.NewGuid(), false);
        Assert.True(requested.IsSuccess, requested.Error?.Description);
        var template = await harness.SeedEffectiveTemplate("RETEST-WS", WorksheetCategory.Chemical, "assay");
        var spec = await harness.SeedEffectiveSpecification(SpecificationAppliesTo.RawMaterial, (template, SpecificationAnalysisType.Chemical));
        var body = new CreateTestRequestRequest { Type = TestRequestType.RawMaterial, SpecificationId = spec.Id,
            ArNumber = "RETEST-AR", ScheduleOrigin = TestRequestScheduleOrigin.Unscheduled, UnscheduledReason = "Governed expiry retest",
            Subjects = [new CreateTestRequestSubjectRequest { MaterialBatchId = batch.Id, SubjectRef = batch.BatchNumber }] };
        Assert.False((await service.CreateTestingAsync(requested.Value, body, Guid.NewGuid())).IsSuccess);
        Assert.Empty(await harness.Db.QcTestRequests.ToListAsync());
        Assert.True((await service.RecordSampleAsync(requested.Value, new RecordMaterialRetestSample
            { SampleQuantity = 1, SampledContainers = 1, ProposedExpiryDate = DateTime.UtcNow.AddDays(90) }, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.AcknowledgeAsync(requested.Value, Guid.NewGuid())).IsSuccess);
        var created = await service.CreateTestingAsync(requested.Value, body, Guid.NewGuid());
        Assert.True(created.IsSuccess, created.Error?.Description);
        var retest = await harness.Db.MaterialRetestRequests.SingleAsync();
        var round = await harness.Db.QcTestRequests.SingleAsync();
        Assert.Equal(round.Id, retest.TestRequestId);
        Assert.Equal(MaterialRetestStatus.Testing, retest.Status);
        Assert.Equal(spec.Id, round.SpecificationId);
        Assert.Equal(template.Id, (await harness.Db.QcWorksheetInstances.SingleAsync()).WorksheetTemplateId);
        Assert.False((await service.CreateTestingAsync(requested.Value, body, Guid.NewGuid())).IsSuccess);
        Assert.Single(await harness.Db.QcTestRequests.ToListAsync());
        Assert.Single(await harness.Db.MaterialRetestEvents.Where(item => item.Action == "TestingLinked").ToListAsync());
    }
}
