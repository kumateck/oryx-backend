using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Acceptance criteria 1, 2, 3 and 6 from the Milestone 1 brief, each named for the rule it
/// proves.
/// </summary>
public class StpLifecycleAcceptanceTests
{
    private static CreateStpRequest NewStpRequest(string code = "QCD/STP/RM/001") => new()
    {
        Code = code,
        Name = "Ascorbic Acid",
        Area = "Raw Materials",
        Purpose = "To describe the procedure",
        Scope = "Applies to raw materials",
        Responsibility = "QC Officer",
        Accountability = "QC Manager",
        Steps =
        [
            new CreateStpStepRequest { Order = 1, Instruction = "Weigh the sample." },
            new CreateStpStepRequest { Order = 2, Instruction = "Dissolve in water." }
        ]
    };

    /// <summary>
    /// Criterion 1 — version pinning / edit-triggers-versioning. An Effective document
    /// cannot be edited in place; creating version 2 leaves version 1 Effective, not
    /// Superseded.
    /// </summary>
    [Fact]
    public async Task Effective_stp_rejects_edits_and_new_version_leaves_v1_effective()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var created = await harness.Stps.CreateStp(NewStpRequest(), userId);
        Assert.True(created.IsSuccess);
        var v1Id = created.Value.Id;

        Assert.True((await harness.Stps.SubmitForReview(v1Id, userId)).IsSuccess);

        var approved = await harness.Stps.Approve(
            v1Id,
            new QcApprovalRequest { Password = QcWorksheetTestContext.CorrectPassword, Comments = "Approved by" },
            userId,
            []);
        Assert.True(approved.IsSuccess);

        var effective = await harness.Stps.MakeEffective(v1Id, userId);
        Assert.True(effective.IsSuccess);
        Assert.Equal(QcDocumentStatus.Effective, effective.Value.Status);

        // Editing an Effective record is refused server-side — the read-only UI is never
        // trusted on its own.
        var edit = await harness.Stps.UpdateStp(v1Id, new UpdateStpRequest
        {
            Code = "QCD/STP/RM/001",
            Name = "Renamed while effective",
            Steps = []
        }, userId);

        Assert.False(edit.IsSuccess);
        Assert.Equal("QcWorksheet.NotEditable", edit.Error.Code);

        var v2 = await harness.Stps.CreateNewVersion(v1Id, userId);
        Assert.True(v2.IsSuccess);
        Assert.Equal(2, v2.Value.Version);
        Assert.Equal(QcDocumentStatus.Draft, v2.Value.Status);
        Assert.Equal(v1Id, v2.Value.SupersedesId);

        // Version 1 stays in force until version 2 itself becomes Effective.
        var v1 = await harness.Stps.GetStp(v1Id);
        Assert.Equal(QcDocumentStatus.Effective, v1.Value.Status);
        Assert.Equal(1, v1.Value.Version);

