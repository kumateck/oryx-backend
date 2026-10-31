using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Finance costing. List prices keep their history: setting a price appends a dated row in the
/// chosen currency, so quotations and invoices already raised keep the price they used. No price
/// is ever converted between currencies.
/// </summary>
public class CostingRepository(ApplicationDbContext context) : ICostingRepository
{
    public async Task<Result<Paginateable<IEnumerable<ProductListPriceDto>>>> GetListPrices(
        int page, int pageSize, string searchQuery, Guid? departmentId, Guid? currencyId)
    {
        var query = context.Products.AsNoTracking().Where(product => !product.DeletedAt.HasValue);
        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.WhereSearch(searchQuery, product => product.Name, product => product.Code);
        if (departmentId.HasValue) query = query.Where(product => product.DepartmentId == departmentId);
        if (currencyId.HasValue)
            query = query.Where(product => product.Prices.OrderByDescending(price => price.Date)
                .Select(price => price.CurrencyId).FirstOrDefault() == currencyId);

        var labels = await CustomerPricingResolver.CurrencyLabelsAsync(context);
        var baseCurrencyId = await CustomerPricingResolver.BaseCurrencyIdAsync(context);
        return await PaginationHelper.GetPaginatedResultAsync(query.OrderBy(product => product.Name), page, pageSize,
            product => MapListPrice(product, labels, baseCurrencyId));
    }

    public async Task<Result<List<ListPriceHistoryDto>>> GetListPriceHistory(Guid productId)
    {
        var product = await context.Products.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == productId && !item.DeletedAt.HasValue);
        if (product is null) return ProductErrors.NotFound(productId);
        var labels = await CustomerPricingResolver.CurrencyLabelsAsync(context);
        var baseCurrencyId = await CustomerPricingResolver.BaseCurrencyIdAsync(context);
        return product.Prices.OrderByDescending(price => price.Date).Select(price => new ListPriceHistoryDto
        {
            Price = price.Price, PriceUoM = price.PriceUoM, Date = price.Date,
            Currency = CustomerPricingResolver.Label(labels, price.CurrencyId ?? baseCurrencyId),
        }).ToList();
    }

    public async Task<Result> SetListPrice(Guid productId, SetListPriceRequest request, Guid userId)
    {
        if (request.Price <= 0) return Error.Validation("ListPrice.Price", "The price must be above zero.");
        var product = await context.Products
            .FirstOrDefaultAsync(item => item.Id == productId && !item.DeletedAt.HasValue);
        if (product is null) return ProductErrors.NotFound(productId);
        if (!await context.Currencies.AnyAsync(currency => currency.Id == request.CurrencyId))
            return Error.NotFound("Currency.NotFound", "Currency not found.");

        var unit = string.IsNullOrWhiteSpace(request.PriceUoM) ? product.PriceUoM : request.PriceUoM.Trim();
        if (product.Prices.Count > 0 && product.Price == request.Price && product.PriceCurrencyId == request.CurrencyId
            && product.PriceUoM == unit)
            return Result.Success();

        product.Prices.Add(new ProductPrices
        {
            Price = request.Price, PriceUoM = unit, CurrencyId = request.CurrencyId, Date = DateTime.UtcNow,
        });
        product.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<CustomerPricingAgreementDto>>>> GetPricingAgreements(
        int page, int pageSize, string searchQuery, Guid? customerId, Guid? productId, DateTime? activeOn)
    {
        var query = AgreementQuery();
        if (customerId.HasValue) query = query.Where(item => item.CustomerId == customerId);
        if (productId.HasValue) query = query.Where(item => item.ProductId == productId);
        if (activeOn.HasValue)
            query = query.Where(item => item.EffectiveFrom <= activeOn.Value
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= activeOn.Value));
        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.WhereSearch(searchQuery, item => item.Customer.Name, item => item.Product.Name,
                item => item.Product.Code);
        return await PaginationHelper.GetPaginatedResultAsync(
            query.OrderBy(item => item.Customer.Name).ThenByDescending(item => item.EffectiveFrom),
            page, pageSize, CustomerRepository.MapPricing);
    }

    public async Task<Result<CustomerPricingAgreementDto>> GetPricingAgreement(Guid id)
    {
        var agreement = await AgreementQuery().FirstOrDefaultAsync(item => item.Id == id);
        return agreement is null
            ? Error.NotFound("CustomerPricing.NotFound", "Pricing agreement not found.")
            : CustomerRepository.MapPricing(agreement);
    }

    private IQueryable<CustomerPricingAgreement> AgreementQuery()
        => context.CustomerPricingAgreements.AsNoTracking().Include(item => item.Customer)
            .Include(item => item.Product).Include(item => item.ProductPacking).Include(item => item.Currency);

    internal static ProductListPriceDto MapListPrice(
        Product product, IReadOnlyDictionary<Guid, CurrencyDto> labels, Guid? baseCurrencyId)
    {
        var current = product.Prices.OrderByDescending(price => price.Date).FirstOrDefault();
        return new ProductListPriceDto
        {
            ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
            DepartmentId = product.DepartmentId, Price = current?.Price ?? 0, PriceUoM = current?.PriceUoM,
            PricedAt = current?.Date,
            Currency = current is null ? null : CustomerPricingResolver.Label(labels, current.CurrencyId ?? baseCurrencyId),
        };
    }
}
