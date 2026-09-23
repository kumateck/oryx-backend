using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateWorkflowServiceTests
{
    [Fact]
    public async Task Lifecycle_enforces_separation_and_retires_replacement()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateWorkflow();
        await SubmitAndReview(fixture, first);
        var reviewerPublish = await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.ReviewerId,
            [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.Equal("TemplateWorkflow.SegregationOfDuties",
            Assert.Single(reviewerPublish.Errors).Code);
        Assert.True((await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var source = fixture.WorkflowRequest();
        var next = new CreateTemplateWorkflowRevisionRequest
        {
            Name = "Sample release workflow v2", Description = source.Description,
            Nodes = source.Nodes, Edges = source.Edges, Layouts = source.Layouts,
            Reason = "Create the replacement workflow revision.",
        };
        var second = await fixture.Service.CreateRevisionAsync(first.TemplateWorkflowId, next,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        await SubmitAndReview(fixture, second.Value);
        Assert.True((await fixture.Service.PublishAsync(second.Value.Id,
            fixture.Transition(second.Value.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);
        var revisions = await fixture.Context.Set<TemplateWorkflowRevision>()
            .Where(x => x.TemplateWorkflowId == first.TemplateWorkflowId)
            .OrderBy(x => x.Sequence).ToListAsync();
        Assert.Equal(TemplateWorkflowRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(TemplateWorkflowRevisionStatus.Published, revisions[1].Status);
    }

    [Fact]
    public async Task Publish_blocks_activity_binding_removed_after_review()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateWorkflow();
        await SubmitAndReview(fixture, draft);
        var activity = await fixture.Context.Set<TemplateActivityRevision>()
            .SingleAsync(x => x.Id == fixture.ActivityRevisionId);
        activity.Status = TemplateActivityRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.PublishAsync(draft.Id,
            fixture.Transition(draft.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.DependencyUnavailable", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Semantic_change_updates_hash_but_layout_change_does_not()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateWorkflow();
        var source = fixture.WorkflowRequest();
        source.Nodes.Single(node => node.Key == "activity").Name = "Renamed governed stage";
        var update = new UpdateTemplateWorkflowRevisionRequest
        {
            Name = source.Name, Description = source.Description,
            Nodes = source.Nodes, Edges = source.Edges, Layouts = source.Layouts,
            ExpectedContentHash = draft.ContentHash,
            Reason = "Rename the governed workflow stage label.",
        };

        var changed = await fixture.Service.UpdateDraftAsync(draft.Id, update,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(changed.IsSuccess);
        Assert.NotEqual(draft.ContentHash, changed.Value.ContentHash);

        var layout = await fixture.Service.UpdateLayoutAsync(draft.Id, new()
        {
            ExpectedContentHash = changed.Value.ContentHash,
            Layouts = [new() { NodeKey = "activity", PositionX = 420, PositionY = 180 }],
        }, fixture.AuthorId, [fixture.AuthorRoleId]);
        Assert.True(layout.IsSuccess);
        Assert.Equal(changed.Value.ContentHash, layout.Value.ContentHash);
    }

    [Fact]
    public async Task Non_finite_layout_coordinates_are_rejected()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateWorkflow();

        var result = await fixture.Service.UpdateLayoutAsync(draft.Id, new()
        {
            ExpectedContentHash = draft.ContentHash,
            Layouts = [new() { NodeKey = "activity", PositionX = double.NaN, PositionY = 10 }],
        }, fixture.AuthorId, [fixture.AuthorRoleId]);

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    private static async Task SubmitAndReview(Fixture fixture,
        TemplateWorkflowRevisionDto revision)
    {
        var transition = fixture.Transition(revision.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
    }
}
