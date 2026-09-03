using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;

namespace APP.Repository;

internal static class MaterialPipelineBuilder
{
    public static List<MaterialPipelineStockDto> Build(
        List<PipelineRequisitionLine> requisitions,
        List<PipelineSourceLine> sources,
        List<PipelinePurchaseOrderLine> purchaseOrders,
        List<PipelineShipmentLine> shipments,
        Dictionary<Guid, PipelineShipmentStatus> shipmentStatuses,
        List<PipelineReceivingLine> receiving,
        HashSet<PipelineSourceQuote> quotedSources,
        Dictionary<Guid, string> departmentsByRequisition
    )
    {
        var result = receiving.Select(item =>
        {
            var shipment = shipments.FirstOrDefault(line => line.ShipmentInvoiceId == item.ShipmentInvoiceId);
            var po = purchaseOrders.FirstOrDefault(line => line.PurchaseOrderId == shipment?.PurchaseOrderId);
            return Create(
                item.Id, item.Reference, ReceivingStage(item.Status), item.Status.ToString(),
                item.Quantity, item.Uom, RouteFor(po, sources), item.SupplierName,
                po?.ExpectedDate, item.LastUpdatedAt,
                DepartmentsFor(po?.SourceRequisitionId, null, sources, departmentsByRequisition)
            );
        }).ToList();

        result.AddRange(shipments.Select(item =>
        {
            shipmentStatuses.TryGetValue(item.ShipmentInvoiceId, out var document);
            var po = purchaseOrders.FirstOrDefault(line => line.PurchaseOrderId == item.PurchaseOrderId);
            return Create(
                item.Id, item.Reference,
                document is null ? MaterialPipelineStage.Shipment : ShipmentStage(document.Status),
                document?.Status.ToString() ?? "Shipment created",
                item.Quantity - item.DistributedQuantity, item.Uom, RouteFor(po, sources),
                item.SupplierName, item.ExpectedDate,
                document?.LastUpdatedAt ?? item.LastUpdatedAt,
                DepartmentsFor(po?.SourceRequisitionId, null, sources, departmentsByRequisition)
            );
        }));

        result.AddRange(purchaseOrders
            .GroupBy(item => new { item.PurchaseOrderId, item.UomId })
            .Where(group => group.First().Status != PurchaseOrderStatus.Completed)
            .Select(group =>
            {
                var item = group.First();
                var shipped = shipments
                    .Where(line =>
                        line.PurchaseOrderId == item.PurchaseOrderId
                        && line.UomId == item.UomId
                    )
                    .Sum(line => line.Quantity);
                return Create(
                    item.PurchaseOrderId, item.Reference, MaterialPipelineStage.PurchaseOrder,
                    item.Status.ToString(), group.Sum(line => line.Quantity) - shipped,
                    item.Uom, RouteFor(item, sources), item.SupplierName,
                    item.ExpectedDate, group.Max(line => line.LastUpdatedAt),
                    DepartmentsFor(item.SourceRequisitionId, item.UomId, sources, departmentsByRequisition)
                );
            }));

        result.AddRange(sources
            .GroupBy(item => new { item.SourceRequisitionId, item.RequisitionId, item.UomId })
            .Select(group =>
            {
                var item = group.First();
                var ordered = purchaseOrders
                    .Where(line => line.SourceRequisitionId == item.SourceRequisitionId && line.UomId == item.UomId)
                    .Sum(line => line.Quantity);
                var expectedDate = requisitions
                    .FirstOrDefault(line => line.RequisitionId == item.RequisitionId)?.ExpectedDate;
                var quoted = quotedSources.Contains(
                    new PipelineSourceQuote(item.SourceRequisitionId, item.UomId)
                );
                return Create(
                    item.SourceRequisitionId, item.Reference,
                    quoted ? MaterialPipelineStage.Quotation : MaterialPipelineStage.Sourcing,
                    quoted ? "Quotation received" : "Supplier sourcing",
                    group.Sum(line => line.Quantity) - ordered, item.Uom, item.Route,
                    item.SupplierName, expectedDate, group.Max(line => line.LastUpdatedAt),
                    DepartmentNamesFor([item.RequisitionId], departmentsByRequisition)
                );
            }));

        result.AddRange(requisitions
            .GroupBy(item => new { item.RequisitionId, item.UomId })
            .Select(group =>
            {
                var item = group.First();
                var sourced = sources
                    .Where(line =>
                        line.RequisitionId == item.RequisitionId
                        && line.UomId == item.UomId
                    )
                    .Sum(line => line.Quantity);
                return Create(
                    item.RequisitionId, item.Reference,
                    MaterialPipelineStage.PurchaseRequisition, "Purchase requisition",
                    group.Sum(line => line.Quantity) - sourced,
                    item.Uom, null, null, item.ExpectedDate,
                    group.Max(line => line.LastUpdatedAt),
                    DepartmentNamesFor([item.RequisitionId], departmentsByRequisition)
                );
            }));

        return
        [
            .. result.Where(item => item.Quantity > 0)
                .OrderByDescending(item => item.Stage)
                .ThenBy(item => item.ExpectedAvailabilityDate)
                .ThenBy(item => item.Reference)
        ];
    }

