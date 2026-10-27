using APP.Extensions;
using AutoMapper;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Procurement;

public static class ShipmentInvoiceCreation
{
    public static async Task<Result<Guid>> Create(
        ApplicationDbContext context,
        IMapper mapper,
        CreateShipmentInvoice request,
        Guid userId
    )
    {
        if (request.Items.DistinctBy(i => new { i.PurchaseOrderId, i.MaterialId }).Count()
            != request.Items.Count)
        {
            return Error.Validation("Items.Count", "Purchase Order & material must be unique");
        }

        if (await context.ShipmentInvoices.AnyAsync(i => i.Code == request.Code))
        {
            return Error.Validation("Code", "Shipment Invoice code already exists.");
        }

        var orderIds = request.Items.Select(i => i.PurchaseOrderId).Distinct().ToList();
        var orders = await context.PurchaseOrders.AsSplitQuery()
            .Include(p => p.Items)
            .Include(p => p.TermsOfPayment)
            .Where(p => orderIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        var priceUnits = await context.BuildQuotationPriceUoMLookup(orderIds);
        var prepared = new List<(
            CreateShipmentInvoiceItem Item,
            PurchaseOrder Order,
            PurchaseOrderItem OrderItem,
            string PriceUoM)>();

        // Validate every line before adding the invoice or changing a tracked order.
        foreach (var item in request.Items)
        {
            if (!orders.TryGetValue(item.PurchaseOrderId, out var order))
                return Error.NotFound("PurchaseOrder.NotFound", "Purchase Order not found");

            var orderItem = order.Items.FirstOrDefault(i => i.MaterialId == item.MaterialId);
            if (orderItem is null)
                return Error.NotFound("PurchaseOrderItem.NotFound", "Purchase Order Item not found");

            var priceUnit = priceUnits.ResolvePriceUoM(
                orderItem.PriceUoM, order.Id, orderItem.MaterialId, orderItem.UoMId);
            if (orderItem.Price > 0m && string.IsNullOrWhiteSpace(priceUnit))
            {
                return Error.Validation(
                    "Item.PriceUoM",
                    $"Purchase Order {order.Code} has a priced material without a price unit. "
                    + "Correct the purchase-order price unit before creating this invoice.");
            }

            if (orderItem.QuantityInvoiced + item.ReceivedQuantity > orderItem.Quantity)
            {
                return Error.Validation(
                    "Item.ReceivedQuantity",
                    "Purchase Order Item does not have enough quantity");
            }

            prepared.Add((item, order, orderItem, priceUnit));
        }

        var invoice = mapper.Map<ShipmentInvoice>(request);
        invoice.CreatedById = userId;
        foreach (var (item, order, orderItem, priceUnit) in prepared)
        {
            var invoiceItem = invoice.Items.Single(i =>
                i.PurchaseOrderId == item.PurchaseOrderId && i.MaterialId == item.MaterialId);
            // Freeze authoritative pricing; request prices never override the order.
            invoiceItem.Price = orderItem.Price;
            invoiceItem.PriceUoM = priceUnit;
            invoiceItem.CurrencyId = orderItem.CurrencyId;
            orderItem.QuantityInvoiced += item.ReceivedQuantity;
        }

        foreach (var order in prepared.Select(p => p.Order).DistinctBy(p => p.Id))
        {
            order.Status = order.Items.All(i => i.QuantityInvoiced == i.Quantity)
                ? PurchaseOrderStatus.Linked
                : PurchaseOrderStatus.PartiallyLinked;
        }

        var dueDays = prepared.Select(p => p.Order.TermsOfPayment?.DueDays).Distinct().ToList();
        invoice.DueDate = dueDays.Count == 1 && dueDays[0].HasValue
            ? DateTime.UtcNow.Date.AddDays(dueDays[0]!.Value)
            : null;
        var currencies = prepared.Select(p => p.OrderItem.CurrencyId).Distinct().ToList();
        invoice.CurrencyId = currencies.Count == 1 ? currencies[0] : null;

        // Orders are already tracked. Updating their entire graph would mark legacy
        // prices as rewritten and trip the price-unit guard on unrelated PO lines.
        await context.ShipmentInvoices.AddAsync(invoice);
        await context.SaveChangesAsync();
        return invoice.Id;
    }
}
