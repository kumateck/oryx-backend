using APP.Services.FullProcedures;
using DOMAIN.Entities.FullProcedures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.FullProcedures;

public sealed partial class TemplateQuestionServiceTests
{
    [Fact]
    public async Task Create_persists_hash_options_and_audit_snapshot()
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.QuestionRequest();
        request.AnswerType = TemplateQuestionAnswerType.SingleChoice;
        request.InputType = "radio";
        request.Options =
        [
            new TemplateQuestionOptionRequest { Value = "yes", Label = "Yes" },
            new TemplateQuestionOptionRequest { Value = "no", Label = "No" },
        ];

        var result = await fixture.Service.CreateAsync(
            request, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Matches("^[a-f0-9]{64}$", result.Value.ContentHash);
        Assert.Equal(2, result.Value.Options.Count);
        var audit = await fixture.Context.Set<TemplateQuestionRevisionAudit>().SingleAsync();
        Assert.Equal("DraftCreated", audit.Action);
        Assert.Contains("\"wording\":\"Record the observed result\"", audit.SnapshotJson);
    }

    [Theory]
    [InlineData(TemplateQuestionAnswerType.ShortText, "radio")]
    [InlineData(TemplateQuestionAnswerType.SingleChoice, "radio")]
    [InlineData(TemplateQuestionAnswerType.Calculation, "formula")]
    public async Task Invalid_answer_contracts_fail_closed(
        TemplateQuestionAnswerType answerType, string inputType)
    {
        await using var fixture = await Fixture.Create();
        var request = fixture.QuestionRequest();
        request.AnswerType = answerType;
        request.InputType = inputType;

        var result = await fixture.Service.CreateAsync(
            request, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.Equal("TemplateQuestion.Invalid", Assert.Single(result.Errors).Code);
        Assert.Empty(await fixture.Context.Set<TemplateQuestion>().ToListAsync());
    }

    [Fact]
    public async Task Resource_grants_scope_reads_and_authoring_independently()
    {
        await using var fixture = await Fixture.Create();
        var viewer = fixture.ViewerRoleId;
        var create = await fixture.Service.CreateAsync(
            fixture.QuestionRequest(), fixture.AuthorId, [viewer], Guid.NewGuid());
        var list = await fixture.Service.ListAsync(fixture.AreaId, [viewer]);
        var denied = await fixture.Service.ListAsync(fixture.AreaId, [Guid.NewGuid()]);

        Assert.Equal("TemplateQuestion.AccessDenied", Assert.Single(create.Errors).Code);
        Assert.True(list.IsSuccess);
        Assert.Empty(list.Value);
        Assert.Equal("TemplateQuestion.AccessDenied", Assert.Single(denied.Errors).Code);
    }

    [Fact]
    public async Task Regulated_lifecycle_requires_three_people_and_retires_prior_revision()
    {
        await using var fixture = await Fixture.Create();
        var first = await fixture.CreateQuestion();
        var transition = fixture.Transition(first.ContentHash);

        Assert.True((await fixture.Service.SubmitForReviewAsync(
            first.Id, transition, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        var selfReview = await fixture.Service.RecordReviewAsync(
            first.Id, transition, fixture.AuthorId, [fixture.ReviewerRoleId], Guid.NewGuid());
        Assert.Equal("TemplateQuestion.SegregationOfDuties", Assert.Single(selfReview.Errors).Code);
        Assert.True((await fixture.Service.RecordReviewAsync(
            first.Id, transition, fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        var reviewerPublish = await fixture.Service.PublishAsync(
            first.Id, transition, fixture.ReviewerId, [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.Equal("TemplateQuestion.SegregationOfDuties",
            Assert.Single(reviewerPublish.Errors).Code);
        var published = await fixture.Service.PublishAsync(
            first.Id, transition, fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid());
        Assert.True(published.IsSuccess);
        Assert.Equal(TemplateQuestionRevisionStatus.Published, published.Value.Status);

        var immutable = await fixture.Service.UpdateDraftAsync(first.Id,
            fixture.UpdateRequest(first.ContentHash), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.Equal("TemplateQuestion.Conflict", Assert.Single(immutable.Errors).Code);

        var second = await fixture.Service.CreateRevisionAsync(
            first.TemplateQuestionId, fixture.RevisionRequest("Record the verified result"),
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        Assert.True(second.IsSuccess);
        var secondTransition = fixture.Transition(second.Value.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(second.Value.Id,
            secondTransition, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(second.Value.Id,
            secondTransition, fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.PublishAsync(second.Value.Id,
            secondTransition, fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);

        var revisions = await fixture.Context.Set<TemplateQuestionRevision>()
            .Where(item => item.TemplateQuestionId == first.TemplateQuestionId)
            .OrderBy(item => item.Sequence).ToListAsync();
        Assert.Equal(TemplateQuestionRevisionStatus.Retired, revisions[0].Status);
        Assert.Equal(TemplateQuestionRevisionStatus.Published, revisions[1].Status);
    }

    [Fact]
    public async Task Stale_hash_cannot_update_or_transition_a_draft()
    {
        await using var fixture = await Fixture.Create();
        var draft = await fixture.CreateQuestion();
        var update = fixture.UpdateRequest(draft.ContentHash);
        update.Wording = "Record a corrected observed result";
        var updated = await fixture.Service.UpdateDraftAsync(
            draft.Id, update, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        var staleUpdate = await fixture.Service.UpdateDraftAsync(
            draft.Id, update, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());
        var staleSubmit = await fixture.Service.SubmitForReviewAsync(
            draft.Id, fixture.Transition(draft.ContentHash), fixture.AuthorId,
            [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(updated.IsSuccess);
        Assert.Equal("TemplateQuestion.Conflict", Assert.Single(staleUpdate.Errors).Code);
        Assert.Equal("TemplateQuestion.Conflict", Assert.Single(staleSubmit.Errors).Code);
    }

    [Fact]
    public async Task Calculation_pins_exact_published_source_revision()
    {
        await using var fixture = await Fixture.Create();
        var source = await fixture.CreateAndPublishQuestion();
        var calculation = fixture.QuestionRequest();
        calculation.Wording = "Calculate the derived result";
        calculation.AnswerType = TemplateQuestionAnswerType.Calculation;
        calculation.InputType = "formula";
        calculation.CalculationQuestionIds = [source.TemplateQuestionId];

        var result = await fixture.Service.CreateAsync(
            calculation, fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var reference = Assert.Single(result.Value.CalculationReferences);
        Assert.Equal(source.TemplateQuestionId, reference.QuestionId);
        Assert.Equal(source.Id, reference.RevisionId);
    }

    [Fact]
    public async Task Review_can_return_a_draft_and_publisher_can_retire_a_release()
    {
        await using var fixture = await Fixture.Create();
        var revision = await fixture.CreateQuestion();
        var transition = fixture.Transition(revision.ContentHash);
        Assert.True((await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);

        var returned = await fixture.Service.ReturnToDraftAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid());
        Assert.True(returned.IsSuccess);
        Assert.Equal(TemplateQuestionRevisionStatus.Draft, returned.Value.Status);
        Assert.Null(returned.Value.ReviewedById);

        Assert.True((await fixture.Service.SubmitForReviewAsync(revision.Id, transition,
            fixture.AuthorId, [fixture.AuthorRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.RecordReviewAsync(revision.Id, transition,
            fixture.ReviewerId, [fixture.ReviewerRoleId], Guid.NewGuid())).IsSuccess);
        Assert.True((await fixture.Service.PublishAsync(revision.Id, transition,
            fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid())).IsSuccess);
        var retired = await fixture.Service.RetireAsync(revision.Id, transition,
            fixture.PublisherId, [fixture.PublisherRoleId], Guid.NewGuid());

        Assert.True(retired.IsSuccess);
        Assert.Equal(TemplateQuestionRevisionStatus.Retired, retired.Value.Status);
        Assert.NotNull(retired.Value.RetiredAt);
        var actions = await fixture.Context.Set<TemplateQuestionRevisionAudit>()
            .OrderBy(item => item.OccurredAt).Select(item => item.Action).ToListAsync();
        Assert.Contains("ReturnedToDraft", actions);
        Assert.Contains("Retired", actions);
    }
}
