using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialPipelineBuilderTests
{
    private static readonly DateTime Timestamp = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_ReportsOnlyTheQuantityRemainingAtEachStage()
    {
        var ids = new PipelineIds();
        var uom = CreateUom(ids.UomId);

        var rows = MaterialPipelineBuilder.Build(
            [new(Guid.NewGuid(), ids.RequisitionId, "REQ-1", ids.UomId, uom, 100m, null, Timestamp)],
            [new(ids.SourceId, ids.RequisitionId, "SRC-1", ids.UomId, uom, 80m,
                ProcurementSource.Foreign, "Supplier", Timestamp)],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.SourceId, "PO-1", ids.UomId, uom,
                60m, "Supplier", null, PurchaseOrderStatus.Linked, Timestamp)],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.InvoiceId, "INV-1", ids.UomId, uom,
                40m, 10m, "Supplier", null, Timestamp)],
            new Dictionary<Guid, PipelineShipmentStatus>
            {
                [ids.InvoiceId] = new(ids.InvoiceId, ShipmentStatus.InTransit, Timestamp),
            },
            [new(Guid.NewGuid(), ids.InvoiceId, "INV-1", uom, 10m, "Supplier",
                DistributedRequisitionMaterialStatus.Checked, Timestamp)],
            [new(ids.SourceId, ids.UomId)],
            new Dictionary<Guid, string> { [ids.RequisitionId] = "Production" }
        );

        Assert.Equal(100m, rows.Sum(row => row.Quantity));
        AssertQuantity(rows, MaterialPipelineStage.PurchaseRequisition, 20m);
        AssertQuantity(rows, MaterialPipelineStage.Quotation, 20m);
        AssertQuantity(rows, MaterialPipelineStage.PurchaseOrder, 20m);
        AssertQuantity(rows, MaterialPipelineStage.InTransit, 30m);
        AssertQuantity(rows, MaterialPipelineStage.QualityCheck, 10m);

        Assert.All(rows, row => Assert.Equal(["Production"], row.Departments));
    }

    [Fact]
    public void Build_GroupsDuplicateRequisitionAndPurchaseOrderLinesBeforeSubtracting()
    {
        var ids = new PipelineIds();
        var uom = CreateUom(ids.UomId);

        var rows = MaterialPipelineBuilder.Build(
            [
                new(Guid.NewGuid(), ids.RequisitionId, "REQ-1", ids.UomId, uom, 60m, null, Timestamp),
                new(Guid.NewGuid(), ids.RequisitionId, "REQ-1", ids.UomId, uom, 40m, null, Timestamp),
            ],
            [new(ids.SourceId, ids.RequisitionId, "SRC-1", ids.UomId, uom, 70m,
                ProcurementSource.Local, "Supplier", Timestamp)],
            [
                new(Guid.NewGuid(), ids.PurchaseOrderId, ids.SourceId, "PO-1", ids.UomId, uom,
                    30m, "Supplier", null, PurchaseOrderStatus.PartiallyLinked, Timestamp),
                new(Guid.NewGuid(), ids.PurchaseOrderId, ids.SourceId, "PO-1", ids.UomId, uom,
                    20m, "Supplier", null, PurchaseOrderStatus.PartiallyLinked, Timestamp),
            ],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.InvoiceId, "INV-1", ids.UomId, uom,
                20m, 0m, "Supplier", null, Timestamp)],
            [], [], [],
            new Dictionary<Guid, string> { [ids.RequisitionId] = "Production" }
        );

        Assert.Single(rows, row => row.Stage == MaterialPipelineStage.PurchaseRequisition);
        Assert.Single(rows, row => row.Stage == MaterialPipelineStage.PurchaseOrder);
        AssertQuantity(rows, MaterialPipelineStage.PurchaseRequisition, 30m);
        AssertQuantity(rows, MaterialPipelineStage.Sourcing, 20m);
        AssertQuantity(rows, MaterialPipelineStage.PurchaseOrder, 30m);
    }

    [Fact]
    public void Build_RemovesAQuantityAfterItIsFullyDistributedToShelfStock()
    {
        var ids = new PipelineIds();
        var uom = CreateUom(ids.UomId);

        var rows = MaterialPipelineBuilder.Build(
            [new(Guid.NewGuid(), ids.RequisitionId, "REQ-1", ids.UomId, uom, 50m, null, Timestamp)],
            [new(ids.SourceId, ids.RequisitionId, "SRC-1", ids.UomId, uom, 50m,
                ProcurementSource.Local, "Supplier", Timestamp)],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.SourceId, "PO-1", ids.UomId, uom,
                50m, "Supplier", null, PurchaseOrderStatus.Completed, Timestamp)],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.InvoiceId, "INV-1", ids.UomId, uom,
                50m, 50m, "Supplier", null, Timestamp)],
            [], [], [], []
        );

        Assert.Empty(rows);
    }

    [Fact]
    public void Build_CombinesDepartmentsFromEveryRequisitionFeedingAStage()
    {
        var ids = new PipelineIds();
        var uom = CreateUom(ids.UomId);
        var otherRequisitionId = Guid.NewGuid();

        var rows = MaterialPipelineBuilder.Build(
            [],
            [
                new(ids.SourceId, ids.RequisitionId, "SRC-1", ids.UomId, uom, 40m,
                    ProcurementSource.Local, "Supplier", Timestamp),
                new(ids.SourceId, otherRequisitionId, "SRC-1", ids.UomId, uom, 30m,
                    ProcurementSource.Local, "Supplier", Timestamp),
            ],
            [new(Guid.NewGuid(), ids.PurchaseOrderId, ids.SourceId, "PO-1", ids.UomId, uom,
                50m, "Supplier", null, PurchaseOrderStatus.Linked, Timestamp)],
            [], [], [], [],
            new Dictionary<Guid, string>
            {
                [ids.RequisitionId] = "Production",
                [otherRequisitionId] = "Packaging",
            }
        );

        var purchaseOrderRow = Assert.Single(
            rows, row => row.Stage == MaterialPipelineStage.PurchaseOrder
        );
        Assert.Equal(["Packaging", "Production"], purchaseOrderRow.Departments);
    }

    private static UnitOfMeasure CreateUom(Guid id) => new()
    {
        Id = id,
        Name = "Kilogram",
        Symbol = "kg",
        Description = "Kilogram",
        Category = UnitOfMeasureCategory.Weight,
        Type = UnitOfMeasureType.Raw,
        CreatedAt = Timestamp,
    };

    private static void AssertQuantity(
        IEnumerable<MaterialPipelineStockDto> rows,
        MaterialPipelineStage stage,
        decimal expected
    ) => Assert.Equal(expected, Assert.Single(rows, row => row.Stage == stage).Quantity);

    private sealed class PipelineIds
    {
        public Guid UomId { get; } = Guid.NewGuid();
        public Guid RequisitionId { get; } = Guid.NewGuid();
        public Guid SourceId { get; } = Guid.NewGuid();
        public Guid PurchaseOrderId { get; } = Guid.NewGuid();
        public Guid InvoiceId { get; } = Guid.NewGuid();
    }
}
