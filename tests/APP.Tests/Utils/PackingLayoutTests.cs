using DOMAIN.Entities.Products;
using Xunit;

namespace APP.Tests.Utils;

public class PackingLayoutTests
{
    // 80 X 20 X 10: Shipper 80 → Outer Carton 20 → Inner Carton 10 → Tablet.
    private static readonly List<decimal> Units = [80m, 20m, 10m];

    [Theory]
    [InlineData(80, 200)]
    [InlineData(1600, 10)]
    [InlineData(16000, 1)]
    public void Packs_per_shipper_picks_the_pack_and_its_base_units(int packsPerShipper, int perPack) =>
        Assert.Equal(perPack, PackingLayout.BaseUnitsPerPack(Units, packsPerShipper));

    [Fact]
    public void Units_that_never_add_up_give_no_pack()
    {
        Assert.Null(PackingLayout.BaseUnitsPerPack(Units, 75));
        Assert.Null(PackingLayout.BaseUnitsPerPack(Units, 0));
        Assert.Equal(1m, PackingLayout.BaseUnitsPerPack(new List<decimal>(), 24));
    }

    [Fact]
    public void Units_are_read_in_their_order()
    {
        var lists = new List<ProductPackingList>
        {
            new() { Quantity = 10, Order = 3 }, new() { Quantity = 80, Order = 1 }, new() { Quantity = 20, Order = 2 },
        };
        Assert.Equal(200m, PackingLayout.BaseUnitsPerPack(lists, 80));
    }

    [Fact]
    public void Base_units_become_packs_then_shippers_and_loose()
    {
        Assert.Equal((5000m, 50m), PackingLayout.ToPacks(1_000_050, 200));
        Assert.Equal((62m, 40m), PackingLayout.ShipperSplit(5000, 80));
    }
}
