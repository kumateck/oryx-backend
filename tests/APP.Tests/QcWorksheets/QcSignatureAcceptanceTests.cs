using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Acceptance criterion 5 — e-signature captured on approve — plus the locked governance
/// decision that QC never takes the approval engine's silent auto-approval fallback.
/// </summary>
public class QcSignatureAcceptanceTests
{
    private static CreateStpRequest NewStp() => new()
    {
        Code = "QCD/STP/RM/004",
        Name = "Amoxicillin Trihydrate",
        Steps = [new CreateStpStepRequest { Order = 1, Instruction = "Weigh." }]
    };

    /// <summary>
    /// Criterion 5 — approving records a QcApproval row carrying EntityType, EntityId,
    /// ApprovedById, ApprovalTime and ReauthConfirmedAt.
    /// </summary>
    [Fact]
    public async Task Approving_records_a_qc_approval_row_with_reauth_timestamp()
    {
        using var harness = new QcWorksheetTestContext();
        var approvalId = await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        var before = DateTime.UtcNow;
        var approved = await harness.Stps.Approve(id, new QcApprovalRequest
        {
            Password = QcWorksheetTestContext.CorrectPassword,
            Comments = "Approved by"
        }, userId, []);

        Assert.True(approved.IsSuccess);
        Assert.Equal(QcDocumentStatus.Approved, approved.Value.Status);
        Assert.True(approved.Value.Approved);

        var row = await harness.Db.QcApprovals.SingleAsync(item => item.EntityId == id);

        Assert.Equal(QcApprovalEntityTypes.StandardTestProcedure, row.EntityType);
        Assert.Equal(id, row.EntityId);
        Assert.Equal(approvalId, row.ApprovalId);
        Assert.Equal(ApprovalStatus.Approved, row.Status);
        Assert.Equal(userId, row.ApprovedById);
        Assert.NotNull(row.ApprovalTime);
        Assert.NotNull(row.ReauthConfirmedAt);
        Assert.True(row.ReauthConfirmedAt >= before);
        Assert.Equal("Approved by", row.Comments);
    }

    /// <summary>
    /// Criterion 5 — the endpoint rejects a wrong credential even though the session is
    /// otherwise valid, and nothing is signed as a result.
    /// </summary>
    [Fact]
    public async Task Wrong_password_is_rejected_and_signs_nothing()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        var result = await harness.Stps.Approve(id, new QcApprovalRequest
        {
            Password = "not-my-password",
            Comments = "Approved by"
        }, userId, []);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.ReauthenticationFailed", result.Error.Code);

        var stp = await harness.Db.QcStandardTestProcedures.SingleAsync(item => item.Id == id);
        Assert.Equal(QcDocumentStatus.UnderReview, stp.Status);
        Assert.False(stp.Approved);

