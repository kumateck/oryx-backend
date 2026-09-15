using APP.Utils;
using DOMAIN.Entities.RndAnalyticalMethods;
using SHARED;

namespace APP.IRepository;

public interface IRndAnalyticalMethodRepository
{
    Task<Result<Guid>> CreateMethod(Guid rndProjectId, CreateRndAnalyticalMethodRequest request, Guid userId);
    Task<Result> UpdateMethod(Guid id, CreateRndAnalyticalMethodRequest request, Guid userId);
    Task<Result> UpdateStatus(Guid id, UpdateRndAnalyticalMethodStatusRequest request, Guid userId);
    Task<Result<Guid>> TransferToStp(Guid id, TransferRndAnalyticalMethodRequest request, Guid userId);
    Task<Result<RndAnalyticalMethodDto>> GetMethod(Guid id);
    Task<Result<Paginateable<IEnumerable<RndAnalyticalMethodDto>>>> GetMethodsForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    );
}
