using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Procurement;

public static class ShortageReceiptPreparation
{
    public static async Task<Result<List<ShipmentShortageReceipt>>> Prepare(
        ApplicationDbContext context, CreateChecklistRequest request, Guid actorId)
    {
        var assignments = await context.MaterialItemDistributions.AsNoTracking()
            .Include(item => item.ShipmentInvoiceItem)
                .ThenInclude(item => item.UoM)
            .Where(item => item.DistributedRequisitionMaterialId ==
                request.DistributedRequisitionMaterialId &&
                item.ShipmentInvoiceItem.FulfillsDiscrepancyItemId != null)
            .ToListAsync();
        if (assignments.Count == 0) return new List<ShipmentShortageReceipt>();
        if (assignments.Count != 1 || request.MaterialBatches.Count == 0)
            return Error.Validation("MakeUp.Receipt", "A make-up checklist must map to one invoice line.");
        var assignment = assignments[0];
        if (await context.ShipmentShortageReceipts.AnyAsync(item =>
            item.ShipmentInvoiceItemId == assignment.ShipmentInvoiceItemId &&
            item.Checklist.DistributedRequisitionMaterialId == request.DistributedRequisitionMaterialId))
            return Error.Validation("MakeUp.Receipt", "This make-up delivery was already counted.");

        decimal counted = 0;
        foreach (var batch in request.MaterialBatches)
        {
            if (batch.TotalQuantity < 0 || batch.MaterialId != assignment.ShipmentInvoiceItem.MaterialId ||
                !batch.UoMId.HasValue)
                return Error.Validation("MakeUp.Batch", "Batch material, quantity or unit is invalid.");
            var from = await context.UnitOfMeasures.AsNoTracking()
                .Where(item => item.Id == batch.UoMId.Value)
                .Select(item => item.Symbol).SingleOrDefaultAsync();
            var converted = UomConverter.Convert(batch.TotalQuantity, from,
                assignment.ShipmentInvoiceItem.UoM.Symbol);
            if (!converted.HasValue)
                return Error.Validation("MakeUp.UoM", "Batch and invoice units cannot be reconciled.");
            counted += converted.Value;
        }
        if (counted > assignment.Quantity)
            return Error.Validation("MakeUp.Quantity", "Physical count exceeds the allocated make-up quantity.");
        return new List<ShipmentShortageReceipt> { new()
        {
            ShipmentInvoiceItemId = assignment.ShipmentInvoiceItemId,
            PhysicalQuantity = counted,
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow,
        }};
    }
}
