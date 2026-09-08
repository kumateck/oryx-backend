using APP.Utils;
using DOMAIN.Entities.RndTrialBatches;
using SHARED;

namespace APP.IRepository;

public interface IRndTrialBatchRepository
{
    Task<Result<Guid>> CreateTrialBatch(Guid rndProjectId, CreateRndTrialBatchRequest request, Guid userId);
    Task<Result> UpdateStatus(Guid id, UpdateRndTrialBatchStatusRequest request, Guid userId);
    Task<Result<RndTrialBatchDto>> GetTrialBatch(Guid id);
    Task<Result<Paginateable<IEnumerable<RndTrialBatchDto>>>> GetTrialBatchesForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    );
}
