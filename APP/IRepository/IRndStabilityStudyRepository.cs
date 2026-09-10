using APP.Utils;
using DOMAIN.Entities.RndStabilityStudies;
using SHARED;

namespace APP.IRepository;

public interface IRndStabilityStudyRepository
{
    Task<Result<Guid>> CreateChamber(CreateRndStabilityChamberRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<RndStabilityChamberDto>>>> GetChambers(int page, int pageSize);

    Task<Result<Guid>> CreateStudy(Guid rndProjectId, CreateRndStabilityStudyRequest request, Guid userId);
    Task<Result> UpdateStudyStatus(Guid id, UpdateRndStabilityStudyStatusRequest request, Guid userId);
    Task<Result> RecordPullPointResult(Guid pullPointId, RecordPullPointResultRequest request, Guid userId);
    Task<Result<RndStabilityStudyDto>> GetStudy(Guid id);
    Task<Result<Paginateable<IEnumerable<RndStabilityStudyDto>>>> GetStudiesForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    );
}
