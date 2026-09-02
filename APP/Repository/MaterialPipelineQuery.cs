using DOMAIN.Entities.Materials;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class MaterialPipelineQuery
{
    public static async Task<List<MaterialPipelineStockDto>> GetAsync(
        ApplicationDbContext context,
        Guid materialId
    )
    {
        var requisitions = await context.RequisitionItems.AsNoTracking()
            .Where(item =>
                item.MaterialId == materialId
                && item.Requisition.RequisitionType == RequisitionType.Purchase
                && item.Requisition.Status != RequestStatus.Completed
                && item.Requisition.Status != RequestStatus.Rejected
                && item.Status != RequestStatus.Completed
                && item.Status != RequestStatus.Rejected
            )
            .Select(item => new PipelineRequisitionLine(
                item.Id, item.RequisitionId, item.Requisition.Code, item.UoMId, item.UoM,
                item.Quantity, item.Requisition.ExpectedDelivery,
                item.UpdatedAt ?? item.CreatedAt
            ))
            .ToListAsync();

        var sources = await context.SourceRequisitionItems.AsNoTracking()
            .Where(item =>
                item.MaterialId == materialId
                && context.Requisitions.Any(requisition =>
                    requisition.Id == item.RequisitionId
                    && requisition.RequisitionType == RequisitionType.Purchase
                    && requisition.Status != RequestStatus.Completed
                    && requisition.Status != RequestStatus.Rejected
                )
            )
            .Select(item => new PipelineSourceLine(
                item.SourceRequisitionId, item.RequisitionId, item.SourceRequisition.Code,
                item.UoMId, item.UoM, item.Quantity, item.Source,
                item.SourceRequisition.Supplier.Name,
                item.SourceRequisition.UpdatedAt ?? item.SourceRequisition.CreatedAt
            ))
            .ToListAsync();

        var purchaseOrders = await context.PurchaseOrderItems.AsNoTracking()
            .Where(item =>
                item.MaterialId == materialId
                && item.PurchaseOrder.Status != PurchaseOrderStatus.Cancelled
                && item.PurchaseOrder.Status != PurchaseOrderStatus.Revised
            )
            .Select(item => new PipelinePurchaseOrderLine(
                item.Id, item.PurchaseOrderId, item.PurchaseOrder.SourceRequisitionId,
                item.PurchaseOrder.Code, item.UoMId, item.UoM, item.Quantity,
                item.PurchaseOrder.Supplier.Name,
                item.PurchaseOrder.EstimatedDeliveryDate ?? item.PurchaseOrder.ExpectedDeliveryDate,
                item.PurchaseOrder.Status, item.PurchaseOrder.UpdatedAt ?? item.PurchaseOrder.CreatedAt
            ))
            .ToListAsync();

        var shipments = await context.ShipmentInvoiceItems.AsNoTracking()
            .Where(item => item.MaterialId == materialId)
            .Select(item => new PipelineShipmentLine(
                item.Id, item.PurchaseOrderId, item.ShipmentInvoiceId,
                item.ShipmentInvoice.Code, item.UoMId, item.UoM, item.ReceivedQuantity,
                context.MaterialItemDistributions
                    .Where(distribution => distribution.ShipmentInvoiceItemId == item.Id)
                    .Sum(distribution => distribution.Quantity),
                item.ShipmentInvoice.Supplier.Name,
                item.PurchaseOrder.EstimatedDeliveryDate ?? item.PurchaseOrder.ExpectedDeliveryDate,
                item.UpdatedAt ?? item.CreatedAt
            ))
            .ToListAsync();

        var shipmentIds = shipments.Select(item => item.ShipmentInvoiceId).Distinct().ToList();
        var documents = await context.ShipmentDocuments.AsNoTracking()
            .Where(document =>
                document.ShipmentInvoiceId.HasValue
                && shipmentIds.Contains(document.ShipmentInvoiceId.Value)
            )
            .Select(document => new PipelineShipmentStatus(
                document.ShipmentInvoiceId!.Value,
                document.Status,
                document.UpdatedAt ?? document.CreatedAt
            ))
            .ToListAsync();
        var statuses = documents.GroupBy(document => document.ShipmentInvoiceId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(document => document.Status)
                    .ThenByDescending(document => document.LastUpdatedAt)
                    .First()
            );

        var receiving = await context.DistributedRequisitionMaterials.AsNoTracking()
            .Where(item =>
                item.MaterialId == materialId
                && item.Status != DistributedRequisitionMaterialStatus.Distributed
            )
            .Select(item => new PipelineReceivingLine(
                item.Id, item.ShipmentInvoiceId, item.ShipmentInvoice.Code, item.UoM,
                item.Quantity, item.ShipmentInvoice.Supplier.Name, item.Status,
                item.UpdatedAt ?? item.CreatedAt
            ))
            .ToListAsync();

        var quotedSources = await context.SupplierQuotationItems.AsNoTracking()
            .Where(item =>
                item.MaterialId == materialId
                && item.SupplierQuotation.ReceivedQuotation
            )
            .Select(item => new PipelineSourceQuote(
                item.SupplierQuotation.SourceRequisitionId,
                item.UoMId
            ))
            .ToListAsync();

        var requisitionIds = requisitions.Select(item => item.RequisitionId)
            .Concat(sources.Select(item => item.RequisitionId))
            .Distinct()
            .ToList();
        var departmentsByRequisition = await context.Requisitions.AsNoTracking()
            .Where(requisition => requisitionIds.Contains(requisition.Id))
            .Select(requisition => new { requisition.Id, DepartmentName = requisition.Department.Name })
            .ToDictionaryAsync(item => item.Id, item => item.DepartmentName);

        return MaterialPipelineBuilder.Build(
            requisitions, sources, purchaseOrders, shipments, statuses,
            receiving, quotedSources.ToHashSet(), departmentsByRequisition
        );
    }
}
