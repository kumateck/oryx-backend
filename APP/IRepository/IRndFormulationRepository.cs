using APP.Utils;
using DOMAIN.Entities.RndFormulations;
using SHARED;

namespace APP.IRepository;

public interface IRndFormulationRepository
{
    Task<Result<Guid>> CreateFormulation(Guid rndProjectId, CreateRndFormulationRequest request, Guid userId);
    Task<Result<Guid>> CreateNewVersion(Guid previousFormulationId, CreateRndFormulationRequest request, Guid userId);
    Task<Result> UpdateFormulation(Guid id, CreateRndFormulationRequest request, Guid userId);
    Task<Result> UpdateStatus(Guid id, RndFormulationStatus status, Guid userId);
    Task<Result<RndFormulationDto>> GetFormulation(Guid id);
    Task<Result<Paginateable<IEnumerable<RndFormulationDto>>>> GetFormulationsForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    );
    Task<Result<Paginateable<IEnumerable<RndFormulationReviewDto>>>> GetReviewQueue(
        int page,
        int pageSize
    );
    Task<Result<RndFormulationReviewDto>> GetReviewItem(Guid formulationId);
}
