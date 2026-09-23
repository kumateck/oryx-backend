using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.IRepository;

public interface ICommercialCertificateRepository
{
    Task<Result<Guid>> GenerateForMaterial(Guid materialSamplingId, Guid actorId);
    Task<Result<Guid>> GenerateForMaterialBatch(Guid materialBatchId, Guid actorId);
    Task<Result<Guid>> GenerateForProduct(Guid analyticalTestRequestId, Guid actorId);
    Task<Result<CommercialCertificateDto>> Get(Guid id);
}
