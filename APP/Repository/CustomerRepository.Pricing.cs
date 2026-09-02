using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<List<CustomerPricingAgreementDto>>> GetPricingAgreements(Guid customerId)
    {
        if (!await CustomerExists(customerId))
            return Error.NotFound("Customer.NotFound", "Customer not found.");
        var entities = await PricingQuery(customerId).OrderByDescending(item => item.EffectiveFrom).ToListAsync();
        return entities.Select(MapPricing).ToList();
    }

    public async Task<Result<Guid>> CreatePricingAgreement(
        Guid customerId, CustomerPricingAgreementRequest request, Guid userId)
    {
        var validation = await ValidatePricing(customerId, request, null);
        if (!validation.IsSuccess) return validation.Error;
        var entity = new CustomerPricingAgreement { CustomerId = customerId, CreatedById = userId };
        AssignPricing(entity, request);
        context.CustomerPricingAgreements.Add(entity);
        await context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<Result> UpdatePricingAgreement(
        Guid customerId, Guid id, CustomerPricingAgreementRequest request, Guid userId)
    {
        var entity = await context.CustomerPricingAgreements.FirstOrDefaultAsync(item =>
            item.Id == id && item.CustomerId == customerId);
        if (entity is null) return Error.NotFound("CustomerPricing.NotFound", "Pricing agreement not found.");
        var validation = await ValidatePricing(customerId, request, id);
        if (!validation.IsSuccess) return validation.Error;
        AssignPricing(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePricingAgreement(Guid customerId, Guid id, Guid userId)
    {
        var entity = await context.CustomerPricingAgreements.FirstOrDefaultAsync(item =>
            item.Id == id && item.CustomerId == customerId);
        if (entity is null) return Error.NotFound("CustomerPricing.NotFound", "Pricing agreement not found.");
        entity.DeletedAt = DateTime.UtcNow;
        entity.LastDeletedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<CustomerPricingAgreementDto>> GetActivePricingAgreement(
        Guid customerId, Guid productId, Guid uomId, DateTime asOf)
    {
        var matches = await PricingQuery(customerId).Where(item => item.ProductId == productId
            && item.UoMId == uomId && item.EffectiveFrom <= asOf
            && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= asOf)).Take(2).ToListAsync();
        if (matches.Count == 0) return Error.NotFound("CustomerPricing.NotFound", "No active pricing agreement found.");
        if (matches.Count > 1) return Error.Conflict("CustomerPricing.Ambiguous", "Multiple active pricing agreements match.");
        return MapPricing(matches[0]);
    }

    private async Task<Result> ValidatePricing(
        Guid customerId, CustomerPricingAgreementRequest request, Guid? exceptId)
    {
        if (!await CustomerExists(customerId)) return Error.NotFound("Customer.NotFound", "Customer not found.");
        if (request.AgreedPrice <= 0) return Error.Validation("CustomerPricing.Price", "Agreed price must be positive.");
        if (request.EffectiveTo < request.EffectiveFrom)
            return Error.Validation("CustomerPricing.Dates", "Effective-to cannot precede effective-from.");
        if (!await context.Products.AnyAsync(item => item.Id == request.ProductId))
            return Error.NotFound("Product.NotFound", "Product not found.");
        if (!await context.UnitOfMeasures.AnyAsync(item => item.Id == request.UoMId))
            return Error.NotFound("UoM.NotFound", "Unit of measure not found.");
        if (!await context.Currencies.AnyAsync(item => item.Id == request.CurrencyId))
            return Error.NotFound("Currency.NotFound", "Currency not found.");
        var overlaps = await context.CustomerPricingAgreements.AnyAsync(item =>
            item.CustomerId == customerId && item.ProductId == request.ProductId && item.UoMId == request.UoMId
            && (!exceptId.HasValue || item.Id != exceptId.Value)
            && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= request.EffectiveFrom)
            && (!request.EffectiveTo.HasValue || item.EffectiveFrom <= request.EffectiveTo.Value));
        return overlaps ? Error.Conflict("CustomerPricing.Overlap", "The effective date range overlaps an existing agreement.") : Result.Success();
    }

    private IQueryable<CustomerPricingAgreement> PricingQuery(Guid customerId)
        => context.CustomerPricingAgreements.AsNoTracking().Include(item => item.Product)
            .Include(item => item.UoM).Include(item => item.Currency).Where(item => item.CustomerId == customerId);

    private static void AssignPricing(CustomerPricingAgreement item, CustomerPricingAgreementRequest request)
    {
        item.ProductId = request.ProductId; item.UoMId = request.UoMId; item.AgreedPrice = request.AgreedPrice;
        item.CurrencyId = request.CurrencyId; item.EffectiveFrom = request.EffectiveFrom;
        item.EffectiveTo = request.EffectiveTo; item.Notes = request.Notes?.Trim();
    }

    private static CustomerPricingAgreementDto MapPricing(CustomerPricingAgreement item) => new()
    {
        Id = item.Id, CreatedAt = item.CreatedAt, CustomerId = item.CustomerId,
        ProductId = item.ProductId, ProductName = item.Product?.Name, UoMId = item.UoMId,
        UoMName = item.UoM?.Name, AgreedPrice = item.AgreedPrice, Currency = item.Currency is null ? null : new()
        { Id = item.Currency.Id, Name = item.Currency.Name, Symbol = item.Currency.Symbol },
        EffectiveFrom = item.EffectiveFrom, EffectiveTo = item.EffectiveTo, Notes = item.Notes,
    };
}
