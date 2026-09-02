using APP.Utils;
using DOMAIN.Entities.OosInvestigations;
using SHARED;

namespace APP.IRepository;

public interface IOosInvestigationRepository
{
    Task<Result<Guid>> InitiateOosInvestigation(InitiateOosInvestigationRequest request, Guid userId);
    Task<Result> UpdateOosInvestigation(Guid investigationId, UpdateOosInvestigationRequest request, Guid userId);
    Task<Result> SubmitToQa(Guid investigationId, Guid userId);
    Task<Result> ReviewByQa(Guid investigationId, ReviewOosInvestigationRequest request, Guid userId);
    Task<Result<OosInvestigationDto>> GetOosInvestigation(Guid id);
    Task<Result<Paginateable<IEnumerable<OosInvestigationDto>>>> GetOosInvestigations(int page, int pageSize, string searchQuery, OosInvestigationStatus? status);
}
