using APP.Services.QcWorksheets;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class ResponseReworkAcceptanceTests
{
    [Theory]
    [InlineData(false, ResponseRejectionOutcome.Rework)]
    [InlineData(true, ResponseRejectionOutcome.Rework)]
    [InlineData(false, ResponseRejectionOutcome.Disposed)]
    [InlineData(true, ResponseRejectionOutcome.Disposed)]
    public async Task Qa_decision_preserves_original_and_routes_batch(bool product,
        ResponseRejectionOutcome outcome)
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(nameof(Response));
        var form = new Form { Id = Guid.NewGuid(), Name = "QC result", CreatedAt = DateTime.UtcNow };
        var batch = product ? null : await harness.SeedMaterialBatch("REWORK-MATERIAL");
        var bmr = product ? await harness.SeedBatchManufacturingRecord("REWORK-PRODUCT") : null;
        var original = new Response
        {
            Id = Guid.NewGuid(), FormId = form.Id,
            MaterialBatchId = batch?.Id, BatchManufacturingRecordId = bmr?.Id,
            CheckedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            FormResponses = [new FormResponse
            {
                Id = Guid.NewGuid(), FormFieldId = Guid.NewGuid(), Value = "Original assay value"
            }]
        };
        harness.Db.Forms.Add(form);
        harness.Db.Responses.Add(original);
        await harness.Db.SaveChangesAsync();
        await harness.ApprovalRepository.CreateInitialApprovalsAsync(nameof(Response), original.Id);

        var rejected = await harness.ApprovalRepository.RejectItem(nameof(Response), original.Id,
            harness.Approver.Id, [], "QA requires repeat testing", outcome);

        Assert.True(rejected.IsSuccess, rejected.Error?.Description);
        Assert.True(original.Rejected);
        Assert.Equal(outcome, original.RejectionOutcome);
        if (product)
            Assert.Equal(outcome == ResponseRejectionOutcome.Rework
                ? BatchManufacturingStatus.Quarantine : BatchManufacturingStatus.Rejected, bmr!.Status);
        else
            Assert.Equal(outcome == ResponseRejectionOutcome.Rework
                ? BatchStatus.Frozen : BatchStatus.Rejected, batch!.Status);
        var tasks = await harness.Db.ResponseReworkTasks.ToListAsync();
        Assert.Equal(outcome == ResponseRejectionOutcome.Rework ? 1 : 0, tasks.Count);
        Assert.Single(await harness.Db.ApprovalActionLogs.Where(item => item.ModelId == original.Id).ToListAsync());
        var duplicate = await harness.ApprovalRepository.RejectItem(nameof(Response), original.Id,
            harness.Approver.Id, [], "Duplicate decision", outcome);
        Assert.False(duplicate.IsSuccess);
        Assert.Equal(tasks.Count, await harness.Db.ResponseReworkTasks.CountAsync());

        if (outcome == ResponseRejectionOutcome.Rework)
        {
            var service = new ResponseReworkService(harness.Db);
            var started = await service.StartAsync(tasks[0].Id, harness.Approver.Id);
            Assert.True(started.IsSuccess, started.Error?.Description);
            var next = await harness.Db.Responses.SingleAsync(item => item.Id == started.Value);
            Assert.NotEqual(original.Id, next.Id);
            Assert.Equal(original.FormId, next.FormId);
            Assert.Equal(1, next.ReworkSequence);
            Assert.False(next.Rejected);
            Assert.Null(next.CheckedAt);
            Assert.Empty(next.FormResponses);
            Assert.Equal("Original assay value", original.FormResponses.Single().Value);
            Assert.Equal(original.Id, tasks[0].RootResponseId);
            Assert.Equal(original.Id, tasks[0].SourceResponseId);
            Assert.False((await service.StartAsync(tasks[0].Id, harness.Approver.Id)).IsSuccess);
        }
    }
}
