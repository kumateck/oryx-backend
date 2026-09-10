using APP.Utils;
using DOMAIN.Entities.RndTechnologyTransfers;
using SHARED;

namespace APP.IRepository;

public interface IRndTechnologyTransferRepository
{
    Task<Result<Guid>> CreateTransfer(Guid rndProjectId, CreateRndTechnologyTransferRequest request, Guid userId);
    Task<Result> UpdateStatus(Guid id, UpdateRndTechnologyTransferStatusRequest request, Guid userId);
    Task<Result<Guid>> PromoteToProduction(Guid id, Guid userId);
    Task<Result<RndTechnologyTransferDto>> GetTransfer(Guid id);
    Task<Result<Paginateable<IEnumerable<RndTechnologyTransferDto>>>> GetTransfersForProject(
        Guid rndProjectId,
        int page,
        int pageSize
    );
}
