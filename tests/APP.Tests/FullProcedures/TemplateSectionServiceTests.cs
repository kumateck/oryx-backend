using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateSectionServiceTests
{
    [Fact]
    public async Task Create_pins_exact_published_questions_conditions_and_audit()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.SectionRequest(),
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Matches("^[a-f0-9]{64}$", result.Value.ContentHash);
        Assert.Equal([fixture.FirstRevisionId, fixture.SecondRevisionId],
            result.Value.Questions.Select(item => item.RevisionId));
        var rule = Assert.Single(result.Value.ConditionalRules);
        Assert.Equal(fixture.SecondQuestionId, rule.TargetQuestionId);
        Assert.Equal(fixture.FirstQuestionId, rule.DependsOnQuestionId);
        var audit = await fixture.Context.Set<TemplateSectionRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Contains("\"title\":\"Environmental checks\"", audit.SnapshotJson);
    }

    [Fact]
    public async Task Unpublished_or_mismatched_question_revision_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var unpublished = fixture.SectionRequest();
        unpublished.Questions[0].QuestionId = fixture.DraftQuestionId;
        unpublished.Questions[0].RevisionId = fixture.DraftRevisionId;
        var unpublishedResult = await fixture.Service.CreateAsync(unpublished,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var mismatched = fixture.SectionRequest();
        mismatched.Questions[0].RevisionId = fixture.SecondRevisionId;
        var mismatchResult = await fixture.Service.CreateAsync(mismatched,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSection.Invalid", Assert.Single(unpublishedResult.Errors).Code);
        Assert.Equal("TemplateSection.Invalid", Assert.Single(mismatchResult.Errors).Code);
        Assert.Empty(await fixture.Context.Set<TemplateSection>().ToListAsync());
    }

    [Theory]
    [InlineData(TemplateSectionConditionOperator.Equals, null, true)]
    [InlineData(TemplateSectionConditionOperator.IsAnswered, "unexpected", false)]
    [InlineData(TemplateSectionConditionOperator.NotEquals, "acceptable", true)]
    public async Task Invalid_condition_shapes_and_forward_dependencies_fail_closed(
        TemplateSectionConditionOperator operation, string? value, bool reverseDependency)
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.SectionRequest();
        request.ConditionalRules[0].Operator = operation;
        request.ConditionalRules[0].Value = value;
        if (reverseDependency)
        {
            request.ConditionalRules[0].TargetQuestionId = fixture.FirstQuestionId;
            request.ConditionalRules[0].DependsOnQuestionId = fixture.SecondQuestionId;
        }

        var result = await fixture.Service.CreateAsync(request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSection.Invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Area_grants_separate_view_from_authoring()
    {
        await using var fixture = await Fixture.Create();
        var create = await fixture.Service.CreateAsync(fixture.SectionRequest(),
            fixture.AuthorId, [fixture.ViewerRoleId], Guid.NewGuid());
        var list = await fixture.Service.ListAsync(fixture.AreaId, [fixture.ViewerRoleId]);
        var denied = await fixture.Service.ListAsync(fixture.AreaId, [Guid.NewGuid()]);

        Assert.Equal("TemplateSection.AccessDenied", Assert.Single(create.Errors).Code);
        Assert.True(list.IsSuccess);
        Assert.Empty(list.Value);
        Assert.Equal("TemplateSection.AccessDenied", Assert.Single(denied.Errors).Code);
    }

    [Fact]
    public async Task Regulated_lifecycle_supports_return_and_retires_replaced_revision()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateSection();
        var transition = fixture.Transition(first.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(first.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.ReturnToDraftAsync(first.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.SubmitForReviewAsync(first.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(first.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        var reviewerPublish = await fixture.Service.PublishAsync(first.Id, transition,
            fixture.ReviewerId, [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.Equal("TemplateSection.SegregationOfDuties",
            Assert.Single(reviewerPublish.Errors).Code);
        Assert.True((await fixture.Service.PublishAsync(first.Id, transition,
            fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var second = await fixture.Service.CreateRevisionAsync(first.TemplateSectionId,
            fixture.RevisionRequest("Environmental verification"), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        await Publish(fixture, second.Value);

        var revisions = await fixture.Context.Set<TemplateSectionRevision>()
            .Where(item => item.TemplateSectionId == first.TemplateSectionId)
            .OrderBy(item => item.Sequence).ToListAsync();
        Assert.Equal(TemplateSectionRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(TemplateSectionRevisionStatus.Published, revisions[1].Status);
        var retired = await fixture.Service.RetireAsync(second.Value.Id,
            fixture.Transition(second.Value.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.True(retired.IsSuccess);
        Assert.Equal(TemplateSectionRevisionStatus.Retired, retired.Value.Status);
    }

    [Fact]
    public async Task Stale_hash_cannot_update_or_submit()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateSection();
        var update = fixture.UpdateRequest(draft.ContentHash);
        update.Title = "Environmental observations";
        var updated = await fixture.Service.UpdateDraftAsync(draft.Id, update,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        var stale = await fixture.Service.SubmitForReviewAsync(draft.Id,
            fixture.Transition(draft.ContentHash), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(updated.IsSuccess);
        Assert.Equal("TemplateSection.Conflict", Assert.Single(stale.Errors).Code);
    }

    [Fact]
    public async Task Publish_blocks_a_question_revision_retired_after_section_review()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateSection();
        var transition = fixture.Transition(draft.ContentHash);
        await fixture.Service.SubmitForReviewAsync(draft.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        await fixture.Service.RecordReviewAsync(draft.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid());
        var question = await fixture.Context.Set<TemplateQuestionRevision>()
            .SingleAsync(item => item.Id == fixture.FirstRevisionId);
        question.Status = TemplateQuestionRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.PublishAsync(draft.Id, transition,
            fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateSection.DependencyUnavailable", Assert.Single(result.Errors).Code);
    }

    private static async Task Publish(Fixture fixture, TemplateSectionRevisionDto revision)
    {
        var transition = fixture.Transition(revision.ContentHash);
        await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        await fixture.Service.RecordReviewAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid());
        await fixture.Service.PublishAsync(revision.Id, transition,
            fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid());
    }
}
