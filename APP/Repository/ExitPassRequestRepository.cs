using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ExitPassRequests;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Repository;

public class ExitPassRequestRepository(ApplicationDbContext context, IMapper mapper) : IExitPassRequestRepository
{
    public async Task<Result<Guid>> CreateExitPassRequest(CreateExitPassRequest request, Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Paginateable<IEnumerable<ExitPassRequestDto>>>> GetExitPassRequests(int page, int pageSize, string searchQuery)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ExitPassRequestDto>> GetExitPassRequest(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> UpdateExitPassRequest(Guid id, CreateExitPassRequest request, Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteExitPassRequest(Guid id, Guid userId)
    {
        throw new NotImplementedException();
    }
}