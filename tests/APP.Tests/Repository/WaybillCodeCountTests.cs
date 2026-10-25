using APP.Repository;
using DOMAIN.Entities.Shipments;
using Xunit;

namespace APP.Tests.Repository;
public class WaybillCodeCountTests
{
    [Fact]
    public async Task Waybill_UsesSharedLogisticsStorageAndIncludesArchivedNumbers()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        context.ShipmentDocuments.AddRange(
            new ShipmentDocument { Id = Guid.NewGuid(), Code = "WB-001", Type = DocType.Waybill },
            new ShipmentDocument { Id = Guid.NewGuid(), Code = "WB-002", Type = DocType.Waybill, DeletedAt = DateTime.UtcNow },
            new ShipmentDocument { Id = Guid.NewGuid(), Code = "WB-003", Type = DocType.Shipment },
            new ShipmentDocument { Id = Guid.NewGuid(), Code = "SD-001", Type = DocType.Shipment });
        await context.SaveChangesAsync();
        var repository = new ConfigurationRepository(context, null!);
        var waybill = await repository.GetCountForCodeConfiguration("Waybill", "WB-");
        Assert.True(waybill.IsSuccess); Assert.Equal(3, waybill.Value);
        var shared = await repository.GetCountForCodeConfiguration(nameof(ShipmentDocument), "WB-");
        Assert.True(shared.IsSuccess); Assert.Equal(waybill.Value, shared.Value);
        var fresh = await repository.GetCountForCodeConfiguration("Waybill", "NEW-");
        Assert.True(fresh.IsSuccess); Assert.Equal(0, fresh.Value);
    }
}
