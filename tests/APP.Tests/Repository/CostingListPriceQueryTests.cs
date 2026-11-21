using APP.Repository;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class CostingListPriceQueryTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "NonProduction";
    }

    [Fact]
    public void Price_page_compiles_for_postgres_without_loading_the_product_storage_graph()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused")
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning)).Options, new Actor());
        var query = CostingRepository.ListPriceQuery(context, null, null, null);
        var sql = query.OrderBy(row => row.ProductName).ThenBy(row => row.ProductId).Take(30).ToQueryString();
        Assert.Contains("ProductPrices", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("DeletedAt", sql);
        foreach (var unrelated in new[] { "Warehouses", "WarehouseLocations", "WarehouseLocationRacks", "WarehouseLocationShelves",
            "Equipments", "Departments", "UnitOfMeasures", "ProductCategories", "LabelClaim", "PrimaryPackDescription" })
            Assert.DoesNotContain(unrelated, sql);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
