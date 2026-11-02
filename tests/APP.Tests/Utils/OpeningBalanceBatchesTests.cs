using APP.Services.Materials;
using Xunit;

namespace APP.Tests.Utils;

public class OpeningBalanceBatchesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Missing_supplier_batches_keep_each_source_row_separate(string batch)
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        Assert.NotEqual(OpeningBalanceBatches.Identity(first, batch), OpeningBalanceBatches.Identity(second, batch));
        Assert.Equal((string.Empty, first), OpeningBalanceBatches.Identity(first, batch));
    }

    [Fact]
    public void Known_supplier_batches_group_case_insensitively_across_source_rows()
    {
        Assert.Equal(OpeningBalanceBatches.Identity(Guid.NewGuid(), " lot-42 "),
            OpeningBalanceBatches.Identity(Guid.NewGuid(), "LOT-42"));
        Assert.NotEqual(OpeningBalanceBatches.Identity(Guid.NewGuid(), "LOT-42"),
            OpeningBalanceBatches.Identity(Guid.NewGuid(), "LOT-43"));
    }
}
