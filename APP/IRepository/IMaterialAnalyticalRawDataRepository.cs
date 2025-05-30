using APP.Utils;
using DOMAIN.Entities.MaterialAnalyticalRawData;

namespace APP.IRepository;
using SHARED;

public interface IMaterialAnalyticalRawDataRepository
{
    Task<Result<Guid>> CreateAnalyticalRawData(CreateMaterialAnalyticalRawDataRequest request);
    
    Task<Result<Paginateable<IEnumerable<MaterialAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery, int materialKind);
    
    Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id);
    
    Task<Result> UpdateAnalyticalRawData(Guid id, CreateMaterialAnalyticalRawDataRequest request);
    
    Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId);
    
}