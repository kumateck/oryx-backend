using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Distribution;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Procurement;

public static class MaterialDistributionPageQueries
{
    public sealed record RequestRow(Guid MaterialId, DistributionRequisitionItem Share);

    public static IQueryable<ShipmentInvoiceItemDto> InvoiceLines(ApplicationDbContext context, Guid invoiceId)
        => context.ShipmentInvoiceItems.IgnoreAutoIncludes().AsNoTracking()
            .Where(line => line.ShipmentInvoiceId == invoiceId && !line.Distributed)
            .TagWith("Material distribution page: invoice source scalars")
            .Select(line => new ShipmentInvoiceItemDto {
                Id = line.Id, CreatedAt = line.CreatedAt,
                Material = new MaterialDto { Id = line.Material.Id, Name = line.Material.Name,
                    Code = line.Material.Code, Description = line.Material.Description, Kind = line.Material.Kind },
                UoM = new UnitOfMeasureDto { Id = line.UoM.Id, Name = line.UoM.Name, Symbol = line.UoM.Symbol },
                Manufacturer = new CollectionItemDto { Id = line.Manufacturer.Id, Name = line.Manufacturer.Name },
                PurchaseOrder = new CollectionItemDto { Id = line.PurchaseOrder.Id, Code = line.PurchaseOrder.Code },
                ExpectedQuantity = line.ExpectedQuantity, ReceivedQuantity = line.ReceivedQuantity,
                Price = line.Price, PriceUoM = line.PriceUoM,
                Currency = line.Currency == null ? null : new CurrencyDto {
                    Id = line.Currency.Id, Name = line.Currency.Name, Symbol = line.Currency.Symbol }
            });

    public static IQueryable<RequestRow> Requests(ApplicationDbContext context, Guid shipmentId, Guid[] materialIds)
        => (from item in context.RequisitionItems.IgnoreAutoIncludes().AsNoTracking()
            join request in context.Requisitions on item.RequisitionId equals request.Id into requests
            from request in requests.DefaultIfEmpty()
            join department in context.Departments on request.DepartmentId equals department.Id into departments
            from department in departments.DefaultIfEmpty()
            where materialIds.Contains(item.MaterialId) && item.Quantity - item.QuantityReceived != 0
                // Preserve existing eligibility: any PO on this shipment may link the requesting requisition.
                && (from line in context.ShipmentInvoiceItems
                    join order in context.PurchaseOrders on line.PurchaseOrderId equals order.Id
                    join source in context.SourceRequisitionItems on order.SourceRequisitionId equals source.SourceRequisitionId
                    join shipment in context.ShipmentDocuments on line.ShipmentInvoiceId equals shipment.ShipmentInvoiceId
                    where shipment.Id == shipmentId && source.RequisitionId == item.RequisitionId
                    select line.Id).Any()
            select new RequestRow(item.MaterialId, new DistributionRequisitionItem {
                Department = department == null ? null : new DepartmentDto { Id = department.Id,
                    Name = department.Name, Code = department.Code, Description = department.Description },
                RequisitionItem = new RequisitionItemDto { Id = item.Id, Quantity = item.Quantity,
                    QuantityReceived = item.QuantityReceived, Status = item.Status,
                    UoM = item.UoM == null ? null : new UnitOfMeasureDto { Id = item.UoM.Id, Name = item.UoM.Name, Symbol = item.UoM.Symbol } },
                QuantityRequested = item.Quantity
            })).TagWith("Material distribution page: eligible requisitions and department scalars");
}
