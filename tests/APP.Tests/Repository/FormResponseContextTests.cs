using APP.Repository;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FormResponseContextCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class FormResponseContextTests
{
    [Fact]
    public async Task DraftResponses_AreReusedWithinStepAndSeparatedAcrossSteps()
    {
        await using var context = CreateContext();
        var form = CreateForm("Product test");
        var secondQuestion = new Question { Id = Guid.NewGuid(), Label = "Second result" };
        var secondField = new FormField
        {
            Id = Guid.NewGuid(),
            FormSectionId = form.Section.Id,
            FormSection = form.Section,
            QuestionId = secondQuestion.Id,
            Question = secondQuestion,
        };
        var batchId = Guid.NewGuid();
        var firstStepId = Guid.NewGuid();
        var secondStepId = Guid.NewGuid();
        context.AddRange(form.Form, form.Section, form.Field, secondField);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        var userId = Guid.NewGuid();

        var first = await repository.SaveFormResponseDraft(
            new SaveResponseDraftRequest
            {
                FormId = form.Form.Id,
                FormFieldId = form.Field.Id,
                Value = "stage one, first answer",
                BatchManufacturingRecordId = batchId,
                ProductionActivityStepId = firstStepId,
            }, userId);
        var subsequent = await repository.SaveFormResponseDraft(
            new SaveResponseDraftRequest
            {
                ResponseId = first.Value,
                FormId = form.Form.Id,
                FormFieldId = secondField.Id,
                Value = "stage one, second answer",
                BatchManufacturingRecordId = batchId,
                ProductionActivityStepId = firstStepId,
            }, userId);
        var nextStage = await repository.SaveFormResponseDraft(
            new SaveResponseDraftRequest
            {
                FormId = form.Form.Id,
                FormFieldId = form.Field.Id,
                Value = "stage two, first answer",
                BatchManufacturingRecordId = batchId,
                ProductionActivityStepId = secondStepId,
            }, userId);
        var firstLookup = await repository.GetResponseId(new GetResponseIdRequest
        {
            BatchManufacturingRecordId = batchId,
            ProductionActivityStepId = firstStepId,
        });
        var secondLookup = await repository.GetResponseId(new GetResponseIdRequest
        {
            BatchManufacturingRecordId = batchId,
            ProductionActivityStepId = secondStepId,
        });

        Assert.True(first.IsSuccess);
        Assert.True(subsequent.IsSuccess);
        Assert.True(nextStage.IsSuccess);
        Assert.Equal(first.Value, subsequent.Value);
        Assert.NotEqual(first.Value, nextStage.Value);
        Assert.Equal(first.Value, firstLookup.Value);
        Assert.Equal(nextStage.Value, secondLookup.Value);
        Assert.Equal(2, await context.Responses.CountAsync());
    }

    [Fact]
    public async Task GetResponseId_ReturnsSuccessfulNullWhenStepHasNoDraft()
    {
        await using var context = CreateContext();

        var result = await CreateRepository(context).GetResponseId(
            new GetResponseIdRequest
            {
                BatchManufacturingRecordId = Guid.NewGuid(),
                ProductionActivityStepId = Guid.NewGuid(),
            });

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task SaveDraft_RejectsFieldFromAnotherStageForm()
    {
        await using var context = CreateContext();
        var first = CreateForm("Intermediate");
        var second = CreateForm("Finished");
        var batchId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        var response = new Response
        {
            Id = Guid.NewGuid(),
            FormId = first.Form.Id,
            Form = first.Form,
            BatchManufacturingRecordId = batchId,
            ProductionActivityStepId = stepId,
        };
        context.AddRange(first.Form, first.Section, first.Field,
            second.Form, second.Section, second.Field, response);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SaveFormResponseDraft(
            new SaveResponseDraftRequest
            {
                ResponseId = response.Id,
                FormId = first.Form.Id,
                FormFieldId = second.Field.Id,
                Value = "must not be stored",
                BatchManufacturingRecordId = batchId,
                ProductionActivityStepId = stepId,
            }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "Response.FormMismatch");
        Assert.Empty(context.FormResponses);
    }

    [Fact]
    public async Task SaveDraft_RejectsResponseFromAnotherProductionStep()
    {
        await using var context = CreateContext();
        var form = CreateForm("Finished");
        var batchId = Guid.NewGuid();
        var response = new Response
        {
            Id = Guid.NewGuid(),
            FormId = form.Form.Id,
            Form = form.Form,
            BatchManufacturingRecordId = batchId,
            ProductionActivityStepId = Guid.NewGuid(),
        };
        context.AddRange(form.Form, form.Section, form.Field, response);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SaveFormResponseDraft(
            new SaveResponseDraftRequest
            {
                ResponseId = response.Id,
                FormId = form.Form.Id,
                FormFieldId = form.Field.Id,
                Value = "must not be stored",
                BatchManufacturingRecordId = batchId,
                ProductionActivityStepId = Guid.NewGuid(),
            }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "Response.ContextMismatch");
        Assert.Empty(context.FormResponses);
    }

    [Fact]
    public async Task SubmitResponse_RejectsMixedStageFieldsBeforeCreatingResponse()
    {
        await using var context = CreateContext();
        var intermediate = CreateForm("Intermediate");
        var finished = CreateForm("Finished");
        context.AddRange(
            intermediate.Form, intermediate.Section, intermediate.Field,
            finished.Form, finished.Section, finished.Field);
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).SubmitFormResponse(
            new CreateResponseRequest
            {
                FormId = finished.Form.Id,
                FormResponses =
                [
                    new CreateFormResponseRequest
                    {
                        FormFieldId = finished.Field.Id,
                        Value = "valid finished result",
                    },
                    new CreateFormResponseRequest
                    {
                        FormFieldId = intermediate.Field.Id,
                        Value = "must not be mixed",
                    },
                ],
            }, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Response.FormMismatch", result.Error.Code);
        Assert.Empty(context.Responses);
        Assert.Empty(context.FormResponses);
    }

    private static (Form Form, FormSection Section, FormField Field) CreateForm(string name)
    {
        var form = new Form { Id = Guid.NewGuid(), Name = name };
        var section = new FormSection
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Name = name,
        };
        var question = new Question { Id = Guid.NewGuid(), Label = "Result" };
        var field = new FormField
        {
            Id = Guid.NewGuid(),
            FormSectionId = section.Id,
            FormSection = section,
            QuestionId = question.Id,
            Question = question,
        };
        return (form, section, field);
    }

    private static FormRepository CreateRepository(ApplicationDbContext context) =>
        new(context, null!, null!, null!);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new FormResponseContextCurrentUser());
}
