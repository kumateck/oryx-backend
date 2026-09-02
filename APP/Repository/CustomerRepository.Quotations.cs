using APP.Utils;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<Guid>> CreateQuotation(
        Guid customerId, CreateCustomerQuotationRequest request, Guid userId)
    {
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == customerId);
        if (customer is null) return Error.NotFound("Customer.NotFound", "Customer not found.");
        if (!customer.CurrencyId.HasValue)
            return Error.Validation("CustomerQuotation.CurrencyRequired", "Set the customer's preferred currency before quoting.");
        if (request.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.ValidUntil", "Quotation validity cannot be in the past.");
        if (request.Items.Count == 0)
            return Error.Validation("CustomerQuotation.Items", "At least one item is required.");
        if (request.Items.GroupBy(item => new { item.ProductId, item.UoMId }).Any(group => group.Count() > 1))
            return Error.Conflict("CustomerQuotation.DuplicateItem", "Product and unit combinations must be unique.");
        if (await context.CustomerQuotations.AnyAsync(item => item.Code == request.Code.Trim()))
            return Error.Conflict("CustomerQuotation.Code", "Quotation code already exists.");

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var uomIds = request.Items.Select(item => item.UoMId).Distinct().ToList();
        var products = await context.Products.Where(item => productIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id);
        if (products.Count != productIds.Count) return Error.NotFound("Product.NotFound", "One or more products were not found.");
        if (await context.UnitOfMeasures.CountAsync(item => uomIds.Contains(item.Id)) != uomIds.Count)
            return Error.NotFound("UoM.NotFound", "One or more units of measure were not found.");

        var quotation = new CustomerQuotation
        {
            CustomerId = customerId, CurrencyId = customer.CurrencyId.Value,
            Code = request.Code.Trim(), Status = CustomerQuotationStatus.Draft,
            ValidUntil = request.ValidUntil, CreatedById = userId,
        };
        foreach (var requestItem in request.Items)
        {
            if (requestItem.Quantity <= 0 || requestItem.DiscountPercent is < 0 or > 100)
                return Error.Validation("CustomerQuotation.Item", "Quantity and discount are outside allowed ranges.");
            var price = requestItem.UnitPrice;
            if (!price.HasValue)
            {
                var active = await GetPricingMatches(customerId, requestItem.ProductId, requestItem.UoMId, DateTime.UtcNow);
                switch (active.Count)
                {
                    case > 1:
                        return Error.Conflict("CustomerPricing.Ambiguous", "Multiple active pricing agreements match.");
                    case 1 when active[0].CurrencyId != customer.CurrencyId:
                        return Error.Conflict("CustomerPricing.Currency", "Active pricing currency differs from the customer's preferred currency.");
                    default:
                        price = active.Count == 1 ? active[0].AgreedPrice : products[requestItem.ProductId].Price;
                        break;
                }
            }
            if (price < 0) return Error.Validation("CustomerQuotation.Price", "Unit price cannot be negative.");
            quotation.Items.Add(new CustomerQuotationItem
            {
                ProductId = requestItem.ProductId, UoMId = requestItem.UoMId,
                Quantity = requestItem.Quantity, UnitPrice = price.Value,
                DiscountPercent = requestItem.DiscountPercent, CreatedById = userId,
            });
        }
        await context.CustomerQuotations.AddAsync(quotation);
        await context.SaveChangesAsync();
        return quotation.Id;
    }

    public async Task<Result<CustomerQuotationDto>> GetQuotation(Guid quotationId, DateTime? asOf = null)
    {
        var quotation = await QuotationQuery().FirstOrDefaultAsync(item => item.Id == quotationId);
        return quotation is null ? Error.NotFound("CustomerQuotation.NotFound", "Quotation not found.")
            : MapQuotation(quotation, asOf ?? DateTime.UtcNow);
    }

    public async Task<Result<Paginateable<IEnumerable<CustomerQuotationDto>>>> GetActiveQuotations(
        Guid customerId, int page, int pageSize, DateTime? asOf = null)
    {
        if (!await CustomerExists(customerId)) return Error.NotFound("Customer.NotFound", "Customer not found.");
        var date = asOf ?? DateTime.UtcNow;
        var query = QuotationQuery().Where(item => item.CustomerId == customerId && item.ValidUntil >= date
            && item.Status != CustomerQuotationStatus.Rejected
            && item.Status != CustomerQuotationStatus.Expired
            && item.Status != CustomerQuotationStatus.ConvertedToOrder);
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            item => MapQuotation(item, date));
    }

    private IQueryable<CustomerQuotation> QuotationQuery()
        => context.CustomerQuotations.AsNoTracking().AsSplitQuery().Include(item => item.Customer)
            .Include(item => item.Currency).Include(item => item.Items).ThenInclude(item => item.Product)
            .Include(item => item.Items).ThenInclude(item => item.UoM).Include(item => item.Approvals);

    private Task<List<CustomerPricingAgreement>> GetPricingMatches(
        Guid customerId, Guid productId, Guid uomId, DateTime asOf)
        => context.CustomerPricingAgreements.AsNoTracking().Where(item => item.CustomerId == customerId
            && item.ProductId == productId && item.UoMId == uomId && item.EffectiveFrom <= asOf
            && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= asOf)).Take(2).ToListAsync();

    private static CustomerQuotationDto MapQuotation(CustomerQuotation item, DateTime asOf) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, CustomerId = item.CustomerId,
        CustomerName = item.Customer?.Name, Code = item.Code,
        Currency = item.Currency is null ? null : new CurrencyDto { Id = item.Currency.Id, Name = item.Currency.Name, Symbol = item.Currency.Symbol },
        Status = item.ValidUntil < asOf && item.Status is CustomerQuotationStatus.Draft or CustomerQuotationStatus.Sent
            ? CustomerQuotationStatus.Expired : item.Status,
        ValidUntil = item.ValidUntil, Approved = item.Approved,
        Items =
        [
            .. item.Items.Select(line => new CustomerQuotationItemDto
            {
                Id = line.Id, CreatedAt = line.CreatedAt, ProductId = line.ProductId,
                ProductName = line.Product?.Name, Quantity = line.Quantity, UoMId = line.UoMId,
                UoMName = line.UoM?.Name, UnitPrice = line.UnitPrice,
                DiscountPercent = line.DiscountPercent, TotalValue = line.TotalValue,
            })
        ],
        TotalValue = item.Items.Sum(line => line.TotalValue),
        Approvals =
        [
            .. item.Approvals.OrderBy(stage => stage.Order).Select(stage => new CustomerQuotationApprovalDto
            {
                Id = stage.Id, Order = stage.Order, Required = stage.Required, Status = stage.Status,
                ApprovalTime = stage.ApprovalTime, Comments = stage.Comments,
            })
        ]
    };
}
