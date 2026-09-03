using APP.Utils;
using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<Paginateable<IEnumerable<CustomerOrderHistoryDto>>>> GetOrderHistory(
        Guid customerId, int page, int pageSize)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        var query = context.ProductionOrders.AsNoTracking().AsSplitQuery()
            .Include(item => item.Products).ThenInclude(item => item.Product)
            .Where(item => item.CustomerId == customerId);
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, MapOrderHistory);
    }

    public async Task<Result<CustomerSummaryDto>> GetSummary(Guid customerId)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        var orders = await context.ProductionOrders.AsNoTracking().AsSplitQuery()
            .Include(item => item.Products).ThenInclude(item => item.Product)
            .Where(item => item.CustomerId == customerId).ToListAsync();
        var total = orders.Sum(item => item.Products.Sum(line => line.TotalValue));
        var comparable = orders.Where(item => item.PromisedDeliveryDate.HasValue && item.DeliveredAt.HasValue).ToList();
        var credit = await GetAvailableCredit(customerId);
        if (!credit.IsSuccess) return credit.Error;
        return new CustomerSummaryDto
        {
            CustomerId = customerId, LifetimeOrderCount = orders.Count,
            TotalOrderValue = total, AverageOrderValue = orders.Count == 0 ? 0 : total / orders.Count,
            OnTimeDeliveryRate = comparable.Count == 0 ? null : comparable.Count(item =>
                item.DeliveredAt!.Value <= item.PromisedDeliveryDate!.Value) * 100m / comparable.Count,
            CurrentOutstandingBalance = credit.Value.OutstandingInPreferredCurrency,
            PreferredCurrencyId = credit.Value.PreferredCurrencyId,
        };
    }

    private static CustomerOrderHistoryDto MapOrderHistory(DOMAIN.Entities.ProductionOrders.ProductionOrder item)
        => new()
        {
            Id = item.Id, Code = item.Code, Status = item.Status, CreatedAt = item.CreatedAt,
            TotalValue = item.Products.Sum(line => line.TotalValue),
            PromisedDeliveryDate = item.PromisedDeliveryDate, DeliveredAt = item.DeliveredAt,
        };
}
