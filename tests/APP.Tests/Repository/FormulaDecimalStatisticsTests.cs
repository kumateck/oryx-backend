using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public sealed class FormulaDecimalStatisticsTests
{
    [Theory]
    [InlineData(0, "12")]
    [InlineData(1, "2")]
    [InlineData(2, "1.63")]
    [InlineData(3, "16.67")]
    [InlineData(4, "10")]
    [InlineData(5, "14")]
    [InlineData(6, "3")]
    public void Calculates_statistics_with_controlled_decimal_output(
        int statistic, string expected)
    {
        var actual = FormulaDecimalStatistics.Calculate(
            ["10", "12", "14"], statistic, 2, 2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Applies_half_away_from_zero_at_decimal_boundary()
    {
        var positive = FormulaDecimalStatistics.Calculate(
            ["24.04", "24.05"], 0, 2, 2);
        var negative = FormulaDecimalStatistics.Calculate(
            ["-24.04", "-24.05"], 0, 2, 2);

        Assert.Equal("24.05", positive);
        Assert.Equal("-24.05", negative);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1.234")]
    public void Rejects_invalid_or_excess_scale_cells(string invalid)
    {
        var actual = FormulaDecimalStatistics.Calculate(
            ["1.00", invalid], 0, 2, 2);

        Assert.Null(actual);
    }

    [Fact]
    public void Rejects_rsd_when_mean_is_zero()
    {
        Assert.Null(FormulaDecimalStatistics.Calculate(
            ["-1", "1"], 3, 2, 2));
    }
}
