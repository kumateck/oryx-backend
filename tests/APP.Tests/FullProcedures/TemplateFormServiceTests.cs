using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateFormServiceTests
{
    [Fact]
    public async Task Create_pins_exact_sections_rules_controls_and_audit()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.CreateAsync(fixture.FormRequest(),
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.RequiresEvidence);
        Assert.True(result.Value.RequiresSignature);
        Assert.Equal([fixture.FirstSectionRevisionId, fixture.SecondSectionRevisionId],
            result.Value.Sections.Select(item => item.RevisionId));
        var rule = Assert.Single(result.Value.ConditionalRules);
        Assert.Equal(fixture.SourceQuestionRevisionId, rule.SourceQuestionRevisionId);
        var audit = await fixture.Context.Set<TemplateFormRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Contains("\"name\":\"Environmental analysis\"", audit.SnapshotJson);
    }

    [Fact]
    public async Task Unpublished_or_mismatched_section_revision_fails_closed()
    {
        await using var fixture = await Fixture.Create();
        var unpublished = fixture.FormRequest();
        unpublished.Sections[0].SectionId = fixture.DraftSectionId;
        unpublished.Sections[0].RevisionId = fixture.DraftSectionRevisionId;
        unpublished.ConditionalRules.Clear();
        var unpublishedResult = await fixture.Service.CreateAsync(unpublished,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var mismatch = fixture.FormRequest();
        mismatch.Sections[0].RevisionId = fixture.SecondSectionRevisionId;
        var mismatchResult = await fixture.Service.CreateAsync(mismatch,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateForm.Invalid", Assert.Single(unpublishedResult.Errors).Code);
        Assert.Equal("TemplateForm.Invalid", Assert.Single(mismatchResult.Errors).Code);
        Assert.Empty(await fixture.Context.Set<TemplateForm>().ToListAsync());
    }

    [Fact]
    public async Task Invalid_source_question_and_forward_dependency_fail_closed()
    {
        await using var fixture = await Fixture.Create();
        var missingQuestion = fixture.FormRequest();
        missingQuestion.ConditionalRules[0].SourceQuestionId = Guid.NewGuid();
        var missingResult = await fixture.Service.CreateAsync(missingQuestion,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var forward = fixture.FormRequest();
        var rule = forward.ConditionalRules[0];
        rule.TargetSectionId = fixture.FirstSectionId;
        rule.SourceSectionId = fixture.SecondSectionId;
        var forwardResult = await fixture.Service.CreateAsync(forward,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateForm.Invalid", Assert.Single(missingResult.Errors).Code);
        Assert.Equal("TemplateForm.Invalid", Assert.Single(forwardResult.Errors).Code);
    }

    [Fact]
    public async Task Lifecycle_enforces_separation_and_retires_replaced_revision()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateForm();
        await SubmitAndReview(fixture, first);
        var reviewerPublish = await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.ReviewerId,
            [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.Equal("TemplateForm.SegregationOfDuties",
            Assert.Single(reviewerPublish.Errors).Code);
        Assert.True((await fixture.Service.PublishAsync(first.Id,
            fixture.Transition(first.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var second = await fixture.Service.CreateRevisionAsync(first.TemplateFormId,
            fixture.RevisionRequest("Environmental analysis v2"), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        await SubmitAndReview(fixture, second.Value);
        Assert.True((await fixture.Service.PublishAsync(second.Value.Id,
            fixture.Transition(second.Value.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var revisions = await fixture.Context.Set<TemplateFormRevision>()
            .Where(item => item.TemplateFormId == first.TemplateFormId)
            .OrderBy(item => item.Sequence).ToListAsync();
        Assert.Equal(TemplateFormRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(TemplateFormRevisionStatus.Published, revisions[1].Status);
    }

    [Fact]
    public async Task Stale_hash_cannot_update_draft()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateForm();
        var source = fixture.FormRequest();
        var request = new UpdateTemplateFormRevisionRequest
        {
            Name = "Updated analysis", Description = source.Description,
            RequiresEvidence = true, RequiresSignature = true,
            Sections = source.Sections, ConditionalRules = source.ConditionalRules,
            ExpectedContentHash = new string('0', 64),
            Reason = "Correct the controlled form draft.",
        };

        var result = await fixture.Service.UpdateDraftAsync(draft.Id, request,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateForm.Conflict", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Publish_blocks_section_retired_after_review()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateForm();
        await SubmitAndReview(fixture, draft);
        var section = await fixture.Context.Set<TemplateSectionRevision>()
            .SingleAsync(item => item.Id == fixture.FirstSectionRevisionId);
        section.Status = TemplateSectionRevisionStatus.Retired;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.PublishAsync(draft.Id,
            fixture.Transition(draft.ContentHash), fixture.PublisherId,
            [fixture.PublisherRoleId], Guid.NewGuid());

        Assert.Equal("TemplateForm.DependencyUnavailable", Assert.Single(result.Errors).Code);
    }

    private static async Task SubmitAndReview(Fixture fixture, TemplateFormRevisionDto revision)
    {
        var transition = fixture.Transition(revision.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
    }
}
