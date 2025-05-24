using APP.Utils;
using DOMAIN.Entities.ProductAnalyticalRawData;
using SHARED;

namespace APP.IRepository;

public interface IProductAnalyticalRawDataRepository
{
    Task<Result<Guid>> CreateAnalyticalRawData(CreateProductAnalyticalRawDataRequest request);
    
    Task<Result<Paginateable<IEnumerable<ProductAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery);
    
    Task<Result<ProductAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id);
    
    Task<Result> UpdateAnalyticalRawData(Guid id, CreateProductAnalyticalRawDataRequest request);
    
    Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId);
}