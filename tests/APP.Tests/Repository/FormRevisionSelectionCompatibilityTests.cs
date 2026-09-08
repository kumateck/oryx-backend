using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class RevisionSelectionCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed class FormRevisionSelectionCompatibilityTests
{
    [Fact]
    public async Task LegacyFormulaInForm_DoesNotBlockOrdinaryDraftAnswer()
    {
        await using var context = CreateContext();
        var setup = CreateMixedForm();
        context.AddRange(setup.Form, setup.Section, setup.ShortAnswerField,
            setup.LegacyFormulaField);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SaveFormResponseDraft(
            RequestFor(setup.Form.Id, setup.ShortAnswerField.Id), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Single(context.FormResponses);
        Assert.Null(Assert.Single(context.Responses).FormRevisionId);
    }

    [Fact]
    public async Task GovernedFormulaInForm_RequiresApprovedFormRevision()
    {
        await using var context = CreateContext();
        var setup = CreateMixedForm();
        var definition = new FormulaDefinition
        {
            Id = Guid.NewGuid(), Key = "governed", Name = "Governed",
            PresentationPreset = "Universal"
        };
        context.AddRange(setup.Form, setup.Section, setup.ShortAnswerField,
            setup.LegacyFormulaField, definition,
            new QuestionFormulaDefinition
            {
                Id = Guid.NewGuid(), QuestionId = setup.LegacyFormulaField.QuestionId,
                Question = setup.LegacyFormulaField.Question,
                FormulaDefinitionId = definition.Id, FormulaDefinition = definition
            });
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SaveFormResponseDraft(
            RequestFor(setup.Form.Id, setup.ShortAnswerField.Id), Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error =>
            error.Code == "FormulaRuntime.ConfigurationUnavailable");
        Assert.Empty(context.Responses);
    }

    private static SaveResponseDraftRequest RequestFor(Guid formId, Guid fieldId) => new()
    {
        FormId = formId,
        FormFieldId = fieldId,
        Value = "complies",
        BatchManufacturingRecordId = Guid.NewGuid(),
        ProductionActivityStepId = Guid.NewGuid(),
    };

    private static (Form Form, FormSection Section, FormField ShortAnswerField,
        FormField LegacyFormulaField) CreateMixedForm()
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Mixed legacy form" };
        var section = new FormSection
        {
            Id = Guid.NewGuid(), FormId = form.Id, Form = form, Name = "Results"
        };
        var shortQuestion = new Question
        {
            Id = Guid.NewGuid(), Label = "Appearance", Type = QuestionType.ShortAnswer
        };
        var formulaQuestion = new Question
        {
            Id = Guid.NewGuid(), Label = "Legacy calculation", Type = QuestionType.Formula
        };
        return (form, section,
            Field(section, shortQuestion), Field(section, formulaQuestion));
    }

    private static FormField Field(FormSection section, Question question) => new()
    {
        Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section,
        QuestionId = question.Id, Question = question
    };

    private static FormRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, null!, null!);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new RevisionSelectionCurrentUser());
}
