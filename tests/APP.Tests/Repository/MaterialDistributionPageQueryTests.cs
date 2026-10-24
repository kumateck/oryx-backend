using APP.Services.Procurement;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialDistributionPageQueryTests
{
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "Production";
    }

    [Fact]
    public void Both_batched_projections_compile_to_single_postgres_queries_without_unrelated_graphs()
    {
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused")
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .Options, new CurrentUser());
        var sqlQueries = new[] {
            MaterialDistributionPageQueries.InvoiceLines(context, Guid.NewGuid()).ToQueryString(),
            MaterialDistributionPageQueries.Requests(context, Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]).ToQueryString()
        };
        foreach (var sql in sqlQueries)
        {
            Assert.Contains("Material distribution page:", sql);
            foreach (var forbidden in new[] { "PasswordHash", "users", "Warehouses", "WarehouseLocations",
                "MaterialBatches", "MaterialBatchEvents", "ReservedQuantities", "MaterialSpecifications",
                "PurchaseOrderItems", "RevisedPurchaseOrders", "split-query" })
                Assert.DoesNotContain(forbidden, sql, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Contains("CreatedAt", sqlQueries[0]); // FIFO, not the creator entity.
        Assert.Contains("Departments", sqlQueries[1]);
        Assert.Contains("EXISTS", sqlQueries[1]); // Deduplicate eligibility without multiplying requisitions.
        Assert.Contains("DeletedAt", sqlQueries[1]);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
