using APP.Services.Grns;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace APP.Tests.Repository;

public class GrnListQueryTests
{
    [Fact]
    public void Page_and_batched_supplier_projections_compile_for_postgres_without_list_unrelated_graphs()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused")
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning)).Options,
            new GrnListFixture.Actor(Guid.NewGuid()));
        var filtered = GrnListQueries.Filter(context, "WHD", MaterialKind.Raw, DOMAIN.Entities.Grns.Status.Pending);
        var headers = GrnListQueries.Page(filtered, 1, 50).ToQueryString();
        var batches = GrnListQueries.Batches(context, [Guid.NewGuid(), Guid.NewGuid()]).ToQueryString();
        foreach (var sql in new[] { headers, batches })
        {
            Assert.Contains("GRN list:", sql);
            foreach (var forbidden in new[] { "PasswordHash", "SecurityStamp", "WarehouseLocationRacks",
                "WarehouseLocationShelves", "MaterialBatchEvents", "ReservedQuantities", "ShelfMaterialBatches",
                "MaterialSamplings", "FormAssignees", "MaterialSpecifications", "split-query" })
                Assert.DoesNotContain(forbidden, sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DeletedAt", sql);
        }
        Assert.Contains("DepartmentId", headers); // Scalar user join remains necessary for authorization scope.
        Assert.Contains("EXISTS", headers);
        Assert.Contains("LIMIT", headers);
        Assert.Contains("ORDER BY", headers);
        Assert.Contains("Suppliers", batches);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
