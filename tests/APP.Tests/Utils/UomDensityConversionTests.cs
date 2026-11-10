using SHARED;
using Xunit;

namespace APP.Tests.Utils;

public class UomDensityConversionTests
{
    [Theory]
    [InlineData(1_000, "mg", "ml", 1.35, 0.7407407407407407407407407407)]
    [InlineData(1_000, "mg", "ml", 1.56, 0.6410256410256410256410256410)]
    [InlineData(1, "g", "ml", 1.35, 0.7407407407407407407407407407)]
    [InlineData(1, "kg", "l", 1.35, 0.7407407407407407407407407407)]
    [InlineData(1, "ml", "mg", 1.35, 1_350)]
    [InlineData(1, "l", "kg", 1.35, 1.35)]
    public void Converts_mass_and_volume_using_configured_density(decimal quantity, string from, string to,
        decimal density, decimal expected)
    {
        var actual = UomConverter.ConvertWithDensity(quantity, from, to, density);
        Assert.True(actual.HasValue);
        Assert.Equal(expected, actual.GetValueOrDefault(), 12);
    }

    [Theory]
    [InlineData("mg", "g")]
    [InlineData("kg", "piece")]
    [InlineData("piece", "ml")]
    public void Rejects_same_dimension_and_unsupported_unit_pairs(string from, string to)
    {
        Assert.Null(UomConverter.ConvertWithDensity(1, from, to, 1.4m));
    }

    [Fact]
    public void Rejects_missing_or_nonpositive_density()
    {
        Assert.Null(UomConverter.ConvertWithDensity(1, "mg", "ml", 0));
        Assert.Null(UomConverter.ConvertWithDensity(1, "mg", "ml", -1));
    }
}