        // And the content was carried over, not lost.
        Assert.Equal(2, v2.Value.Steps.Count);
    }

    /// <summary>
    /// Criterion 1 (second half) — version 1 only retires when version 2 becomes Effective.
    /// </summary>
    [Fact]
    public async Task Making_v2_effective_supersedes_v1()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var v1Id = (await harness.Stps.CreateStp(NewStpRequest(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(v1Id, userId);
        await harness.Stps.Approve(v1Id, Sign(), userId, []);
        await harness.Stps.MakeEffective(v1Id, userId);

        var v2Id = (await harness.Stps.CreateNewVersion(v1Id, userId)).Value.Id;
        await harness.Stps.SubmitForReview(v2Id, userId);
        await harness.Stps.Approve(v2Id, Sign(), userId, []);
        var v2Effective = await harness.Stps.MakeEffective(v2Id, userId);

        Assert.True(v2Effective.IsSuccess);
        Assert.Equal(QcDocumentStatus.Effective, v2Effective.Value.Status);

        var v1 = await harness.Stps.GetStp(v1Id);
        Assert.Equal(QcDocumentStatus.Superseded, v1.Value.Status);
    }

    /// <summary>Criterion 2 — draft edits are free; no version is spawned per edit.</summary>
    [Fact]
    public async Task Draft_edits_do_not_create_new_versions()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var id = (await harness.Stps.CreateStp(NewStpRequest(), userId)).Value.Id;

        for (var i = 1; i <= 3; i++)
        {
            var edit = await harness.Stps.UpdateStp(id, new UpdateStpRequest
            {
                Code = "QCD/STP/RM/001",
                Name = $"Edited {i}",
                Steps = [new CreateStpStepRequest { Order = 1, Instruction = $"Step {i}" }]
            }, userId);

            Assert.True(edit.IsSuccess);
            Assert.Equal(1, edit.Value.Version);
            Assert.Equal(QcDocumentStatus.Draft, edit.Value.Status);
        }

        Assert.Equal(1, await harness.Db.QcStandardTestProcedures.CountAsync());
    }

    /// <summary>
    /// Criterion 3 — an edit while UnderReview resets the document to Draft, so a reviewer
    /// is never evaluating a moving target.
    /// </summary>
    [Fact]
    public async Task Editing_under_review_stp_resets_it_to_draft()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStpRequest(), userId)).Value.Id;

        var submitted = await harness.Stps.SubmitForReview(id, userId);
        Assert.True(submitted.IsSuccess);
        Assert.Equal(QcDocumentStatus.UnderReview, submitted.Value.Status);

        var edited = await harness.Stps.UpdateStp(id, new UpdateStpRequest
        {
            Code = "QCD/STP/RM/001",
            Name = "Changed mid-review",
            Steps = [new CreateStpStepRequest { Order = 1, Instruction = "Revised step" }]
        }, userId);

        Assert.True(edited.IsSuccess);
        Assert.Equal(QcDocumentStatus.Draft, edited.Value.Status);
        Assert.Equal(1, edited.Value.Version);
    }

    /// <summary>
    /// Criterion 6 — a step's cross-reference is a resolved structured link to the other
    /// STP, not plain text.
    /// </summary>
    [Fact]
    public async Task Stp_step_cross_reference_resolves_to_the_referenced_document()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var stpA = await harness.Stps.CreateStp(NewStpRequest("QCD/STP/RM/001"), userId);
        Assert.True(stpA.IsSuccess);

        var stpB = await harness.Stps.CreateStp(new CreateStpRequest
        {
            Code = "QCD/STP/RM/002",
            Name = "Ibuprofen",
            Steps =
            [
                new CreateStpStepRequest
                {
                    Order = 1,
                    Instruction = "Refer to the identification method.",
                    ReferencedStpId = stpA.Value.Id
                }
            ]
        }, userId);

        Assert.True(stpB.IsSuccess);

        var step = Assert.Single(stpB.Value.Steps);
        Assert.NotNull(step.ReferencedStp);
        Assert.Equal(stpA.Value.Id, step.ReferencedStp.Id);
        Assert.Equal("QCD/STP/RM/001", step.ReferencedStp.Code);
        Assert.Equal("Ascorbic Acid", step.ReferencedStp.Name);
    }

    /// <summary>A cross-reference to a document that does not exist is rejected.</summary>
    [Fact]
    public async Task Unknown_cross_reference_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var result = await harness.Stps.CreateStp(new CreateStpRequest
        {
            Code = "QCD/STP/RM/003",
            Name = "Broken reference",
            Steps =
            [
                new CreateStpStepRequest
                {
                    Order = 1,
                    Instruction = "Refer to nothing.",
                    ReferencedStpId = Guid.NewGuid()
                }
            ]
        }, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcStp.ReferencedStpNotFound", result.Error.Code);
    }

    /// <summary>create-new-version is only meaningful from Effective.</summary>
    [Fact]
    public async Task New_version_from_a_draft_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var id = (await harness.Stps.CreateStp(NewStpRequest(), userId)).Value.Id;
        var result = await harness.Stps.CreateNewVersion(id, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.NewVersionRequiresEffective", result.Error.Code);
    }

    private static QcApprovalRequest Sign() => new()
    {
        Password = QcWorksheetTestContext.CorrectPassword,
        Comments = "Approved by"
    };
}
