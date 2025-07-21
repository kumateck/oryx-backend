using APP.Utils;
using DOMAIN.Entities.MaterialAnalyticalRawData;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.UniformityOfWeights;

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
    
    Task<Result<Guid>> CreateUniformityOfWeight(CreateUniformityOfWeight request);
    Task<Result<Paginateable<IEnumerable<UniformityOfWeightDto>>>> GetUniformityOfWeights(int page, int pageSize, string searchQuery);
    Task<Result<UniformityOfWeightDto>> GetUniformityOfWeight(Guid id);
    Task<Result> UpdateUniformityOfWeight(Guid id, CreateUniformityOfWeight request);
    Task<Result> DeleteUniformityOfWeight(Guid id, Guid userId);
    Task<Result<Guid>> SubmitUniformityOfWeightResponse(CreateUniformityOfWeightResponse request);
    Task<Result<IEnumerable<UniformityOfWeightResponseDto>>> GetResponsesByMaterialBatchId(Guid unifomityOfWeightId, Guid materialBatchId);
}