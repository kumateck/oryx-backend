using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class PipelineCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class MaterialPipelineQueryTests
{
    private static readonly DateTime Timestamp = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAsync_ReconcilesPartialDeliveryWithoutDoubleCounting()
    {
        await using var context = CreateContext();
        var ids = new PipelineIds();
        SeedPipeline(context, ids);
        await context.SaveChangesAsync();

        var rows = await MaterialPipelineQuery.GetAsync(context, ids.MaterialId);

        Assert.Equal(100m, rows.Sum(row => row.Quantity));
        AssertQuantity(rows, MaterialPipelineStage.PurchaseRequisition, 20m);
        AssertQuantity(rows, MaterialPipelineStage.Sourcing, 20m);
        AssertQuantity(rows, MaterialPipelineStage.PurchaseOrder, 20m);
        AssertQuantity(rows, MaterialPipelineStage.InTransit, 30m);
        AssertQuantity(rows, MaterialPipelineStage.WarehouseReceiving, 10m);
        Assert.All(rows, row => Assert.Equal(["Production"], row.Departments));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new PipelineCurrentUserService());
    }

    private static void SeedPipeline(ApplicationDbContext context, PipelineIds ids)
    {
        var uom = new UnitOfMeasure
        {
            Id = ids.UomId,
            Name = "Kilogram",
            Symbol = "kg",
            CreatedAt = Timestamp,
        };
        var supplier = new Supplier
        {
            Id = ids.SupplierId,
            Name = "Pipeline Supplier",
            CreatedAt = Timestamp,
        };

        context.AddRange(
            new Material
            {
                Id = ids.MaterialId,
                Code = "RMA014",
                Name = "Aerosil 200",
                CreatedAt = Timestamp,
            },
            uom,
            supplier,
            new Department
            {
                Id = ids.DepartmentId,
                Code = "PROD",
                Name = "Production",
                CreatedAt = Timestamp,
            },
            new Requisition
            {
                Id = ids.RequisitionId,
                Code = "REQ-1",
                RequisitionType = RequisitionType.Purchase,
                Status = RequestStatus.Sourced,
                DepartmentId = ids.DepartmentId,
                CreatedAt = Timestamp,
            },
            new RequisitionItem
            {
                Id = ids.RequisitionItemId,
                RequisitionId = ids.RequisitionId,
                MaterialId = ids.MaterialId,
                UoMId = ids.UomId,
                Quantity = 100m,
                QuantityReceived = 10m,
                Status = RequestStatus.Sourced,
                CreatedAt = Timestamp,
            },
            new SourceRequisition
            {
                Id = ids.SourceId,
                Code = "SRC-1",
                SupplierId = ids.SupplierId,
                CreatedAt = Timestamp,
            },
            new SourceRequisitionItem
            {
                Id = Guid.NewGuid(),
                SourceRequisitionId = ids.SourceId,
                RequisitionId = ids.RequisitionId,
                MaterialId = ids.MaterialId,
                UoMId = ids.UomId,
                Quantity = 80m,
                Source = ProcurementSource.Foreign,
                CreatedAt = Timestamp,
            },
            new PurchaseOrder
            {
                Id = ids.PurchaseOrderId,
                Code = "PO-1",
                SourceRequisitionId = ids.SourceId,
                SupplierId = ids.SupplierId,
                Status = PurchaseOrderStatus.PartiallyLinked,
                CreatedAt = Timestamp,
            },
            new PurchaseOrderItem
            {
                Id = ids.PurchaseOrderItemId,
                PurchaseOrderId = ids.PurchaseOrderId,
                MaterialId = ids.MaterialId,
                UoMId = ids.UomId,
                Quantity = 60m,
                QuantityInvoiced = 40m,
                CreatedAt = Timestamp,
            },
            new ShipmentInvoice
            {
                Id = ids.InvoiceId,
                Code = "INV-1",
                SupplierId = ids.SupplierId,
                CreatedAt = Timestamp,
            },
            new ShipmentInvoiceItem
            {
                Id = ids.InvoiceItemId,
                ShipmentInvoiceId = ids.InvoiceId,
                PurchaseOrderId = ids.PurchaseOrderId,
                MaterialId = ids.MaterialId,
                UoMId = ids.UomId,
                ExpectedQuantity = 50m,
                ReceivedQuantity = 40m,
                CreatedAt = Timestamp,
            },
            new ShipmentDocument
            {
                Id = Guid.NewGuid(),
                Code = "SHIP-1",
                ShipmentInvoiceId = ids.InvoiceId,
                Status = ShipmentStatus.InTransit,
                CreatedAt = Timestamp,
            },
            new DistributedRequisitionMaterial
            {
                Id = ids.ReceivingId,
                ShipmentInvoiceId = ids.InvoiceId,
                MaterialId = ids.MaterialId,
                UoMId = ids.UomId,
                Quantity = 10m,
                Status = DistributedRequisitionMaterialStatus.Pending,
                CreatedAt = Timestamp,
            },
            new MaterialItemDistribution
            {
                Id = Guid.NewGuid(),
                DistributedRequisitionMaterialId = ids.ReceivingId,
                ShipmentInvoiceItemId = ids.InvoiceItemId,
                Quantity = 10m,
            }
        );
    }

    private static void AssertQuantity(
        IEnumerable<MaterialPipelineStockDto> rows,
        MaterialPipelineStage stage,
        decimal expected
    ) => Assert.Equal(expected, Assert.Single(rows, row => row.Stage == stage).Quantity);

    private sealed class PipelineIds
    {
        public Guid MaterialId { get; } = Guid.NewGuid();
        public Guid UomId { get; } = Guid.NewGuid();
        public Guid SupplierId { get; } = Guid.NewGuid();
        public Guid DepartmentId { get; } = Guid.NewGuid();
        public Guid RequisitionId { get; } = Guid.NewGuid();
        public Guid RequisitionItemId { get; } = Guid.NewGuid();
        public Guid SourceId { get; } = Guid.NewGuid();
        public Guid PurchaseOrderId { get; } = Guid.NewGuid();
        public Guid PurchaseOrderItemId { get; } = Guid.NewGuid();
        public Guid InvoiceId { get; } = Guid.NewGuid();
        public Guid InvoiceItemId { get; } = Guid.NewGuid();
        public Guid ReceivingId { get; } = Guid.NewGuid();
    }
}
