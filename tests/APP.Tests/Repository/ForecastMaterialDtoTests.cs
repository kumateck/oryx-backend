using DOMAIN.Entities.ProductionSchedules;
using Xunit;

namespace APP.Tests.Repository;

public class ForecastMaterialDtoTests
{
    [Fact]
    public void IsAvailable_FullyExpiredStock_IsFalseEvenWhenQuantityOnHandExceedsNeeded()
    {
        var dto = new ForecastMaterialDto
        {
            QuantityOnHand = 31_778_000m,
            ExpiredQuantity = 31_778_000m,
            QuantityNeeded = 1_830_000m,
        };

        Assert.False(dto.IsAvailable);
    }

    [Fact]
    public void IsAvailable_PartiallyExpiredStock_UsesOnlyUsableQuantity()
    {
        var dto = new ForecastMaterialDto
        {
            QuantityOnHand = 500m,
            ExpiredQuantity = 200m,
            QuantityNeeded = 250m,
        };

        // Usable = 500 - 200 = 300, which exceeds 250
        Assert.True(dto.IsAvailable);
    }

    [Fact]
    public void IsAvailable_NoExpiredStock_MatchesRawQuantityComparison()
    {
        var dto = new ForecastMaterialDto
        {
            QuantityOnHand = 400m,
            ExpiredQuantity = 0m,
            QuantityNeeded = 300m,
        };

        Assert.True(dto.IsAvailable);
    }
}
