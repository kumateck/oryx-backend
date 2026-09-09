using APP.Utils;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
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
        if (request.ValidUntil < DateTime.UtcNow)
            return Error.Validation("CustomerQuotation.ValidUntil", "Quotation validity cannot be in the past.");
        if (request.Items.Count == 0)
            return Error.Validation("CustomerQuotation.Items", "At least one item is required.");
        if (request.Items.GroupBy(item => new { item.ProductId, item.ProductPackingId }).Any(group => group.Count() > 1))
            return Error.Conflict("CustomerQuotation.DuplicateItem", "Product and packing combinations must be unique.");
        if (await context.CustomerQuotations.AnyAsync(item => item.Code == request.Code.Trim()))
            return Error.Conflict("CustomerQuotation.Code", "Quotation code already exists.");

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
        var productPackingIds = request.Items.Select(item => item.ProductPackingId).Distinct().ToList();
        var products = await context.Products.IgnoreQueryFilters()
            .Where(item => productIds.Contains(item.Id) && !item.DeletedAt.HasValue)
            .ToDictionaryAsync(item => item.Id);
        if (products.Count != productIds.Count) return Error.NotFound("Product.NotFound", "One or more products were not found.");
        if (await context.ProductPackings.CountAsync(item => productPackingIds.Contains(item.Id)) != productPackingIds.Count)
            return Error.NotFound("ProductPacking.NotFound", "One or more packing styles were not found.");

        var priceDate = DateTime.UtcNow;
        var currency = await ResolveQuotationCurrency(customer, request.Items, priceDate);
        if (currency.IsFailure) return currency.Error;

        var quotation = new CustomerQuotation
        {
            CustomerId = customerId, CurrencyId = currency.Value,
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
                var resolved = await ResolveUnitPrice(
                    customer, products[requestItem.ProductId], requestItem.ProductPackingId, priceDate);
                if (resolved.IsFailure) return resolved.Error;
                price = resolved.Value.UnitPrice;
            }
            if (price < 0) return Error.Validation("CustomerQuotation.Price", "Unit price cannot be negative.");
            quotation.Items.Add(new CustomerQuotationItem
            {
                ProductId = requestItem.ProductId, ProductPackingId = requestItem.ProductPackingId,
                Quantity = requestItem.Quantity, UnitPrice = price.Value,
                DiscountPercent = requestItem.DiscountPercent, CreatedById = userId,
            });
        }
        await context.CustomerQuotations.AddAsync(quotation);
        await context.SaveChangesAsync();
        return quotation.Id;
    }

    public async Task<Result<ResolvedQuotationPriceDto>> ResolveQuotationUnitPrice(
        Guid customerId, Guid productId, Guid productPackingId, DateTime asOf)
    {
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == customerId);
        if (customer is null) return Error.NotFound("Customer.NotFound", "Customer not found.");
        var product = await context.Products.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.Id == productId && !item.DeletedAt.HasValue);
        if (product is null) return Error.NotFound("Product.NotFound", "Product not found.");
        if (!await context.ProductPackings.AnyAsync(item => item.Id == productPackingId))
            return Error.NotFound("ProductPacking.NotFound", "Packing style not found.");
        return await ResolveUnitPrice(customer, product, productPackingId, asOf);
    }

    /// <summary>
    /// Same precedence <see cref="CreateQuotation"/> applies when a request omits a unit price:
    /// the single active pricing agreement for this customer/product/packing, else the product's
    /// list price. Backed by <see cref="CustomerPricingResolver"/> so the quotation, production
    /// order, and production schedule report paths can never disagree on what "the resolved
    /// price" means.
    /// </summary>
    private async Task<Result<ResolvedQuotationPriceDto>> ResolveUnitPrice(
        Customer customer, Product product, Guid productPackingId, DateTime asOf)
    {
        var resolution = await CustomerPricingResolver.ResolveAsync(
            context, customer.Id, product.Id, productPackingId, product.Price, asOf);
        if (resolution.Ambiguous)
            return Error.Conflict("CustomerPricing.Ambiguous", "Multiple active pricing agreements match.");
        if (resolution.FromAgreement && customer.CurrencyId.HasValue
            && resolution.CurrencyId != customer.CurrencyId)
            return Error.Conflict("CustomerPricing.Currency", "Active pricing currency differs from the customer's preferred currency.");
        if (!resolution.FromAgreement && !customer.CurrencyId.HasValue)
            return Error.Validation(
                "CustomerQuotation.CurrencyRequired",
                "Set the customer's preferred currency or create an active pricing agreement for this product and packing.");
        return new ResolvedQuotationPriceDto { UnitPrice = resolution.UnitPrice, FromAgreement = resolution.FromAgreement };
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

    public async Task<Result<List<CustomerQuotationDto>>> GetConvertibleQuotations(
        Guid customerId, DateTime? asOf = null)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        var date = asOf ?? DateTime.UtcNow;
        var quotations = await QuotationQuery()
            .Where(item => item.CustomerId == customerId
                && item.Status == CustomerQuotationStatus.Accepted
                && item.Approved
                && item.ValidUntil >= date
                && !context.ProductionOrders.Any(order =>
                    order.SourceCustomerQuotationId == item.Id))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();
        return quotations.Select(item => MapQuotation(item, date)).ToList();
    }

    private IQueryable<CustomerQuotation> QuotationQuery()
        => context.CustomerQuotations.AsNoTracking().AsSplitQuery().Include(item => item.Customer)
            .Include(item => item.Currency).Include(item => item.Items).ThenInclude(item => item.Product)
            .Include(item => item.Items).ThenInclude(item => item.ProductPacking).Include(item => item.Approvals);

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
                ProductName = line.Product?.Name, Quantity = line.Quantity, ProductPackingId = line.ProductPackingId,
                ProductPackingName = line.ProductPacking?.Name, UnitPrice = line.UnitPrice,
                PackPerShipper = line.ProductPacking?.PackPerShipper ?? 0,
                Shippers = line.ProductPacking != null && line.ProductPacking.PackPerShipper > 0
                    ? line.Quantity / line.ProductPacking.PackPerShipper : 0,
                Loose = line.ProductPacking != null && line.ProductPacking.PackPerShipper > 0
                    ? line.Quantity % line.ProductPacking.PackPerShipper : line.Quantity,
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
