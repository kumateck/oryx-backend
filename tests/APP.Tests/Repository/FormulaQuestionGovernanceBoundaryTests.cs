using APP.Repository;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class FormulaBoundaryCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed class FormulaQuestionGovernanceBoundaryTests
{
    [Fact]
    public async Task GenericCreate_RejectsFormulaQuestions()
    {
        await using var context = CreateContext();
        var result = await Repository(context).CreateQuestion(
            Request(QuestionType.Formula), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Form.Question.FormulaGovernanceRequired",
            Assert.Single(result.Errors).Code);
        Assert.Empty(context.Questions);
    }

    [Fact]
    public async Task GenericUpdate_DoesNotReplaceLegacyFormulaOption()
    {
        await using var context = CreateContext();
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Legacy assay", Type = QuestionType.Formula
        };
        var option = new QuestionOption
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Question = question,
            Name = "{\"expression\":\":legacy\"}"
        };
        context.AddRange(question, option);
        await context.SaveChangesAsync();

        var result = await Repository(context).UpdateQuestion(
            Request(QuestionType.ShortAnswer), question.Id, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Form.Question.FormulaGovernanceRequired",
            Assert.Single(result.Errors).Code);
        var persisted = await context.QuestionOptions.SingleAsync();
        Assert.Equal(option.Id, persisted.Id);
        Assert.Equal("{\"expression\":\":legacy\"}", persisted.Name);
    }

    [Fact]
    public async Task GovernedMetadataUpdate_PreservesLegacyFormulaOption()
    {
        await using var context = CreateContext();
        var question = new Question
        {
            Id = Guid.NewGuid(), Label = "Legacy assay", Type = QuestionType.Formula
        };
        var option = new QuestionOption
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Question = question,
            Name = "{\"expression\":\":legacy\"}"
        };
        context.AddRange(question, option);
        await context.SaveChangesAsync();

        var result = await FormulaQuestionDraftPersistence.UpdateMetadataAsync(
            context, Request(QuestionType.Formula), question.Id,
            Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated", (await context.Questions.SingleAsync()).Label);
        var persisted = await context.QuestionOptions.SingleAsync();
        Assert.Equal(option.Id, persisted.Id);
        Assert.Equal("{\"expression\":\":legacy\"}", persisted.Name);
    }

    private static CreateQuestionRequest Request(QuestionType type) => new()
    {
        Label = "Updated", Type = type, Validation = QuestionValidationType.None,
        Options = [new CreateQuestionOptionsRequest { Name = "replacement" }]
    };

    private static FormRepository Repository(ApplicationDbContext context) =>
        new(context, null!, null!, null!, null!);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new FormulaBoundaryCurrentUser());
}
