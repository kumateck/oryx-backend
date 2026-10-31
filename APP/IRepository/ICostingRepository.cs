using APP.Utils;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Customers;
using SHARED;

namespace APP.IRepository;

/// <summary>Finance costing: product list prices and customer pricing agreements across customers.</summary>
public interface ICostingRepository
{
    Task<Result<Paginateable<IEnumerable<ProductListPriceDto>>>> GetListPrices(
        int page, int pageSize, string searchQuery, Guid? departmentId, Guid? currencyId);
    Task<Result<List<ListPriceHistoryDto>>> GetListPriceHistory(Guid productId);
    Task<Result> SetListPrice(Guid productId, SetListPriceRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<CustomerPricingAgreementDto>>>> GetPricingAgreements(
        int page, int pageSize, string searchQuery, Guid? customerId, Guid? productId, DateTime? activeOn);
    Task<Result<CustomerPricingAgreementDto>> GetPricingAgreement(Guid id);
}
