using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateAdoptionServiceTests
{
    [Fact]
    public async Task Question_adoption_creates_target_draft_lineage_and_grant_audit()
    {
        await using var fixture = await Fixture.Create();

        var adoption = await fixture.Adopt(TemplateRevisionKind.Question);

        var draft = await fixture.Context.Set<TemplateQuestionRevision>().AsNoTracking()
            .SingleAsync(x => x.Id == adoption.TargetRevisionId);
        var grant = await fixture.Context.Set<TemplateSharingGrant>().AsNoTracking()
            .SingleAsync(x => x.Id == adoption.TemplateSharingGrantId);
        var audit = await fixture.Context.Set<TemplateSharingGrantAudit>().AsNoTracking()
            .SingleAsync(x => x.TemplateSharingGrantId == grant.Id);
        Assert.Equal(TemplateQuestionRevisionStatus.Draft, draft.Status);
        Assert.Null(draft.ReviewedById);
        Assert.Null(draft.PublishedById);
        Assert.Equal(fixture.TargetAreaId, adoption.TargetAreaId);
        Assert.Equal(3, grant.Version);
        Assert.Equal("Adopted", audit.Action);
        Assert.Equal(64, adoption.SnapshotHash.Length);
    }

    [Fact]
    public async Task Every_composite_kind_translates_dependencies_and_roles_to_target_area()
    {
        await using var fixture = await Fixture.Create();

        var section = await fixture.Adopt(TemplateRevisionKind.Section);
        var form = await fixture.Adopt(TemplateRevisionKind.Form);
        var activity = await fixture.Adopt(TemplateRevisionKind.Activity);
        var workflow = await fixture.Adopt(TemplateRevisionKind.Workflow);

        var sectionQuestion = await fixture.Context.Set<TemplateSectionQuestion>().AsNoTracking()
            .SingleAsync(x => x.TemplateSectionRevisionId == section.TargetRevisionId);
        var formSection = await fixture.Context.Set<TemplateFormSection>().AsNoTracking()
            .SingleAsync(x => x.TemplateFormRevisionId == form.TargetRevisionId);
        var activityForm = await fixture.Context.Set<TemplateActivityFormBinding>().AsNoTracking()
            .SingleAsync(x => x.TemplateActivityRevisionId == activity.TargetRevisionId);
        var activityRole = await fixture.Context.Set<TemplateActivityActionRole>().AsNoTracking()
            .SingleAsync(x => x.TemplateActivityAction.TemplateActivityRevisionId ==
                activity.TargetRevisionId);
        var workflowNode = await fixture.Context.Set<TemplateWorkflowNode>().AsNoTracking()
            .SingleAsync(x => x.TemplateWorkflowRevisionId == workflow.TargetRevisionId &&
                x.NodeType == TemplateWorkflowNodeType.Activity);
        Assert.Equal(fixture.TargetQuestionRevisionId,
            sectionQuestion.TemplateQuestionRevisionId);
        Assert.Equal(fixture.TargetSectionRevisionId, formSection.TemplateSectionRevisionId);
        Assert.Equal(fixture.TargetFormRevisionId, activityForm.TemplateFormRevisionId);
        Assert.Equal(fixture.TargetAuthorRoleId, activityRole.RoleId);
        Assert.Equal(fixture.TargetActivityRevisionId, workflowNode.TemplateActivityRevisionId);
    }

    [Fact]
    public async Task Missing_or_extra_local_mapping_fails_without_creating_a_draft()
    {
        await using var fixture = await Fixture.Create();
        var before = await fixture.Context.Set<TemplateSection>().CountAsync();
        var request = fixture.Request(TemplateRevisionKind.Section);
        request.DependencyMappings.Clear();

        var result = await fixture.Service.AdoptAsync(
            fixture.GrantIds[TemplateRevisionKind.Section], request, fixture.ActorId,
            [fixture.TargetAuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateAdoption.DependencyUnavailable", Assert.Single(result.Errors).Code);
        Assert.Equal(before, await fixture.Context.Set<TemplateSection>().CountAsync());
        Assert.Empty(await fixture.Context.Set<TemplateAdoption>().ToListAsync());
    }

    [Fact]
    public async Task Adoption_requires_current_grant_version_and_target_author_assignment()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.Request(TemplateRevisionKind.Question);
        request.ExpectedGrantVersion = 1;
        var stale = await fixture.Service.AdoptAsync(
            fixture.GrantIds[TemplateRevisionKind.Question], request, fixture.ActorId,
            [fixture.TargetAuthorRoleId], Guid.NewGuid());
        request.ExpectedGrantVersion = 2;
        var denied = await fixture.Service.AdoptAsync(
            fixture.GrantIds[TemplateRevisionKind.Question], request, fixture.ActorId,
            [Guid.NewGuid()], Guid.NewGuid());

        Assert.Equal("TemplateAdoption.Conflict", Assert.Single(stale.Errors).Code);
        Assert.Equal("TemplateAdoption.AccessDenied", Assert.Single(denied.Errors).Code);
    }

    [Fact]
    public async Task Adoption_rechecks_source_publication_before_cloning()
    {
        await using var fixture = await Fixture.Create();
        var source = await fixture.Context.Set<TemplateQuestionRevision>()
            .SingleAsync(x => x.Id == fixture.SourceQuestionRevisionId);
        source.Status = TemplateQuestionRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        var unavailable = await fixture.Service.AdoptAsync(
            fixture.GrantIds[TemplateRevisionKind.Question],
            fixture.Request(TemplateRevisionKind.Question), fixture.ActorId,
            [fixture.TargetAuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateAdoption.DependencyUnavailable",
            Assert.Single(unavailable.Errors).Code);
        Assert.Empty(await fixture.Context.Set<TemplateAdoption>().ToListAsync());
    }

    [Fact]
    public async Task One_grant_cannot_create_two_target_drafts()
    {
        await using var fixture = await Fixture.Create();
        var adoption = await fixture.Adopt(TemplateRevisionKind.Question);
        var request = fixture.Request(TemplateRevisionKind.Question);
        request.ExpectedGrantVersion = 3;

        var duplicate = await fixture.Service.AdoptAsync(adoption.TemplateSharingGrantId,
            request, fixture.ActorId, [fixture.TargetAuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateAdoption.Conflict", Assert.Single(duplicate.Errors).Code);
        Assert.Single(await fixture.Context.Set<TemplateAdoption>().ToListAsync());
    }

    [Fact]
    public async Task Adoption_read_is_scoped_to_target_area_roles()
    {
        await using var fixture = await Fixture.Create();
        var adoption = await fixture.Adopt(TemplateRevisionKind.Question);

        var allowed = await fixture.Service.GetAsync(
            adoption.Id, [fixture.TargetAuthorRoleId]);
        var denied = await fixture.Service.GetAsync(adoption.Id, [Guid.NewGuid()]);

        Assert.True(allowed.IsSuccess);
        Assert.Equal("TemplateAdoption.AccessDenied", Assert.Single(denied.Errors).Code);
    }
}
