using APP.Repository;
using APP.Services.Forms;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Forms.Request;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class CompletenessCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file sealed class SubmissionRecorder : IFormulaSubmissionService
{
    public int Calls { get; private set; }
    public Task<Result<FormulaSubmissionSetDto?>> FinalizeAsync(Guid responseId, Guid actorId, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(Result.Success<FormulaSubmissionSetDto?>(null));
    }
}

public sealed class FormSubmissionCompletenessTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Finalize_RejectsNullableSavedDraftBeforeFormulaExecution(string? value)
    {
        await using var context = CreateContext();
        var response = Seed(context, value, required: false);
        await context.SaveChangesAsync();
        var formulas = new SubmissionRecorder();

        var result = await Repository(context, formulas).SubmitFormResponseFinal(response.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Response.MissingFields", result.Error.Code);
        Assert.Equal(0, formulas.Calls);
        Assert.Null(response.CheckedAt);
        Assert.Equal(value, Assert.Single(response.FormResponses).Value);
    }

    [Fact]
    public async Task Finalize_RequiresEveryWorksheetFieldEvenWhenTemplateMarksItOptional()
    {
        await using var context = CreateContext();
        var response = Seed(context, "Complies", required: false);
        response.FormResponses.Clear();
        await context.SaveChangesAsync();
        var result = await Repository(context, new SubmissionRecorder()).SubmitFormResponseFinal(response.Id, Guid.NewGuid());
        Assert.Equal("Response.MissingFields", result.Error.Code);
    }

    [Fact]
    public async Task Finalize_AcceptsCompleteSavedDraft()
    {
        await using var context = CreateContext();
        var response = Seed(context, "Complies", required: false);
        await context.SaveChangesAsync();
        var formulas = new SubmissionRecorder();
        var result = await Repository(context, formulas).SubmitFormResponseFinal(response.Id, Guid.NewGuid());
        Assert.True(result.IsSuccess);
        Assert.Equal(1, formulas.Calls);
    }

    [Fact]
    public async Task DirectSubmit_RejectsMissingDraftDataBeforeCreatingRecords()
    {
        await using var context = CreateContext();
        var response = Seed(context, "Complies", required: false);
        context.Responses.Remove(response);
        await context.SaveChangesAsync();
        var formulas = new SubmissionRecorder();
        var result = await Repository(context, formulas).SubmitFormResponse(new CreateResponseRequest
        {
            FormId = response.FormId, BatchManufacturingRecordId = response.BatchManufacturingRecordId,
            FormResponses = []
        }, Guid.NewGuid());
        Assert.Equal("Response.MissingFields", result.Error.Code);
        Assert.Empty(context.Responses);
        Assert.Empty(context.FormResponses);
        Assert.Equal(0, formulas.Calls);
    }

    [Theory]
    [InlineData("null", false)]
    [InlineData("{}", false)]
    [InlineData("{\"a\":1,\"b\":null}", false)]
    [InlineData("{\"a\":1}", false)]
    [InlineData("{\"a\":0,\"b\":2}", true)]
    public void SimpleFormula_ChecksEveryInput(string answer, bool complete) =>
        Assert.Equal(complete, FormAnswerCompleteness.IsComplete(answer, QuestionType.Formula,
            "{\"type\":0,\"expression\":\":a + :b\"}"));

    [Theory]
    [InlineData("", "1", 100, false)]
    [InlineData("0.321", "", 100, false)]
    [InlineData("0.321", "1", null, false)]
    [InlineData("0.321", "0", 100, true)]
    public void Assay_RejectsNullableWeightsReadingsAndManualVariables(string weight, string reading, int? factor, bool complete)
    {
        var answer = System.Text.Json.JsonSerializer.Serialize(new
        {
            standardInputs = new { wt = weight }, sampleInputs = new[] { new { wt = "0.2" } },
            tableData = new[] { new[] { reading, "2" } },
            manualVars = new { factor = new { raw = "100", evaluated = factor, label = "" } }
        });
        const string definition = """
            {"type":3,"expression":":sample1Avg / :standardAvg * :factor","numberOfReadings":1,
             "numberOfSamples":1,"standardInTable":true,"standardInputs":[{"key":"wt"}],
             "sampleInputs":[[{"key":"wt"}]],"manualVarLabels":{"factor":"Factor"}}
            """;
        Assert.Equal(complete, FormAnswerCompleteness.IsComplete(answer, QuestionType.Formula, definition));
    }

    [Fact]
    public void Uniformity_OnlyRequiresTheSelectedModesInputs()
    {
        const string definition = """{"type":2,"mode":"tablet","sampleCount":2}""";
        Assert.False(FormAnswerCompleteness.IsComplete("""{"inputs":{"bulkTotal":"4","indWts":["2",""]}}""", QuestionType.Formula, definition));
        Assert.True(FormAnswerCompleteness.IsComplete("""{"inputs":{"bulkTotal":"4","indWts":["2","2"]},"results":{"totalContent":null}}""", QuestionType.Formula, definition));
    }

    [Fact]
    public void Choices_PreserveLegacyCommaSeparatedAnswersButRejectNullAndEmptyArrays()
    {
        Assert.True(FormAnswerCompleteness.IsComplete("Balance A, Balance B", QuestionType.Instrument, null));
        Assert.True(FormAnswerCompleteness.IsComplete("""[{"value":"Balance A","label":"Balance A"}]""", QuestionType.Instrument, null));
        Assert.False(FormAnswerCompleteness.IsComplete("null", QuestionType.Instrument, null));
        Assert.False(FormAnswerCompleteness.IsComplete("[]", QuestionType.Instrument, null));
    }

    [Fact]
    public void Table_RequiresEveryConfiguredEditableCell()
    {
        const string definition = """{"rows":1,"cols":2,"cellConfigs":{"0-0":{"type":"input"},"0-1":{"type":"select"}}}""";
        Assert.False(FormAnswerCompleteness.IsComplete("""{"0-0":"0","0-1":""}""", QuestionType.Table, definition));
        Assert.True(FormAnswerCompleteness.IsComplete("""{"0-0":"0","0-1":"Complies"}""", QuestionType.Table, definition));
    }

    private static Response Seed(ApplicationDbContext context, string? value, bool required)
    {
        var form = new Form { Id = Guid.NewGuid(), Name = "Worksheet" };
        var section = new FormSection { Id = Guid.NewGuid(), FormId = form.Id, Form = form, Name = "Results" };
        var question = new Question { Id = Guid.NewGuid(), Label = "Appearance", Type = QuestionType.ShortAnswer };
        var field = new FormField { Id = Guid.NewGuid(), FormSectionId = section.Id, FormSection = section, QuestionId = question.Id, Question = question, Required = required };
        var response = new Response { Id = Guid.NewGuid(), FormId = form.Id, Form = form, BatchManufacturingRecordId = Guid.NewGuid() };
        response.FormResponses.Add(new FormResponse { Id = Guid.NewGuid(), FormFieldId = field.Id, FormField = field, Value = value! });
        context.AddRange(form, section, field, response);
        return response;
    }

    private static FormRepository Repository(ApplicationDbContext context, IFormulaSubmissionService formulas) => new(context, null!, null!, null!, formulas);
    private static ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CompletenessCurrentUser());
}
