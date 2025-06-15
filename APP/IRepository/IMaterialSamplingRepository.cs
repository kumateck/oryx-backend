using DOMAIN.Entities.MaterialSampling;
using SHARED;

namespace APP.IRepository;

public interface IMaterialSamplingRepository
{
    Task<Result<Guid>> CreateMaterialSampling(CreateMaterialSamplingRequest materialSamplingRequest);
    
    Task<Result<MaterialSamplingDto>> GetMaterialSamplingByMaterialId(Guid id);
}