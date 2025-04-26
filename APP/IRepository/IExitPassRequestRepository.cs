using APP.Utils;
using DOMAIN.Entities.ExitPassRequests;
using SHARED;

namespace APP.IRepository;

public interface IExitPassRequestRepository
{
    Task<Result<Guid>> CreateExitPassRequest(CreateExitPassRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<ExitPassRequestDto>>>> GetExitPassRequests(int page, int pageSize, string searchQuery);
    Task<Result<ExitPassRequestDto>> GetExitPassRequest(Guid id);
    Task<Result> UpdateExitPassRequest(Guid id, CreateExitPassRequest request, Guid userId);
    Task<Result> DeleteExitPassRequest(Guid id, Guid userId);
    
}