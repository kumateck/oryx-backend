using APP.Services.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// The local formula evaluator, exercised against the real formulas documented in the field
/// catalog rather than invented ones.
/// </summary>
public class QcFormulaEvaluatorTests
{
    private sealed class Resolver(
        Dictionary<string, double> scalars,
        Dictionary<string, IReadOnlyList<double>> columns) : IQcFormulaValueResolver
    {
        public bool TryGetScalar(string fieldKey, out double value) =>
            scalars.TryGetValue(fieldKey, out value);

        public bool TryGetColumn(string tableFieldKey, string columnKey, out IReadOnlyList<double> values) =>
            columns.TryGetValue($"{tableFieldKey}.{columnKey}", out values);
    }

    private static Resolver With(
        Dictionary<string, double> scalars = null,
        Dictionary<string, IReadOnlyList<double>> columns = null) =>
        new(scalars ?? [], columns ?? []);

    [Fact]
    public void Parses_and_evaluates_the_real_cfu_formula()
    {
        // From the Amoxicillin finished-product worksheet: average count, then result.
        var resolver = With(new Dictionary<string, double> { ["plate1"] = 30, ["plate2"] = 40 });

        Assert.True(QcFormulaEvaluator.TryEvaluate(
            "({plate1} + {plate2}) / 2", resolver, out var average, out _));
        Assert.Equal(35, average);

        var second = With(new Dictionary<string, double>
        {
            ["average_count"] = 35, ["dilution_factor"] = 10
        });

        Assert.True(QcFormulaEvaluator.TryEvaluate(
            "{average_count} * {dilution_factor}", second, out var result, out _));
        Assert.Equal(350, result);
    }

    [Fact]
    public void Evaluates_the_real_dissolution_formula_with_table_aggregates()
    {
        var resolver = With(
            new Dictionary<string, double> { ["std_conc"] = 10, ["spl_conc"] = 20 },
            new Dictionary<string, IReadOnlyList<double>>
            {
                ["dissolution_table.abs_spl"] = new List<double> { 0.40, 0.42, 0.44, 0.46, 0.48, 0.50 },
                ["dissolution_table.abs_std"] = new List<double> { 0.45, 0.45, 0.45, 0.45, 0.45, 0.45 }
            });

        var formula = "AVG({dissolution_table.abs_spl}) / AVG({dissolution_table.abs_std}) * "
                      + "{std_conc} / {spl_conc} * 100";

        Assert.True(QcFormulaEvaluator.TryEvaluate(formula, resolver, out var value, out var error));
        Assert.Null(error);
        Assert.Equal(50d, value, 6);
    }

    [Theory]
    [InlineData("SUM({t.c})", 10)]
    [InlineData("MIN({t.c})", 1)]
    [InlineData("MAX({t.c})", 4)]
    [InlineData("AVG({t.c})", 2.5)]
    public void Supports_each_aggregate_function(string formula, double expected)
    {
        var resolver = With(columns: new Dictionary<string, IReadOnlyList<double>>
        {
            ["t.c"] = new List<double> { 1, 2, 3, 4 }
        });

        Assert.True(QcFormulaEvaluator.TryEvaluate(formula, resolver, out var value, out _));
        Assert.Equal(expected, value, 6);
    }

    [Fact]
    public void Rsd_is_percent_relative_standard_deviation()
    {
        var resolver = With(columns: new Dictionary<string, IReadOnlyList<double>>
        {
            ["t.c"] = new List<double> { 10, 12, 14 }
        });

        Assert.True(QcFormulaEvaluator.TryEvaluate("RSD({t.c})", resolver, out var value, out _));

        // mean 12, sample sd 2 -> 16.666...%
        Assert.Equal(16.6666666, value, 5);
    }

    [Fact]
    public void Honours_operator_precedence_and_parentheses()
    {
        var resolver = With(new Dictionary<string, double> { ["a"] = 2, ["b"] = 3, ["c"] = 4 });

        Assert.True(QcFormulaEvaluator.TryEvaluate("{a} + {b} * {c}", resolver, out var flat, out _));
        Assert.Equal(14, flat);

        Assert.True(QcFormulaEvaluator.TryEvaluate("({a} + {b}) * {c}", resolver, out var grouped, out _));
        Assert.Equal(20, grouped);
    }

    [Fact]
    public void Reports_references_without_needing_values()
    {
        var analysis = QcFormulaEvaluator.Analyze(
            "AVG({tbl.col}) * {scale} + RSD({tbl.other})");

        Assert.True(analysis.IsValid);
        Assert.Equal(["scale"], analysis.ScalarFieldKeys);
        Assert.Equal(2, analysis.TableReferences.Count);
        Assert.Contains(analysis.TableReferences, item =>
            item.Function == "AVG" && item.TableFieldKey == "tbl" && item.ColumnKey == "col");
        Assert.Contains(analysis.TableReferences, item =>
            item.Function == "RSD" && item.TableFieldKey == "tbl" && item.ColumnKey == "other");
    }

    [Theory]
    [InlineData("{a} +")]
    [InlineData("{a} + + ")]
    [InlineData("AVG({a})")]          // aggregate needs table.column
    [InlineData("NOPE({a.b})")]       // unknown function
    [InlineData("{a.b} * 2")]         // table column used as a scalar
    [InlineData("(({a}) ")]           // unbalanced parentheses
    [InlineData("")]
    public void Rejects_malformed_formulas(string formula)
    {
        Assert.False(QcFormulaEvaluator.Analyze(formula).IsValid);
    }

    [Fact]
    public void Division_by_zero_is_an_error_not_an_infinity()
    {
        var resolver = With(new Dictionary<string, double> { ["a"] = 1, ["b"] = 0 });

        Assert.False(QcFormulaEvaluator.TryEvaluate("{a} / {b}", resolver, out _, out var error));
        Assert.Contains("divides by zero", error);
    }

    [Fact]
    public void Missing_value_is_reported_clearly()
    {
        Assert.False(QcFormulaEvaluator.TryEvaluate("{a} + 1", With(), out _, out var error));
        Assert.Contains("'a'", error);
    }

    [Fact]
    public void Rsd_of_a_single_value_is_refused()
    {
        var resolver = With(columns: new Dictionary<string, IReadOnlyList<double>>
        {
            ["t.c"] = new List<double> { 5 }
        });

        Assert.False(QcFormulaEvaluator.TryEvaluate("RSD({t.c})", resolver, out _, out var error));
        Assert.Contains("at least two values", error);
    }
}
