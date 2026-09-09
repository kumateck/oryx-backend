using System.Data;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.ProductionOrders;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<Guid>> ConvertQuotationToProductionOrder(Guid quotationId, Guid userId)
    {
        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var quotation = await context.CustomerQuotations
            .Include(item => item.Items).ThenInclude(item => item.Product)
            .Include(item => item.Items).ThenInclude(item => item.ProductPacking)
            .FirstOrDefaultAsync(item => item.Id == quotationId);
        if (quotation is null) return Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.");
        if (quotation.Status != CustomerQuotationStatus.Accepted || !quotation.Approved)
            return Error.Conflict("CustomerQuotation.Status", "Only an accepted, approved quotation can be converted.");
        if (quotation.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.Expired", "An expired quotation cannot be converted.");
        if (await context.ProductionOrders.AnyAsync(item => item.SourceCustomerQuotationId == quotationId))
            return Error.Conflict("CustomerQuotation.Converted", "Quotation already has a production order.");

        var order = new ProductionOrder
        {
            CustomerId = quotation.CustomerId, Code = $"PO-{quotation.Code}",
            Status = ProductionOrderStatus.Pending, SourceCustomerQuotationId = quotation.Id,
            CreatedById = userId,
            Products =
            [
                .. quotation.Items.Select(item =>
                {
                    var perShipper = item.ProductPacking?.PackPerShipper ?? 0;
                    return new ProductionOrderProducts
                    {
                        ProductId = item.ProductId, TotalOrderQuantity = item.Quantity,
                        VolumePerPiece = item.Product.BaseQuantity > 0 ? item.Product.BaseQuantity : 1m,
                        ProductPackingId = item.ProductPackingId, UnitPrice = item.UnitPrice,
                        DiscountPercent = item.DiscountPercent,
                        Shippers = perShipper > 0 ? item.Quantity / perShipper : 0,
                        Loose = perShipper > 0 ? item.Quantity % perShipper : item.Quantity,
                    };
                })
            ],
        };
        quotation.Status = CustomerQuotationStatus.ConvertedToOrder;
        quotation.UpdatedAt = DateTime.UtcNow;
        quotation.LastUpdatedById = userId;
        await context.ProductionOrders.AddAsync(order);
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(nameof(ProductionOrder), order.Id);

        if (transaction is not null) await transaction.CommitAsync();
        return order.Id;
    }
}
