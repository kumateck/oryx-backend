using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets;

public class QcPendingApprovalCoverageTests
{
    [Fact]
    public async Task Submitted_worksheet_and_oos_case_have_review_detail_routes()
    {
        using var harness = new QcWorksheetTestContext();
        var approvalId = await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetInstance);
        var template = new WorksheetTemplate
        {
            Id = Guid.NewGuid(), Code = "QC/TMP/01", Name = "Assay", Version = 2,
        };
        var instance = new WorksheetInstance
        {
            Id = Guid.NewGuid(), WorksheetTemplateId = template.Id,
            WorksheetTemplate = template, WorksheetTemplateVersion = 2,
        };
        var oos = new OosCase
        {
            Id = Guid.NewGuid(), WorksheetInstanceId = instance.Id,
            WorksheetInstance = instance, FieldKey = "assay_result",
        };
        harness.Db.AddRange(template, instance, oos);
        harness.Db.QcApprovals.AddRange(
            new QcApproval
            {
                Id = Guid.NewGuid(), EntityType = QcApprovalEntityTypes.WorksheetInstance,
                EntityId = instance.Id, ApprovalId = approvalId,
                UserId = harness.Approver.Id, Order = 1,
                Status = ApprovalStatus.Pending, ActivatedAt = DateTime.UtcNow,
            },
            new QcApproval
            {
                Id = Guid.NewGuid(), EntityType = QcApprovalEntityTypes.OosCase,
                EntityId = oos.Id, ApprovalId = approvalId,
                UserId = harness.Approver.Id, Order = 1,
                Status = ApprovalStatus.Pending, ActivatedAt = DateTime.UtcNow,
            });
        await harness.Db.SaveChangesAsync();

        var pending = await harness.Approvals.GetPendingApprovals(harness.Approver.Id, []);

        Assert.True(pending.IsSuccess);
        Assert.Contains(pending.Value, item =>
            item.EntityId == instance.Id
            && item.ResourcePath == $"qc/worksheets/review/{instance.Id}");
        Assert.Contains(pending.Value, item =>
            item.EntityId == oos.Id
            && item.ResourcePath == $"qc/worksheets/oos-cases/{oos.Id}");
    }
}
