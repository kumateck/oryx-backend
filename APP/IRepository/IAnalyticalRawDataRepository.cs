using APP.Utils;
using DOMAIN.Entities.AnalyticalRawData;

namespace APP.IRepository;
using SHARED;

public interface IAnalyticalRawDataRepository
{
    Task<Result<Guid>> CreateAnalyticalRawData(CreateAnalyticalRawDataRequest request);
    
    Task<Result<Paginateable<IEnumerable<AnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery);
    
    Task<Result<AnalyticalRawDataDto>> GetAnalyticalRawData(Guid id);
    
    Task<Result> UpdateAnalyticalRawData(Guid id, CreateAnalyticalRawDataRequest request);
    
    Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId);
    
}