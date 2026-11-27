using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>"Percentage" reports a fraction in percent, as Oryx Next showed it, so the compiled result is the expression × 100.</summary>
public sealed class LegacyFormulaPercentageTests
{
    private const string Assay = """
        {"type":3,"expression":"(:sampleAvg * :standardWeight * :Fillweight) / (:standardAvg * :sampleWeight * :Claim) * :purity",
         "numberOfReadings":5,"numberOfSamples":2,"standardInTable":true,"headers":["Standard","Sample 1","Sample 2"],
         "standardInputs":[{"key":"weight"}],"sampleInputs":[[{"key":"weight"}],[{"key":"weight"}]],
         "manualVarLabels":{"Fillweight":"Fill Weight","Claim":"Claim","purity":"Purity"},PERCENT}
        """;

    [Theory]
    [InlineData("\"percentageResults\":true", true)]
    [InlineData("\"percent\":true", true)]
    [InlineData("\"percentageResults\":false", false)]
    public void Percentage_results_are_the_expression_times_100(string setting, bool scaled)
    {
        var (definition, _) = LegacyFormulaRepairDefinition.Compile(Assay.Replace("PERCENT", setting));
        foreach (var result in definition.GetProperty("results").EnumerateArray())
        {
            var expression = result.GetProperty("expression").GetString()!;
            Assert.Equal(scaled, expression.StartsWith('(') && expression.EndsWith(") * 100", StringComparison.Ordinal));
            Assert.Equal(scaled ? 2 : 0, result.GetProperty("numericScale").GetInt32());
        }
    }

    [Fact]
    public void Switching_percentage_on_changes_the_definition_hash()
    {
        var (_, on) = LegacyFormulaRepairDefinition.Compile(Assay.Replace("PERCENT", "\"percentageResults\":true"));
        var (_, off) = LegacyFormulaRepairDefinition.Compile(Assay.Replace("PERCENT", "\"percentageResults\":false"));
        Assert.NotEqual(on, off);
    }
}
