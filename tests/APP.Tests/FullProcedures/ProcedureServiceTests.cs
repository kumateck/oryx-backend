using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class ProcedureServiceTests
{
    [Fact]
    public async Task Create_pins_workflow_applicability_scope_and_audit()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.Request(), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.WorkflowRevisionId, result.Value.TemplateWorkflowRevisionId);
        Assert.Equal(new string('a', 64), result.Value.WorkflowContentHash);
        Assert.Equal("{\"maximum\":1.5,\"type\":\"object\"}",
            result.Value.ParameterSchemaJson);
        Assert.Single(result.Value.Applicabilities);
        Assert.Equal(ProcedureRecordScope.Manufacturing,
            Assert.Single(result.Value.StageScopes).RecordScope);
        var audit = await fixture.Context.Set<ProcedureRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Equal(result.Value.ContentHash, audit.ContentHash);
    }

    [Fact]
    public async Task Create_rejects_missing_or_duplicate_activity_scope()
    {
        await using var fixture = await Fixture.Create();
        var missing = fixture.Request();
        missing.StageScopes = [];
        var missingResult = await fixture.Service.CreateAsync(missing, fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.Equal("Procedure.Invalid", Assert.Single(missingResult.Errors).Code);

        var duplicate = fixture.Request();
        duplicate.StageScopes.Add(new() { WorkflowNodeKey = "MIX",
            RecordScope = ProcedureRecordScope.Packaging });
        var duplicateResult = await fixture.Service.CreateAsync(duplicate, fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.Equal("Procedure.Invalid", Assert.Single(duplicateResult.Errors).Code);
    }

    [Fact]
    public async Task Create_requires_target_area_author_assignment()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.Request(), fixture.AuthorId,
            [Guid.NewGuid()], Guid.NewGuid());
        Assert.Equal("Procedure.AccessDenied", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Approval_enforces_segregation_and_dependency_availability()
    {
        await using var fixture = await Fixture.Create();
        var revision = await fixture.CreateProcedure();
        await fixture.Review(revision);

        var reviewerApproval = await fixture.Service.ApproveAsync(revision.Id,
            fixture.Transition(revision.ContentHash), fixture.ReviewerId,
            [fixture.ApproverRoleId], Guid.NewGuid());
        Assert.Equal("Procedure.SegregationOfDuties",
            Assert.Single(reviewerApproval.Errors).Code);

        var workflow = await fixture.Context.Set<TemplateWorkflowRevision>()
            .SingleAsync(x => x.Id == fixture.WorkflowRevisionId);
        workflow.Status = TemplateWorkflowRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();
        var drift = await fixture.Service.ApproveAsync(revision.Id,
            fixture.Transition(revision.ContentHash), fixture.ApproverId,
            [fixture.ApproverRoleId], Guid.NewGuid());
        Assert.Equal("Procedure.DependencyUnavailable", Assert.Single(drift.Errors).Code);
    }

    [Fact]
    public async Task Approved_replacement_retires_previous_revision()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateProcedure();
        await fixture.Approve(first);
        var source = fixture.Request();
        var next = new CreateProcedureRevisionRequest
        {
            Name = "Commercial tablet Procedure v2", Description = source.Description,
            TemplateWorkflowId = source.TemplateWorkflowId,
            TemplateWorkflowRevisionId = source.TemplateWorkflowRevisionId,
            ParameterSchemaJson = source.ParameterSchemaJson,
            Applicabilities = source.Applicabilities, StageScopes = source.StageScopes,
            Reason = "Create the replacement Procedure revision.",
        };
        var second = await fixture.Service.CreateRevisionAsync(first.ProcedureDefinitionId,
            next, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        await fixture.Approve(second.Value);

        var revisions = await fixture.Context.Set<ProcedureRevision>()
            .Where(x => x.ProcedureDefinitionId == first.ProcedureDefinitionId)
            .OrderBy(x => x.Sequence).ToListAsync();
        Assert.Equal(ProcedureRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(ProcedureRevisionStatus.Approved, revisions[1].Status);
    }

    [Fact]
    public async Task Validation_reports_soft_deleted_applicability_without_mutation()
    {
        await using var fixture = await Fixture.Create();
        var revision = await fixture.CreateProcedure();
        var product = await fixture.Context.Set<DOMAIN.Entities.Products.Product>()
            .IgnoreQueryFilters().SingleAsync(x => x.Id == fixture.ProductId);
        product.DeletedAt = DateTime.UtcNow;
        await fixture.Context.SaveChangesAsync();

        var report = await fixture.Service.ValidateAsync(revision.Id,
            [fixture.AuthorRoleId]);
        Assert.True(report.IsSuccess);
        Assert.False(report.Value.IsValid);
        Assert.Contains("DependencyUnavailable", report.Value.BlockedReasons);
        Assert.Equal(ProcedureRevisionStatus.Draft,
            (await fixture.Context.Set<ProcedureRevision>().SingleAsync()).Status);
    }
}
