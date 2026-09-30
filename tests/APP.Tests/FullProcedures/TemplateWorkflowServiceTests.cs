using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateWorkflowServiceTests
{
    [Fact]
    public async Task Create_pins_activity_and_records_audit()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.WorkflowRequest(),
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Nodes.Count);
        Assert.Equal(2, result.Value.Edges.Count);
        var activityNode = Assert.Single(result.Value.Nodes,
            node => node.NodeType == TemplateWorkflowNodeType.Activity);
        Assert.Equal(fixture.ActivityRevisionId, activityNode.TemplateActivityRevisionId);
        var audit = await fixture.Context.Set<TemplateWorkflowRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Contains("\"name\":\"Sample release workflow\"", audit.SnapshotJson);
    }

    [Fact]
    public async Task Start_and_end_cardinality_is_enforced()
    {
        await using var fixture = await Fixture.Create();

        var extraStart = fixture.WorkflowRequest();
        extraStart.Nodes.Add(new() { Key = "start2", Name = "Extra start", Order = 3,
            NodeType = TemplateWorkflowNodeType.Start });
        var extraStartResult = await fixture.Service.CreateAsync(extraStart,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var noEnd = fixture.WorkflowRequest();
        noEnd.Nodes.RemoveAll(node => node.Key == "end");
        noEnd.Edges.RemoveAll(edge => edge.TargetKey == "end");
        var noEndResult = await fixture.Service.CreateAsync(noEnd,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(extraStartResult.Errors).Code);
        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(noEndResult.Errors).Code);
    }

    [Fact]
    public async Task Branch_requires_unique_nonempty_branch_keys()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.WorkflowRequest();
        request.TemplateAreaId = fixture.AreaId;
        request.Nodes =
        [
            new() { Key = "start", Name = "Start", Order = 0, NodeType = TemplateWorkflowNodeType.Start },
            new() { Key = "branch", Name = "Branch", Order = 1, NodeType = TemplateWorkflowNodeType.Branch },
            new() { Key = "end1", Name = "End one", Order = 2, NodeType = TemplateWorkflowNodeType.End },
            new() { Key = "end2", Name = "End two", Order = 3, NodeType = TemplateWorkflowNodeType.End },
        ];
        request.Edges =
        [
            new() { SourceKey = "start", TargetKey = "branch", Order = 0 },
            new() { SourceKey = "branch", TargetKey = "end1", BranchKey = "same", Order = 0 },
            new() { SourceKey = "branch", TargetKey = "end2", BranchKey = "same", Order = 1 },
        ];

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Fork_and_join_group_keys_must_pair_bijectively()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.WorkflowRequest();
        request.Nodes =
        [
            new() { Key = "start", Name = "Start", Order = 0, NodeType = TemplateWorkflowNodeType.Start },
            new() { Key = "fork", Name = "Fork", Order = 1, NodeType = TemplateWorkflowNodeType.Fork,
                JoinGroupKey = "fj" },
            new() { Key = "waitA", Name = "Wait A", Order = 2, NodeType = TemplateWorkflowNodeType.Wait,
                WaitKind = TemplateWorkflowWaitKind.Duration },
            new() { Key = "waitB", Name = "Wait B", Order = 3, NodeType = TemplateWorkflowNodeType.Wait,
                WaitKind = TemplateWorkflowWaitKind.Duration },
            new() { Key = "join", Name = "Join", Order = 4, NodeType = TemplateWorkflowNodeType.Join,
                JoinGroupKey = "different" },
            new() { Key = "end", Name = "End", Order = 5, NodeType = TemplateWorkflowNodeType.End },
        ];
        request.Edges =
        [
            new() { SourceKey = "start", TargetKey = "fork", Order = 0 },
            new() { SourceKey = "fork", TargetKey = "waitA", Order = 0 },
            new() { SourceKey = "fork", TargetKey = "waitB", Order = 1 },
            new() { SourceKey = "waitA", TargetKey = "join", Order = 0 },
            new() { SourceKey = "waitB", TargetKey = "join", Order = 1 },
            new() { SourceKey = "join", TargetKey = "end", Order = 0 },
        ];

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Hold_and_resume_group_keys_must_match()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.WorkflowRequest();
        request.Nodes =
        [
            new() { Key = "start", Name = "Start", Order = 0, NodeType = TemplateWorkflowNodeType.Start },
            new() { Key = "hold", Name = "Hold", Order = 1, NodeType = TemplateWorkflowNodeType.Hold,
                HoldGroupKey = "h1" },
            new() { Key = "resume", Name = "Resume", Order = 2, NodeType = TemplateWorkflowNodeType.Resume,
                HoldGroupKey = "h2" },
            new() { Key = "end", Name = "End", Order = 3, NodeType = TemplateWorkflowNodeType.End },
        ];
        request.Edges =
        [
            new() { SourceKey = "start", TargetKey = "hold", Order = 0 },
            new() { SourceKey = "hold", TargetKey = "resume", Order = 0 },
            new() { SourceKey = "resume", TargetKey = "end", Order = 0 },
        ];

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Rework_target_must_be_a_strictly_earlier_node()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.WorkflowRequest();
        request.Nodes =
        [
            new() { Key = "start", Name = "Start", Order = 0, NodeType = TemplateWorkflowNodeType.Start },
            new() { Key = "activity", Name = "Run analysis", Order = 1,
                NodeType = TemplateWorkflowNodeType.Activity,
                TemplateActivityId = fixture.ActivityId,
                TemplateActivityRevisionId = fixture.ActivityRevisionId },
            new() { Key = "rework", Name = "Rework", Order = 2, NodeType = TemplateWorkflowNodeType.Rework,
                ReworkTargetKey = "end", ReworkMaxAttempts = 3 },
            new() { Key = "end", Name = "End", Order = 3, NodeType = TemplateWorkflowNodeType.End },
        ];
        request.Edges =
        [
            new() { SourceKey = "start", TargetKey = "activity", Order = 0 },
            new() { SourceKey = "activity", TargetKey = "rework", Order = 0 },
            new() { SourceKey = "rework", TargetKey = "end", Order = 0 },
        ];

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Disconnected_subgraph_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.WorkflowRequest();
        request.Nodes.Add(new() { Key = "orphan", Name = "Orphan", Order = 3,
            NodeType = TemplateWorkflowNodeType.Wait, WaitKind = TemplateWorkflowWaitKind.Duration });

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Unpublished_or_mismatched_activity_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var unpublished = fixture.WorkflowRequest();
        unpublished.Nodes.Single(node => node.Key == "activity").TemplateActivityId =
            fixture.DraftActivityId;
        unpublished.Nodes.Single(node => node.Key == "activity").TemplateActivityRevisionId =
            fixture.DraftActivityRevisionId;
        var unpublishedResult = await fixture.Service.CreateAsync(unpublished,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var mismatch = fixture.WorkflowRequest();
        mismatch.Nodes.Single(node => node.Key == "activity").TemplateActivityRevisionId =
            fixture.DraftActivityRevisionId;
        var mismatchResult = await fixture.Service.CreateAsync(mismatch,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(unpublishedResult.Errors).Code);
        Assert.Equal("TemplateWorkflow.Invalid", Assert.Single(mismatchResult.Errors).Code);
    }

}
