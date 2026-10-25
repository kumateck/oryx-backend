using DOMAIN.Entities.Shipments.Request;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Procurement;

public static class ShipmentShortageBalance
{
    public static async Task<ShipmentShortageBalanceDto?> Load(
        ApplicationDbContext context, Guid discrepancyItemId)
    {
        var line = await context.Set<DOMAIN.Entities.Shipments.ShipmentDiscrepancyItem>()
            .AsNoTracking().SingleOrDefaultAsync(item => item.Id == discrepancyItemId);
        if (line is null) return null;

        var makeUpLines = await context.ShipmentInvoiceItems.AsNoTracking()
            .Where(item => item.FulfillsDiscrepancyItemId == discrepancyItemId)
            .Select(item => new { item.Id, item.ReceivedQuantity, item.Distributed }).ToListAsync();
        var ids = makeUpLines.Select(item => item.Id).ToList();
        var assignments = await context.MaterialItemDistributions.AsNoTracking()
            .Where(item => ids.Contains(item.ShipmentInvoiceItemId))
            .Select(item => new
            {
                item.ShipmentInvoiceItemId,
                item.DistributedRequisitionMaterial.Status,
            }).ToListAsync();
        var finalized = assignments.GroupBy(item => item.ShipmentInvoiceItemId)
            .Where(group => group.All(item =>
                item.Status >= DistributedRequisitionMaterialStatus.Checked))
            .Select(group => group.Key).ToHashSet();
        var receipts = await context.ShipmentShortageReceipts.AsNoTracking()
            .Where(item => ids.Contains(item.ShipmentInvoiceItemId))
            .GroupBy(item => item.ShipmentInvoiceItemId)
            .Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.PhysicalQuantity) })
            .ToListAsync();
        var byInvoiceItem = receipts.ToDictionary(item => item.Id, item => item.Quantity);
        var confirmed = receipts.Sum(item => item.Quantity);
        var inFlight = makeUpLines.Sum(item =>
            item.Distributed && finalized.Contains(item.Id) ? 0 :
                Math.Max(item.ReceivedQuantity - byInvoiceItem.GetValueOrDefault(item.Id), 0));
        var shortage = Math.Max(line.DeclaredQuantitySnapshot - line.ReceivedQuantity, 0);
        return new ShipmentShortageBalanceDto
        {
            ShipmentDiscrepancyId = line.ShipmentDiscrepancyId,
            ShipmentDiscrepancyItemId = line.Id,
            DeclaredQuantity = line.DeclaredQuantitySnapshot,
            InitialPhysicalQuantity = line.ReceivedQuantity,
            ConfirmedMakeUpQuantity = confirmed,
            InFlightMakeUpQuantity = inFlight,
            OutstandingQuantity = Math.Max(shortage - confirmed, 0),
            AvailableToShipQuantity = Math.Max(shortage - confirmed - inFlight, 0),
        };
    }
}
