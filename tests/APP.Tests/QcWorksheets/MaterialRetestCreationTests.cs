using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class MaterialRetestCreationTests
{
    [Fact]
    public async Task A_retest_is_tested_on_the_material_test_never_on_a_qc_v2_round()
    {
        using var harness = new QcWorksheetTestContext();
        var service = new MaterialRetestService(harness.Db, harness.ApprovalRepository, harness.SignatureService, harness.TestRequests);
        var batch = await harness.SeedMaterialBatch("RETEST-CREATE", BatchStatus.Available);
        batch.TotalQuantity = 10;
        await harness.Db.SaveChangesAsync();
        await MaterialRetestAcceptanceTests.SeedArd(harness, batch);
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
            { ArNumber = "RETEST-AR-1", SampleQuantity = 1, SampledContainers = 1, ProposedExpiryDate = DateTime.UtcNow.AddDays(90) }, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.AcknowledgeAsync(requested.Value, Guid.NewGuid())).IsSuccess);
        // Acknowledging hands the batch to the material test, so no QC V2 test round can be created or linked.
        Assert.False((await service.CreateTestingAsync(requested.Value, body, Guid.NewGuid())).IsSuccess);
        Assert.Empty(await harness.Db.QcTestRequests.ToListAsync());
        Assert.Equal(MaterialRetestStatus.Acknowledged, (await harness.Db.MaterialRetestRequests.SingleAsync()).Status);
    }
}
