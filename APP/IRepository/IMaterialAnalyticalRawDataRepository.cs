using APP.Utils;
using DOMAIN.Entities.MaterialAnalyticalRawData;
using DOMAIN.Entities.Materials;

namespace APP.IRepository;
using SHARED;

public interface IMaterialAnalyticalRawDataRepository
{
    Task<Result<Guid>> CreateAnalyticalRawData(CreateMaterialAnalyticalRawDataRequest request);
    
    Task<Result<Paginateable<IEnumerable<MaterialAnalyticalRawDataDto>>>> GetAnalyticalRawData(int page, int pageSize, string searchQuery, MaterialKind materialKind);
    
    Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawData(Guid id);
    Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawDataByMaterial(Guid id); 
    Task<Result<MaterialAnalyticalRawDataDto>> GetAnalyticalRawDataByMaterialBatch(Guid id);
    Task<Result> UpdateAnalyticalRawData(Guid id, CreateMaterialAnalyticalRawDataRequest request);
    
    Task<Result> DeleteAnalyticalRawData(Guid id, Guid userId);
    Task<Result> StartTestForMaterialBatch(Guid id);
}