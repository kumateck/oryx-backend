using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.IRepository;

public interface IMicrobialRequirementRepository
{
    Task<Result<Guid>> Create(CreateMicrobialRequirementRequest request, Guid actorId);
    Task<Result<List<MicrobialRequirementDto>>> List(Guid? materialId, Guid? productId);
}
