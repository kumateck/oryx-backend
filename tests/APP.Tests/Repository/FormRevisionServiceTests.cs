using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class FormRevisionTestUser(Guid userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file sealed class UnusedFormulaClient : IFormulaCalculationClient
{
    public Task<Result<FormulaServiceResponse>> ValidateAsync(
        JsonElement request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Result<FormulaServiceResponse>> EvaluateAsync(
        JsonElement request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    public Task<Result<bool>> IsReadyAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Success(true));
}

public sealed class FormRevisionServiceTests
{
    [Fact]
    public async Task Lifecycle_RequiresReviewAndSeparateApprover()
    {
        var author = Guid.NewGuid();
        var reviewer = Guid.NewGuid();
        var approver = Guid.NewGuid();
        await using var context = Context(author);
        var form = new Form { Id = Guid.NewGuid(), Name = "Controlled template" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Name = "Section", Description = ""
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Observation", Type = QuestionType.ShortAnswer,
            Validation = QuestionValidationType.None
        };
        context.AddRange(form, section, question, new FormField
        {
            Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id,
            Description = "Observed result"
        });
        await context.SaveChangesAsync();
        var service = new FormRevisionService(context, new UnusedFormulaClient());

        var draft = await service.CreateDraftAsync(form.Id,
            new FormRevisionDraftRequest([]), author, Guid.NewGuid());
        Assert.True(draft.IsSuccess);
        var transition = new FormRevisionTransitionRequest(
            draft.Value.ContentHash, "Controlled template lifecycle test.");
        Assert.True((await service.SubmitForReviewAsync(
            draft.Value.Id, transition, author, Guid.NewGuid())).IsSuccess);
        Assert.Equal("FormRevision.SegregationOfDuties", Assert.Single(
            (await service.RecordReviewAsync(draft.Value.Id, transition,
                author, Guid.NewGuid())).Errors).Code);
        Assert.True((await service.RecordReviewAsync(draft.Value.Id, transition,
            reviewer, Guid.NewGuid())).IsSuccess);
        Assert.Equal("FormRevision.SegregationOfDuties", Assert.Single(
            (await service.ApproveAsync(draft.Value.Id, transition,
                reviewer, Guid.NewGuid())).Errors).Code);
        var approved = await service.ApproveAsync(
            draft.Value.Id, transition, approver, Guid.NewGuid());

        Assert.True(approved.IsSuccess);
        Assert.Equal("Approved", approved.Value.StatusName);
        Assert.Equal(4, await context.FormRevisionAudits.CountAsync());

        var replacement = await service.CreateDraftAsync(form.Id,
            new FormRevisionDraftRequest([]), author, Guid.NewGuid());
        var replacementTransition = new FormRevisionTransitionRequest(
            replacement.Value.ContentHash, "Approve a controlled template replacement.");
        Assert.True((await service.SubmitForReviewAsync(replacement.Value.Id,
            replacementTransition, author, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.RecordReviewAsync(replacement.Value.Id,
            replacementTransition, reviewer, Guid.NewGuid())).IsSuccess);
        Assert.True((await service.ApproveAsync(replacement.Value.Id,
            replacementTransition, approver, Guid.NewGuid())).IsSuccess);

        var original = await context.FormRevisions.SingleAsync(
            item => item.Id == approved.Value.Id);
        Assert.Equal(FormRevisionStatus.Retired, original.Status);
        Assert.NotNull(original.RetiredAt);
        Assert.Single(await context.FormRevisions.Where(item =>
            item.FormId == form.Id && item.Status == FormRevisionStatus.Approved)
            .ToListAsync());
        Assert.Contains(await context.FormRevisionAudits.ToListAsync(),
            item => item.FormRevisionId == original.Id && item.Action == "Superseded");
    }

    [Fact]
    public async Task FormulaField_DraftMayBeIncompleteButCannotBePublished()
    {
        var author = Guid.NewGuid();
        await using var context = Context(author);
        var form = new Form { Id = Guid.NewGuid(), Name = "Formula template" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Name = "Section", Description = ""
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Assay", Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        };
        context.AddRange(form, section, question, new FormField
        {
            Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id
        });
        await context.SaveChangesAsync();
        var service = new FormRevisionService(context, new UnusedFormulaClient());

        var result = await service.CreateDraftAsync(form.Id,
            new FormRevisionDraftRequest([]), author, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var transition = new FormRevisionTransitionRequest(
            result.Value.ContentHash, "Submit incomplete formula template.");
        var submitted = await service.SubmitForReviewAsync(
            result.Value.Id, transition, author, Guid.NewGuid());
        Assert.True(submitted.IsFailure);
        Assert.Equal("FormRevision.Invalid", Assert.Single(submitted.Errors).Code);
    }

    [Fact]
    public async Task Draft_CanBeRefreshedFromTheCurrentTemplate()
    {
        var author = Guid.NewGuid();
        await using var context = Context(author);
        var form = new Form { Id = Guid.NewGuid(), Name = "Editable template" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Name = "Section", Description = ""
        };
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Observation", Type = QuestionType.ShortAnswer,
            Validation = QuestionValidationType.None
        };
        var field = new FormField
        {
            Id = Guid.NewGuid(), FormSectionId = section.Id, QuestionId = question.Id,
            Description = "Initial"
        };
        context.AddRange(form, section, question, field);
        await context.SaveChangesAsync();
        var service = new FormRevisionService(context, new UnusedFormulaClient());
        var draft = await service.CreateDraftAsync(form.Id,
            new FormRevisionDraftRequest([]), author, Guid.NewGuid());

        field.Description = "Corrected draft description";
        await context.SaveChangesAsync();
        var updated = await service.UpdateDraftAsync(draft.Value.Id,
            new FormRevisionDraftRequest([]), author, Guid.NewGuid());

        Assert.True(updated.IsSuccess);
        Assert.NotEqual(draft.Value.ContentHash, updated.Value.ContentHash);
        Assert.Equal(2, await context.FormRevisionAudits.CountAsync());
    }

    private static ApplicationDbContext Context(Guid actor) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new FormRevisionTestUser(actor));
}
