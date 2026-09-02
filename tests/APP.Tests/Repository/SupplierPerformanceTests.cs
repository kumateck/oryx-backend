using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;
using Xunit;

namespace APP.Tests.Repository;

public class SupplierPerformanceTests
{
    private static readonly DateTime PeriodStart = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ComputePerformance_UsesOnTimeShipmentsAndRejectedBatches()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Measured Supplier" };
        context.Suppliers.Add(supplier);
        AddShipment(context, supplier.Id, PeriodStart.AddDays(5), PeriodStart.AddDays(5));
        AddShipment(context, supplier.Id, PeriodStart.AddDays(10), PeriodStart.AddDays(12));
        AddBatches(context, supplier.Id);
        await context.SaveChangesAsync();
        var repository = SupplierRelationshipTestContext.CreateRepository(context);

        var result = await repository.ComputePerformance(supplier.Id, new ComputeSupplierPerformanceRequest
        {
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodStart.AddMonths(1).AddDays(-1),
            OnTimeDeliveryWeight = 0.6m,
            QualityWeight = 0.4m,
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.EvaluatedDeliveries);
        Assert.Equal(1, result.Value.OnTimeDeliveries);
        Assert.Equal(50m, result.Value.OnTimeDeliveryRate);
        Assert.Equal(2, result.Value.EvaluatedBatches);
        Assert.Equal(1, result.Value.RejectedBatches);
        Assert.Equal(50m, result.Value.QualityRejectRate);
        Assert.Equal(50m, result.Value.Score);
    }

    private static void AddShipment(
        INFRASTRUCTURE.Context.ApplicationDbContext context,
        Guid supplierId,
        DateTime expected,
        DateTime arrived)
    {
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(), SupplierId = supplierId, ExpectedDeliveryDate = expected,
        };
        var invoice = new ShipmentInvoice
        {
            Id = Guid.NewGuid(), SupplierId = supplierId,
            Items =
            [
                new ShipmentInvoiceItem
                {
                    Id = Guid.NewGuid(), PurchaseOrderId = order.Id, PurchaseOrder = order,
                    MaterialId = Guid.NewGuid(), UoMId = Guid.NewGuid(), ManufacturerId = Guid.NewGuid(),
                },
            ],
        };
        context.Add(new ShipmentDocument
        {
            Id = Guid.NewGuid(), ShipmentInvoiceId = invoice.Id,
            ShipmentInvoice = invoice, ArrivedAt = arrived,
        });
    }

    private static void AddBatches(
        INFRASTRUCTURE.Context.ApplicationDbContext context, Guid supplierId)
    {
        var distributed = new DistributedRequisitionMaterial { Id = Guid.NewGuid() };
        var checklist = new Checklist
        {
            Id = Guid.NewGuid(), SupplierId = supplierId,
            DistributedRequisitionMaterialId = distributed.Id,
            DistributedRequisitionMaterial = distributed,
        };
        var acceptedMaterial = new Material { Id = Guid.NewGuid(), Name = "Accepted Material" };
        var rejectedMaterial = new Material { Id = Guid.NewGuid(), Name = "Rejected Material" };
        context.AddRange(
            checklist,
            distributed,
            acceptedMaterial,
            rejectedMaterial,
            new MaterialBatch
            {
                Id = Guid.NewGuid(), ChecklistId = checklist.Id, Checklist = checklist,
                MaterialId = acceptedMaterial.Id, Material = acceptedMaterial, UoMId = Guid.NewGuid(),
                DateReceived = PeriodStart.AddDays(3), Status = BatchStatus.Available,
            },
            new MaterialBatch
            {
                Id = Guid.NewGuid(), ChecklistId = checklist.Id, Checklist = checklist,
                MaterialId = rejectedMaterial.Id, Material = rejectedMaterial, UoMId = Guid.NewGuid(),
                DateReceived = PeriodStart.AddDays(4), Status = BatchStatus.Rejected,
                DateRejected = PeriodStart.AddDays(8),
            }
        );
    }
}
