using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateActivityServiceTests
{
    [Fact]
    public async Task Create_pins_forms_actions_roles_resources_data_rules_and_audit()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.ActivityRequest(),
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.FormRevisionId, Assert.Single(result.Value.Forms).RevisionId);
        Assert.Equal(2, result.Value.Actions.Count);
        Assert.Equal([fixture.ReviewerRoleId], result.Value.Actions[0].CheckerRoleIds);
        Assert.Single(result.Value.Resources);
        Assert.Equal(2, result.Value.DataBindings.Count);
        Assert.Equal(5, result.Value.CompletionRules.Count);
        var audit = await fixture.Context.Set<TemplateActivityRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Contains("\"name\":\"Sample analysis\"", audit.SnapshotJson);
    }

    [Fact]
    public async Task Unpublished_or_mismatched_form_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var unpublished = fixture.ActivityRequest();
        unpublished.Forms[0].FormId = fixture.DraftFormId;
        unpublished.Forms[0].RevisionId = fixture.DraftFormRevisionId;
        var unpublishedResult = await fixture.Service.CreateAsync(unpublished,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var mismatch = fixture.ActivityRequest();
        mismatch.Forms[0].RevisionId = fixture.DraftFormRevisionId;
        var mismatchResult = await fixture.Service.CreateAsync(mismatch,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateActivity.Invalid", Assert.Single(unpublishedResult.Errors).Code);
        Assert.Equal("TemplateActivity.Invalid", Assert.Single(mismatchResult.Errors).Code);
    }

    [Fact]
    public async Task Role_overlap_and_missing_completion_rule_fail_closed()
    {
        await using var fixture = await Fixture.Create();
        var overlap = fixture.ActivityRequest();
        overlap.Actions[0].CheckerRoleIds = [fixture.AuthorRoleId];
        var overlapResult = await fixture.Service.CreateAsync(overlap,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var incomplete = fixture.ActivityRequest();
        incomplete.CompletionRules.RemoveAll(item =>
            item.RuleType == TemplateActivityCompletionRuleType.DomainReceiptRecorded);
        var incompleteResult = await fixture.Service.CreateAsync(incomplete,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateActivity.Invalid", Assert.Single(overlapResult.Errors).Code);
        Assert.Equal("TemplateActivity.Invalid", Assert.Single(incompleteResult.Errors).Code);
    }

    [Fact]
    public async Task Unknown_role_resource_or_action_capability_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var role = fixture.ActivityRequest();
        role.Actions[0].PerformerRoleIds = [Guid.NewGuid()];
        var roleResult = await fixture.Service.CreateAsync(role,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var resource = fixture.ActivityRequest();
        resource.Resources[0].CapabilityId = "unknown-resource";
        var resourceResult = await fixture.Service.CreateAsync(resource,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateActivity.Invalid", Assert.Single(roleResult.Errors).Code);
        Assert.Equal("TemplateActivity.Invalid", Assert.Single(resourceResult.Errors).Code);
    }

    [Fact]
    public async Task Lifecycle_enforces_separation_and_retires_replacement()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateActivity();
        await SubmitAndReview(fixture, first);
        var reviewerPublish = await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.ReviewerId,
            [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.Equal("TemplateActivity.SegregationOfDuties",
            Assert.Single(reviewerPublish.Errors).Code);
        Assert.True((await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var source = fixture.ActivityRequest();
        var next = new CreateTemplateActivityRevisionRequest
        {
            Name = "Sample analysis v2", Instructions = source.Instructions,
            Forms = source.Forms, Actions = source.Actions, Resources = source.Resources,
            DataBindings = source.DataBindings, CompletionRules = source.CompletionRules,
            Reason = "Create the replacement activity revision.",
        };
        var second = await fixture.Service.CreateRevisionAsync(first.TemplateActivityId, next,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        await SubmitAndReview(fixture, second.Value);
        Assert.True((await fixture.Service.PublishAsync(second.Value.Id,
            fixture.Transition(second.Value.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);
        var revisions = await fixture.Context.Set<TemplateActivityRevision>()
            .Where(x => x.TemplateActivityId == first.TemplateActivityId)
            .OrderBy(x => x.Sequence).ToListAsync();
        Assert.Equal(TemplateActivityRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(TemplateActivityRevisionStatus.Published, revisions[1].Status);
    }

    [Fact]
    public async Task Publish_blocks_form_or_area_binding_removed_after_review()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateActivity();
        await SubmitAndReview(fixture, draft);
        var form = await fixture.Context.Set<TemplateFormRevision>()
            .SingleAsync(x => x.Id == fixture.FormRevisionId);
        form.Status = TemplateFormRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.PublishAsync(draft.Id,
            fixture.Transition(draft.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateActivity.DependencyUnavailable", Assert.Single(result.Errors).Code);
    }

    private static async Task SubmitAndReview(Fixture fixture,
        TemplateActivityRevisionDto revision)
    {
        var transition = fixture.Transition(revision.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
    }
}
