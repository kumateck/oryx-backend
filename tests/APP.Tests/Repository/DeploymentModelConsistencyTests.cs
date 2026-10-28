using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class DeploymentModelCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public sealed class DeploymentModelConsistencyTests
{
    [Fact]
    public void NpgsqlModelMatchesTheCommittedMigrationSnapshot()
    {
        using var context = CreateContext();
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public void ShipmentInvoiceLineCanHaveMultipleDiscrepancyItems()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ShipmentDiscrepancyItem));
        Assert.NotNull(entity);
        var index = Assert.Single(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(ShipmentDiscrepancyItem.ShipmentInvoiceItemId)]));
        Assert.False(index.IsUnique);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=offline_model_check;Username=unused")
            .Options,
        new DeploymentModelCurrentUser());
}