    private static List<string> DepartmentsFor(
        Guid? sourceRequisitionId, Guid? uomId,
        List<PipelineSourceLine> sources,
        Dictionary<Guid, string> departmentsByRequisition
    ) => sourceRequisitionId is null
        ? []
        : DepartmentNamesFor(
            sources
                .Where(source =>
                    source.SourceRequisitionId == sourceRequisitionId
                    && (uomId is null || source.UomId == uomId)
                )
                .Select(source => source.RequisitionId),
            departmentsByRequisition
        );

    private static List<string> DepartmentNamesFor(
        IEnumerable<Guid> requisitionIds,
        Dictionary<Guid, string> departmentsByRequisition
    ) =>
    [
        .. requisitionIds
            .Select(departmentsByRequisition.GetValueOrDefault)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .OrderBy(name => name)
    ];

    private static ProcurementSource? RouteFor(
        PipelinePurchaseOrderLine item,
        List<PipelineSourceLine> sources
    ) => item is null
        ? null
        : sources.FirstOrDefault(source =>
            source.SourceRequisitionId == item.SourceRequisitionId
            && source.UomId == item.UomId
        )?.Route;

    private static MaterialPipelineStockDto Create(
        Guid id, string reference, MaterialPipelineStage stage, string status,
        decimal quantity, UnitOfMeasure uom, ProcurementSource? route,
        string supplierName, DateTime? expectedDate, DateTime lastUpdatedAt,
        List<string> departments
    ) => new()
    {
        Id = id,
        Reference = reference,
        Stage = stage,
        Status = status,
        Quantity = Math.Max(quantity, 0),
        Uom = new UnitOfMeasureDto
        {
            Id = uom.Id,
            Name = uom.Name,
            Symbol = uom.Symbol,
            Description = uom.Description,
            IsScalable = uom.IsScalable,
            IsRawMaterial = uom.IsRawMaterial,
            Type = uom.Type,
            Category = uom.Category,
            CreatedAt = uom.CreatedAt,
        },
        Route = route,
        SupplierName = supplierName,
        ExpectedAvailabilityDate = expectedDate,
        LastUpdatedAt = lastUpdatedAt,
        Departments = departments,
    };

    private static MaterialPipelineStage ShipmentStage(ShipmentStatus status) => status switch
    {
        ShipmentStatus.AtPort => MaterialPipelineStage.AtPort,
        ShipmentStatus.Cleared => MaterialPipelineStage.Cleared,
        ShipmentStatus.InTransit => MaterialPipelineStage.InTransit,
        ShipmentStatus.Arrived => MaterialPipelineStage.Arrived,
        _ => MaterialPipelineStage.Shipment,
    };

    private static MaterialPipelineStage ReceivingStage(
        DistributedRequisitionMaterialStatus status
    ) => status switch
    {
        DistributedRequisitionMaterialStatus.Checked => MaterialPipelineStage.QualityCheck,
        DistributedRequisitionMaterialStatus.GrnGenerated => MaterialPipelineStage.GrnGenerated,
        _ => MaterialPipelineStage.WarehouseReceiving,
    };
}

internal sealed record PipelineRequisitionLine(
    Guid Id, Guid RequisitionId, string Reference, Guid? UomId, UnitOfMeasure Uom,
    decimal Quantity, DateTime? ExpectedDate, DateTime LastUpdatedAt
);
internal sealed record PipelineSourceLine(
    Guid SourceRequisitionId, Guid RequisitionId, string Reference, Guid UomId,
    UnitOfMeasure Uom, decimal Quantity, ProcurementSource Route, string SupplierName,
    DateTime LastUpdatedAt
);
internal sealed record PipelinePurchaseOrderLine(
    Guid Id, Guid PurchaseOrderId, Guid SourceRequisitionId, string Reference,
    Guid UomId, UnitOfMeasure Uom, decimal Quantity, string SupplierName,
    DateTime? ExpectedDate, PurchaseOrderStatus Status, DateTime LastUpdatedAt
);
internal sealed record PipelineShipmentLine(
    Guid Id, Guid PurchaseOrderId, Guid ShipmentInvoiceId, string Reference,
    Guid UomId, UnitOfMeasure Uom, decimal Quantity, decimal DistributedQuantity,
    string SupplierName, DateTime? ExpectedDate, DateTime LastUpdatedAt
);
internal sealed record PipelineShipmentStatus(
    Guid ShipmentInvoiceId, ShipmentStatus Status, DateTime LastUpdatedAt
);
internal readonly record struct PipelineSourceQuote(Guid SourceRequisitionId, Guid UomId);
internal sealed record PipelineReceivingLine(
    Guid Id, Guid? ShipmentInvoiceId, string Reference, UnitOfMeasure Uom,
    decimal Quantity, string SupplierName, DistributedRequisitionMaterialStatus Status,
    DateTime LastUpdatedAt
);