        var stage = await harness.Db.QcApprovals.SingleAsync(item => item.EntityId == id);
        Assert.Equal(ApprovalStatus.Pending, stage.Status);
        Assert.Null(stage.ReauthConfirmedAt);
        Assert.Null(stage.ApprovedById);
    }

    /// <summary>Criterion 5 — a missing credential is refused too.</summary>
    [Fact]
    public async Task Missing_password_is_rejected()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        var result = await harness.Stps.Approve(
            id, new QcApprovalRequest { Password = "  ", Comments = "x" }, userId, []);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.ReauthenticationRequired", result.Error.Code);
    }

    /// <summary>
    /// Criterion 5 — the approval goes through the real IApprovalRepository.ApproveItem, not
    /// a bespoke parallel path. Calling the generic engine directly, with re-authentication
    /// already confirmed, produces exactly the same signed result.
    /// </summary>
    [Fact]
    public async Task Generic_approval_engine_drives_qc_approvals()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        // Prove the stage rows were created by the real engine's CreateInitialApprovalsAsync.
        var stage = await harness.Db.QcApprovals.SingleAsync(item => item.EntityId == id);
        Assert.Equal(ApprovalStatus.Pending, stage.Status);
        Assert.NotNull(stage.ActivatedAt);

        // Re-authenticate, then call the same method the generic ApprovalController calls.
        Assert.True((await harness.SignatureService.VerifyAsync(
            userId, QcWorksheetTestContext.CorrectPassword)).IsSuccess);

        var result = await harness.ApprovalRepository.ApproveItem(
            QcWorksheetModelTypes.StandardTestProcedure, id, userId, [], "Approved by");

        Assert.True(result.IsSuccess);

        var stp = await harness.Db.QcStandardTestProcedures.SingleAsync(item => item.Id == id);
        Assert.Equal(QcDocumentStatus.Approved, stp.Status);
        Assert.True(stp.Approved);
    }

    /// <summary>
    /// The gap the QC wrapper exists to close: reaching the generic approval engine
    /// <i>without</i> re-authenticating cannot sign a QC document, even with a valid session.
    /// </summary>
    [Fact]
    public async Task Generic_engine_without_reauthentication_cannot_approve_qc()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        // No VerifyAsync call: this is the generic endpoint's "valid session only" path.
        var result = await harness.ApprovalRepository.ApproveItem(
            QcWorksheetModelTypes.StandardTestProcedure, id, userId, [], "Approved by");

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.ReauthenticationRequired", result.Error.Code);

        var stp = await harness.Db.QcStandardTestProcedures.SingleAsync(item => item.Id == id);
        Assert.Equal(QcDocumentStatus.UnderReview, stp.Status);
        Assert.False(stp.Approved);
    }

    /// <summary>
    /// Locked governance decision: with no approval chain configured, QC fails loudly rather
    /// than silently auto-approving the way every other module does.
    /// </summary>
    [Fact]
    public async Task Submit_without_a_configured_chain_fails_loudly()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        var result = await harness.Stps.SubmitForReview(id, userId);

        Assert.False(result.IsSuccess);
        Assert.Equal("QcWorksheet.NoApprovalWorkflowConfigured", result.Error.Code);

        var stp = await harness.Db.QcStandardTestProcedures.SingleAsync(item => item.Id == id);
        Assert.Equal(QcDocumentStatus.Draft, stp.Status);
        Assert.False(stp.Approved);
        Assert.Empty(await harness.Db.QcApprovals.ToListAsync());
    }

    /// <summary>
    /// Defence in depth for the same rule, at the engine level: the approval engine itself
    /// refuses to auto-approve a QC model type.
    /// </summary>
    [Fact]
    public async Task Approval_engine_refuses_to_auto_approve_qc_model_types()
    {
        using var harness = new QcWorksheetTestContext();
        var userId = (await harness.SeedUser()).Id;
        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ApprovalRepository.CreateInitialApprovalsAsync(
                QcWorksheetModelTypes.StandardTestProcedure, id));
    }

    /// <summary>Rejecting returns the document to Draft and records the signature.</summary>
    [Fact]
    public async Task Rejecting_returns_document_to_draft_and_records_signature()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        var userId = harness.Approver.Id;

        var id = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(id, userId);

        var rejected = await harness.Stps.Reject(id, new QcApprovalRequest
        {
            Password = QcWorksheetTestContext.CorrectPassword,
            Comments = "Insufficient detail in section 5.2"
        }, userId, []);

        Assert.True(rejected.IsSuccess);
        Assert.Equal(QcDocumentStatus.Draft, rejected.Value.Status);

        var row = await harness.Db.QcApprovals.SingleAsync(item => item.EntityId == id);
        Assert.Equal(ApprovalStatus.Rejected, row.Status);
        Assert.NotNull(row.ReauthConfirmedAt);
        Assert.Equal("Insufficient detail in section 5.2", row.Comments);
    }

    /// <summary>
    /// The centralized queue surfaces pending QC approvals across entity types from the one
    /// shared table.
    /// </summary>
    [Fact]
    public async Task Centralized_queue_lists_pending_qc_approvals()
    {
        using var harness = new QcWorksheetTestContext();
        await harness.SeedApprovalChain(QcWorksheetModelTypes.StandardTestProcedure);
        await harness.SeedApprovalChain(QcWorksheetModelTypes.WorksheetTemplate);
        var userId = harness.Approver.Id;

        var stpId = (await harness.Stps.CreateStp(NewStp(), userId)).Value.Id;
        await harness.Stps.SubmitForReview(stpId, userId);

        var template = await harness.Templates.CreateTemplate(new CreateWorksheetTemplateRequest
        {
            Code = "WS-001",
            Name = "Assay worksheet",
            Category = WorksheetCategory.Chemical,
            Sections =
            [
                new CreateWorksheetSectionRequest
                {
                    Order = 1,
                    Name = "Preparation",
                    Fields =
                    [
                        new CreateWorksheetFieldRequest
                        {
                            Order = 1, FieldKey = "sample_weight", Label = "Sample weight",
                            Type = WorksheetFieldType.Number, Mode = WorksheetFieldMode.Entry
                        }
                    ]
                }
            ]
        }, userId);

        Assert.True(template.IsSuccess);
        await harness.Templates.SubmitForReview(template.Value.Id, userId);

        var pending = await harness.Approvals.GetPendingApprovals(userId, []);

        Assert.True(pending.IsSuccess);
        Assert.Equal(2, pending.Value.Count);
        Assert.Contains(pending.Value, item =>
            item.EntityType == QcApprovalEntityTypes.StandardTestProcedure && item.EntityId == stpId);
        Assert.Contains(pending.Value, item =>
            item.EntityType == QcApprovalEntityTypes.WorksheetTemplate);
    }
}
