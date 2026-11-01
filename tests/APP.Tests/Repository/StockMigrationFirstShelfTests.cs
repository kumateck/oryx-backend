using APP.Repository;
using DOMAIN.Entities.Warehouses;
using Xunit;

namespace APP.Tests.Repository;

public class StockMigrationFirstShelfTests
{
    private static WarehouseLocationRack Rack(string name, params string[] shelves) =>
        new() { Name = name, Shelves = shelves.Select(s => new WarehouseLocationShelf { Name = s }).ToList() };

    [Fact]
    public void Picks_the_first_shelf_of_the_first_rack_by_name_not_by_creation_order()
    {
        var store = new Warehouse
        {
            Name = "SYRUP Raw Warehouse",
            Locations = [new WarehouseLocation { Name = "General", Racks = [Rack("G/02", "G/02/A"), Rack("G/01", "G/01/B", "G/01/A", "G/01/C")] }],
        };

        Assert.Equal("G/01/A", StockMigrationRepository.FirstShelf(store)?.Name);
    }

    [Fact]
    public void Gives_no_shelf_when_the_store_has_none() =>
        Assert.Null(StockMigrationRepository.FirstShelf(new Warehouse { Locations = [new WarehouseLocation { Racks = [Rack("G/01")] }] }));
}
