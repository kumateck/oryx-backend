using System.Data;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Procurement;

public static class ShortageMakeUpInvoiceCreation
{
    public static async Task<Result<Guid>> Create(ApplicationDbContext context,
        CreateShortageMakeUpInvoice request, Guid actorId)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.RequestKey == Guid.Empty ||
            request.Quantity <= 0 || request.ShipmentDiscrepancyItemId == Guid.Empty)
            return Error.Validation("MakeUp.Invalid", "Code, request key, case line and positive quantity are required.");

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var existing = await context.ShipmentInvoices.AsNoTracking().Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.FulfillmentRequestKey == request.RequestKey);
        if (existing is not null)
        {
            if (existing.Code != request.Code.Trim() || existing.Items.Count != 1 ||
                existing.Items[0].FulfillsDiscrepancyItemId != request.ShipmentDiscrepancyItemId ||
                existing.Items[0].ReceivedQuantity != request.Quantity)
                return Error.Validation("MakeUp.RequestKey", "Request key belongs to a different make-up invoice.");
            return existing.Id;
        }

        if (await context.ShipmentInvoices.AnyAsync(item => item.Code == request.Code))
            return Error.Validation("Code.Duplicate", "Shipment invoice code already exists.");

        var line = await context.Set<ShipmentDiscrepancyItem>()
            .Include(item => item.ShipmentDiscrepancy)
            .Include(item => item.ShipmentInvoiceItem)
                .ThenInclude(item => item.ShipmentInvoice)
            .SingleOrDefaultAsync(item => item.Id == request.ShipmentDiscrepancyItemId);
        if (line is null || line.ShipmentInvoiceItem is null)
            return Error.NotFound("Discrepancy.NotFound", "Source invoice discrepancy line not found.");
        if (!line.ShipmentDiscrepancy.Approved || line.ShipmentDiscrepancy.Rejected)
            return Error.Validation("Discrepancy.Approval", "The discrepancy requires approval.");

        var balance = await ShipmentShortageBalance.Load(context, line.Id);
        if (balance is null || balance.DeclaredQuantity <= balance.InitialPhysicalQuantity ||
            request.Quantity > balance.AvailableToShipQuantity)
            return Error.Validation("MakeUp.Quantity", "Quantity exceeds the approved shortage available to ship.");

        var source = line.ShipmentInvoiceItem;
        if (!source.ShipmentInvoice.SupplierId.HasValue)
            return Error.Validation("MakeUp.Supplier", "Source invoice must have a supplier.");
        var invoice = new ShipmentInvoice
        {
            Code = request.Code.Trim(),
            Kind = ShipmentInvoiceKind.NoChargeShortageMakeUp,
            FulfillmentRequestKey = request.RequestKey,
            SupplierId = source.ShipmentInvoice.SupplierId,
            CurrencyId = source.CurrencyId,
            TotalCost = 0,
            CreatedById = actorId,
            CreatedAt = DateTime.UtcNow,
            Items = [new ShipmentInvoiceItem
            {
                MaterialId = source.MaterialId,
                UoMId = source.UoMId,
                ManufacturerId = source.ManufacturerId,
                PurchaseOrderId = source.PurchaseOrderId,
                ExpectedQuantity = request.Quantity,
                ReceivedQuantity = request.Quantity,
                Price = 0,
                PriceUoM = source.PriceUoM,
                TotalCost = 0,
                CurrencyId = source.CurrencyId,
                FulfillsDiscrepancyItemId = line.Id,
                Reason = $"Approved shortage make-up for discrepancy {line.ShipmentDiscrepancyId}",
                CreatedById = actorId,
                CreatedAt = DateTime.UtcNow,
            }],
        };
        context.ShipmentInvoices.Add(invoice);
        await context.SaveChangesAsync();
        if (transaction is not null) await transaction.CommitAsync();
        return invoice.Id;
    }
}
