using System.Globalization;
using System.Text.Json;
using APP.Services.Formulas;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaWorksheetInputResolverTests
{
    private const string Values = """
        {
          "standardInputs":{"wt":"5.5"},
          "sampleInputs":[{"wt":"4.5"},{"wt":"6.5"}],
          "manualVars":{"factor":{"raw":"2.5","evaluated":2.5}},
          "tableData":[["10","20"],["12","24"],["14","28"]]
        }
        """;

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 2)]
    [InlineData(2, 1.632993161855452)]
    [InlineData(3, 16.666666666666664)]
    [InlineData(4, 10)]
    [InlineData(5, 14)]
    [InlineData(6, 3)]
    public void ResolvesControlledTableStatistics(int statistic, double expected)
    {
        using var values = JsonDocument.Parse(Values);
        using var source = JsonDocument.Parse(
            $$"""{"sourceType":3,"reference":"table:column:0","statistic":{{statistic}}}""");

        var result = FormulaResponseInputResolver.ResolveValue(
            values.RootElement, source.RootElement, "stat");

        Assert.NotNull(result);
        Assert.Equal(expected, double.Parse(result, CultureInfo.InvariantCulture), 12);
    }

    [Theory]
    [InlineData("local:standardInput:wt", "standardInput.wt", "5.5")]
    [InlineData("local:sampleInput:1:wt", "sampleInput2.wt", "6.5")]
    [InlineData("local:manual:factor", "factor", "2.5")]
    [InlineData("local:cell:2:1", "R3sample1", "28")]
    public void ResolvesUniversalScalarSources(
        string reference, string variableKey, string expected)
    {
        using var values = JsonDocument.Parse(Values);
        using var source = JsonDocument.Parse(
            $$"""{"sourceType":0,"reference":"{{reference}}"}""");

        Assert.Equal(expected, FormulaResponseInputResolver.ResolveValue(
            values.RootElement, source.RootElement, variableKey));
    }

    [Fact]
    public void EnforcesInputScaleAndRoundsTableStatisticsSeparately()
    {
        using var values = JsonDocument.Parse(Values);
        using var strictInput = JsonDocument.Parse(
            """{"sourceType":0,"reference":"local:standardInput:wt","inputDecimalPlaces":1}""");
        using var rejectedInput = JsonDocument.Parse(
            """{"sourceType":0,"reference":"local:standardInput:wt","inputDecimalPlaces":0}""");
        using var roundedAverage = JsonDocument.Parse(
            """{"sourceType":3,"reference":"table:column:0","statistic":0,"inputDecimalPlaces":2,"tableCalculationDecimalPlaces":1}""");
        using var decimalValues = JsonDocument.Parse(
            """{"tableData":[["24.04"],["24.06"]]}""");

        Assert.Equal("5.5", FormulaResponseInputResolver.ResolveValue(
            values.RootElement, strictInput.RootElement, "standardInput.wt"));
        Assert.Null(FormulaResponseInputResolver.ResolveValue(
            values.RootElement, rejectedInput.RootElement, "standardInput.wt"));
        Assert.Equal("24.1", FormulaResponseInputResolver.ResolveValue(
            decimalValues.RootElement, roundedAverage.RootElement, "sample1Avg"));
    }

    [Fact]
    public void RejectsMalformedTableCellsInsteadOfSilentlyDroppingThem()
    {
        using var values = JsonDocument.Parse(
            """{"tableData":[["24.00"],["invalid"]]}""");
        using var average = JsonDocument.Parse(
            """{"sourceType":3,"reference":"table:column:0","statistic":0,"inputDecimalPlaces":2,"tableCalculationDecimalPlaces":2}""");

        Assert.Null(FormulaResponseInputResolver.ResolveValue(
            values.RootElement, average.RootElement, "sample1Avg"));
    }

    [Theory]
    [InlineData("{\"tableData\":[[\"24.00\"],[true]]}")]
    [InlineData("{\"tableData\":[[\"24.00\"],[]]}")]
    public void RejectsStructurallyInvalidTableCells(string payload)
    {
        using var values = JsonDocument.Parse(payload);
        using var average = JsonDocument.Parse(
            """{"sourceType":3,"reference":"table:column:0","statistic":0}""");

        Assert.Null(FormulaResponseInputResolver.ResolveValue(
            values.RootElement, average.RootElement, "sample1Avg"));
    }

    [Fact]
    public void CalculatedCellsOverrideRawValuesWithoutMutatingEvidence()
    {
        using var values = JsonDocument.Parse(Values);
        using var source = JsonDocument.Parse(
            """{"sourceType":3,"reference":"table:column:1","statistic":0}""");
        var calculated = new Dictionary<(int, int), string>
        {
            [(0, 1)] = "2.50", [(1, 1)] = "5.00", [(2, 1)] = "7.50"
        };

        var result = FormulaResponseInputResolver.ResolveValue(
            values.RootElement, source.RootElement, "sample1Avg", calculated);

        Assert.Equal("5", result);
        Assert.Contains("20", values.RootElement.GetRawText());
    }

    [Fact]
    public async Task PreprocessorBatchesRawCellsAndMapsAuthoritativeOutputs()
    {
        using var values = JsonDocument.Parse(Values);
        var definition = """
            {"worksheetPolicy":{"preprocessor":{
              "definitionHash":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              "definition":{"formulaLanguageVersion":"oryx-formula-v1","numericPolicyVersion":"oryx-decimal-v1-half-up"},
              "configurationHash":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
              "configuration":{"placementKey":"worksheet-preprocessor"},
              "inputs":[{"variableKey":"raw","row":0,"column":0}],
              "outputs":[{"resultKey":"derived","row":0,"column":1}]
            }}}
            """;

        var result = await FormulaWorksheetPreprocessor.ResolveAsync(
            values.RootElement, definition, new WorksheetFormulaClient(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("20.00", result.Value[(0, 1)]);
    }

    private sealed class WorksheetFormulaClient : IFormulaCalculationClient
    {
        public Task<Result<FormulaServiceResponse>> EvaluateAsync(
            JsonElement request, CancellationToken cancellationToken = default)
        {
            Assert.Equal("10", request.GetProperty("resolvedInputs")
                .GetProperty("raw").GetString());
            var evaluation = JsonSerializer.SerializeToElement(new
            {
                status = 0,
                results = new[] { new { key = "derived", roundedResult = "20.00" } }
            });
            return Task.FromResult(Result.Success(new FormulaServiceResponse(
                "v1", 1, "test", 5, "COMPLETED", "engine", new string('c', 64),
                new string('a', 64), new string('a', 64), new string('b', 64),
                new string('b', 64), null, "worksheet-preprocessor", null,
                [], [], [], [], evaluation, [])));
        }

        public Task<Result<FormulaServiceResponse>> ValidateAsync(
            JsonElement request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result<bool>> IsReadyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success(true));
    }
}
