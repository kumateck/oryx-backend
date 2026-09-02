using APP.Utils;
using Xunit;

namespace APP.Tests.Utils;

public class SpecificationReferenceHelperTests
{
    [Theory]
    [InlineData("In House")]
    [InlineData("In-house")]
    [InlineData("IN HOUSE")]
    public void FormatReference_LeavesInHouseUntouched(string input)
    {
        var result = SpecificationReferenceHelper.FormatReference(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void FormatReference_AppendsCurrentYear_WhenNoYearPresent()
    {
        var result = SpecificationReferenceHelper.FormatReference("BP");

        Assert.Equal($"BP {DateTime.UtcNow.Year}", result);
    }

    [Fact]
    public void FormatReference_DoesNotDoubleAppend_WhenAlreadyCurrentYear()
    {
        var currentYear = DateTime.UtcNow.Year;
        var result = SpecificationReferenceHelper.FormatReference($"USP {currentYear}");

        Assert.Equal($"USP {currentYear}", result);
    }

    [Fact]
    public void FormatReference_ReplacesStaleYear_WithCurrentYear()
    {
        var result = SpecificationReferenceHelper.FormatReference("BP 2025");

        Assert.Equal($"BP {DateTime.UtcNow.Year}", result);
    }

    [Fact]
    public void FormatReference_ReturnsNullOrWhitespace_Unchanged()
    {
        Assert.Null(SpecificationReferenceHelper.FormatReference(null));
        Assert.Equal("", SpecificationReferenceHelper.FormatReference(""));
    }
}
