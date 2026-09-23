using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateSharingServiceTests
{
    [Fact]
    public async Task Request_pins_exact_published_revision_and_audits()
    {
        await using var fixture = await Fixture.Create();

        var result = await fixture.Service.RequestAsync(fixture.Request(),
            fixture.SourceActorId, [fixture.SourcePublisherRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(TemplateSharingGrantStatus.Pending, result.Value.Status);
        Assert.Equal(new string('c', 64), result.Value.RevisionContentHash);
        var audit = await fixture.Context.Set<TemplateSharingGrantAudit>().SingleAsync();
        Assert.Equal("Requested", audit.Action);
    }

    [Fact]
    public async Task Other_area_approves_and_requester_cannot_self_approve()
    {
        await using var fixture = await Fixture.Create();
        var grant = await fixture.RequestGrant();
        var decision = new DecideTemplateSharingGrantRequest { ExpectedVersion = grant.Version,
            Reason = "Approve the exact shared definition revision." };

        var self = await fixture.Service.ApproveAsync(grant.Id, decision,
            fixture.SourceActorId, [fixture.TargetAuthorRoleId], Guid.NewGuid());
        var approved = await fixture.Service.ApproveAsync(grant.Id, decision,
            fixture.TargetActorId, [fixture.TargetAuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSharing.SegregationOfDuties", Assert.Single(self.Errors).Code);
        Assert.True(approved.IsSuccess);
        Assert.Equal(TemplateSharingGrantStatus.Active, approved.Value.Status);
        Assert.Equal(2, approved.Value.Version);
    }

    [Fact]
    public async Task Draft_revision_and_duplicate_open_grant_fail_closed()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.Service.RequestAsync(
            fixture.Request(fixture.DraftQuestionRevisionId), fixture.SourceActorId,
            [fixture.SourcePublisherRoleId], Guid.NewGuid());
        var first = await fixture.RequestGrant();
        var duplicate = await fixture.Service.RequestAsync(fixture.Request(),
            fixture.SourceActorId, [fixture.SourcePublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSharing.DependencyUnavailable", Assert.Single(draft.Errors).Code);
        Assert.Equal("TemplateSharing.Conflict", Assert.Single(duplicate.Errors).Code);
        Assert.Equal(TemplateSharingGrantStatus.Pending, first.Status);
    }

    [Fact]
    public async Task Approval_fails_when_either_participating_area_is_inactive()
    {
        await using var fixture = await Fixture.Create();
        var grant = await fixture.RequestGrant();
        var target = await fixture.Context.Set<TemplateArea>()
            .SingleAsync(x => x.Id == fixture.TargetAreaId);
        target.IsActive = false;
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        var result = await fixture.Service.ApproveAsync(grant.Id, new()
        {
            ExpectedVersion = grant.Version,
            Reason = "Do not approve sharing into an inactive area.",
        }, fixture.TargetActorId, [fixture.TargetAuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSharing.DependencyUnavailable", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Usage_returns_definition_counts_without_response_data()
    {
        await using var fixture = await Fixture.Create();
        var grant = await fixture.ApproveGrant(await fixture.RequestGrant());

        var usage = await fixture.Service.GetUsageAsync(TemplateRevisionKind.Question,
            fixture.QuestionId, fixture.QuestionRevisionId,
            [fixture.SourcePublisherRoleId]);
        var denied = await fixture.Service.GetUsageAsync(TemplateRevisionKind.Question,
            fixture.QuestionId, fixture.QuestionRevisionId, [Guid.NewGuid()]);

        Assert.True(usage.IsSuccess);
        Assert.Equal(1, usage.Value.DirectDefinitionReferences);
        Assert.Equal(1, usage.Value.ActiveSharingGrants);
        Assert.Equal(TemplateSharingGrantStatus.Active, grant.Status);
        Assert.Equal("TemplateSharing.AccessDenied", Assert.Single(denied.Errors).Code);
    }

    [Fact]
    public async Task Revoke_requires_current_version_and_area_publisher()
    {
        await using var fixture = await Fixture.Create();
        var grant = await fixture.ApproveGrant(await fixture.RequestGrant());
        var stale = await fixture.Service.RevokeAsync(grant.Id, new()
        {
            ExpectedVersion = 1, Reason = "Revoke the obsolete sharing grant.",
        }, fixture.TargetActorId, [fixture.TargetPublisherRoleId], Guid.NewGuid());
        var revoked = await fixture.Service.RevokeAsync(grant.Id, new()
        {
            ExpectedVersion = grant.Version, Reason = "Revoke the obsolete sharing grant.",
        }, fixture.TargetActorId, [fixture.TargetPublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSharing.Conflict", Assert.Single(stale.Errors).Code);
        Assert.True(revoked.IsSuccess);
        Assert.Equal(TemplateSharingGrantStatus.Revoked, revoked.Value.Status);
    }
}
