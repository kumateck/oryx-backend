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

    // pH of sample as the question editor stores it (ATR 01a1cb57): the expression holds the builder's settings JSON.
    private const string WrappedPh = """
        {"type":0,"expression":"{\"expression\":\"(:ph1+:ph2)/2\",\"decimalPlaces\":2,\"percent\":false,\"type\":0,\"variables\":{\"ph1\":{\"value\":\"\",\"label\":\"pH reading 1\"},\"ph2\":{\"value\":\"\",\"label\":\"pH reading 2\"}},\"constants\":{}}"}
        """;

    [Theory]
    [InlineData("{\"ph1\":5.1232,\"ph2\":5.1126}", true)]
    [InlineData("{\"ph1\":5.1232}", false)]
    public void SimpleFormula_StoredInsideTheExpression_ReadsTheInnerSettings(string answer, bool complete) =>
        Assert.Equal(complete, FormAnswerCompleteness.IsComplete(answer, QuestionType.Formula, WrappedPh));

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

    private const string GelAssay = """
        {"type":3,"expression":"((:sampleAvg * :standardWeight * :Fillweight) / (:standardAvg * :sampleWeight * :Claim) * :purity) * 100",
         "numberOfReadings":5,"numberOfSamples":2,"standardInTable":true,"headers":["Standard","Sample 1","Sample 2"],
         "standardInputs":[{"key":"weight"}],"sampleInputs":[[{"key":"weight"}],[{"key":"weight"}]],
         "manualVarLabels":{"Fillweight":"Fill Weight","Claim":"Claim","purity":"Purity"}}
        """;

    [Theory]
    // Five standard injections and two per sample: the shorter sample columns are complete.
    [InlineData("""[["795.1","820.1","813.5"],["795.1","820.3","813.9"],["795.1","",""],["794.7","",""],["794.0","",""]]""", false, true)]
    [InlineData("""[["795.1","820.1","813.5"],["795.1","820.3","813.9"],["795.1",null,null],["794.7",null,null],["794.0",null,null]]""", false, true)]
    // A column with no reading at all is not.
    [InlineData("""[["795.1","820.1",""],["795.1","820.3",""],["795.1","",""],["794.7","",""],["794.0","",""]]""", false, false)]
    // Each column needs at least two readings before its average is taken.
    [InlineData("""[["795.1","820.1","813.5"],["795.1","820.3",""],["795.1","",""],["794.7","",""],["794.0","",""]]""", false, false)]
    // A filled cell must be a number.
    [InlineData("""[["795.1","820.1","813.5"],["795.1","x","813.9"],["795.1","",""],["794.7","",""],["794.0","",""]]""", false, false)]
    // Calculated table columns evaluate every cell, so those worksheets still need every reading.
    [InlineData("""[["795.1","820.1","813.5"],["795.1","820.3","813.9"],["795.1","",""],["794.7","",""],["794.0","",""]]""", true, false)]
    [InlineData("""[["795.1","820.1","813.5"],["795.1","820.3","813.9"],["795.1","820.2","813.6"],["794.7","820.1","813.5"],["794.0","820.2","813.7"]]""", true, true)]
    public void Readings_columns_may_hold_fewer_readings_than_the_table_has_rows(string table, bool formulaInTable, bool complete)
    {
        var answer = """{"standardInputs":{"weight":"0.0514"},"sampleInputs":[{"weight":"5.1669"},{"weight":"5.0255"}],"tableData":"""
            + table
            + ""","manualVars":{"Fillweight":{"raw":"30.0","evaluated":30},"Claim":{"raw":"0.300","evaluated":0.3},"purity":{"raw":"0.992","evaluated":0.992}}}""";
        var definition = formulaInTable ? GelAssay.Replace("\"type\":3,", "\"type\":3,\"formulaInTable\":true,") : GelAssay;
        Assert.Equal(complete, FormAnswerCompleteness.IsComplete(answer, QuestionType.Formula, definition));
    }

    [Theory]
    [InlineData("""{"manualVars":{"C":"61.0224","A":"29.7506","B":"57.3314"}}""", true)]
    [InlineData("""{"manualVars":{"C":"61.0224","A":"29.7506","B":""}}""", false)]
    public void Universal_formula_with_only_manual_inputs_needs_no_readings_table(string answer, bool complete)
    {
        // Weight per mL: (C-A)/(B-A), no sample columns, and the editor's default of five readings.
        const string definition = """
            {"type":3,"expression":"(:C-:A)/(:B-:A)","noOfSamples":0,"noOfReadings":5,"standardInTable":false,
             "manualVarLabels":{"C":"Weight of pycnometer & sample","A":"Weight of empty pycnometer","B":"Weight of pycnometer & Water"}}
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

    // Assay: Paracetamol's approved revision keeps a standard Weight input in its settings, but standard inputs are off,
    // so the worksheet never shows that box. Only the inputs the worksheet shows are required.
    private const string Paracetamol = """
        {"type":3,"expression":"(:sampleAvg/:A)*(:dil/:sampleWeight)*:wt*5*(1000/:Claim)*:purity","constants":{"Claim":120,"A":715,"dil":200},
         "headers":["Sample 1","Sample 2"],"numberOfReadings":2,"numberOfSamples":2,"standardInput":false,"standardInTable":false,
         "standardInputs":[{"key":"weight"}],"sampleInput":true,"sampleInTable":true,"sampleInputs":[[{"key":"weight"}],[{"key":"weight"}]],
         "manualVarLabels":{"wt":"","purity":""}}
        """;

    [Theory]
    [InlineData("""{"standardInputs":{},"sampleInputs":[{"weight":"5.6613"},{"weight":"5.6822"}],"tableData":[["0.4468","0.4474"],["0.4465","0.4470"]],"manualVars":{"wt":{"raw":"1.13","evaluated":1.13},"purity":{"raw":"100","evaluated":100}}}""", true)]
    [InlineData("""{"standardInputs":{},"sampleInputs":[{"weight":"5.6613"},{}],"tableData":[["0.4468","0.4474"],["0.4465","0.4470"]],"manualVars":{"wt":{"raw":"1.13","evaluated":1.13},"purity":{"raw":"100","evaluated":100}}}""", false)]
    public void UniversalFormula_RequiresOnlyTheInputsTheWorksheetShows(string answer, bool complete) =>
        Assert.Equal(complete, FormAnswerCompleteness.IsComplete(answer, QuestionType.Formula, Paracetamol));

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
