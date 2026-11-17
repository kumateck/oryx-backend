using System.Text.Json;
using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public class FormulaRepairEvaluationInputTests
{
    private const string Definition = """
        {"type":3,"expression":":factor * :ph","numberOfSamples":0,"manualVarLabels":{"factor":"Dilution factor","ph":"pH"}}
        """;

    [Fact]
    public void Manual_readings_send_the_evaluated_number_not_the_typed_expression()
    {
        var saved = """{"manualVars":{"factor":{"raw":"25 x 4","evaluated":100},"ph":{"raw":"6.8","evaluated":null}}}""";

        var inputs = LegacyFormulaRepairDefinition.ResolveInputs(Definition, saved, ["factor", "ph"], null);

        Assert.Equal("100", inputs["factor"]);
        // Without an evaluated number the typed value is still used.
        Assert.Equal("6.8", inputs["ph"]);
    }

    [Theory]
    [InlineData(3, "divides by zero")]
    [InlineData(2, "not a number")]
    [InlineData(1, "has no value")]
    public void An_engine_refusal_names_the_reading_and_the_reason(int status, string expected)
    {
        var evaluation = JsonSerializer.SerializeToElement(new
        {
            status, results = Array.Empty<object>(),
            errors = new[] { new { status, path = "/variables/factor", message = "Refused.", variableKey = "factor" } }
        });
        var response = new FormulaServiceResponse("v1", 2, null, 5, "Completed", "e", "b", null, null, null, null, null, null, null,
            [], [], [], [], evaluation, []);

        var message = QuestionFormulaRepairService.EngineRefusal(response, Definition, new Dictionary<string, string?> { ["factor"] = "0" });

        Assert.Contains(expected, message);
        Assert.Contains("Dilution factor (factor) = \"0\"", message);
    }

    [Fact]
    public void A_refused_request_reports_the_service_issues()
    {
        var response = new FormulaServiceResponse("v1", 2, null, 3, "RequestInvalid", "e", "b", null, null, null, null, null, null, null,
            [], [], [], [new FormulaServiceRequestIssue("/resolvedInputs/ph", "The value must be a decimal string.")], null, []);

        var message = QuestionFormulaRepairService.EngineRefusal(response, Definition, new Dictionary<string, string?>());

        Assert.Contains("RequestInvalid", message);
        Assert.Contains("The value must be a decimal string.", message);
    }
}
