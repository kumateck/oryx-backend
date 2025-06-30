using APP.Utils;
using DOMAIN.Entities.OvertimeRequests;
using SHARED;

namespace APP.IRepository;

public interface IOvertimeRequestRepository
{
    Task<Result<Guid>> CreateOvertimeRequest(CreateOvertimeRequest request);
    
    Task<Result<Paginateable<IEnumerable<OvertimeRequestDto>>>> GetOvertimeRequests(int page, int pageSize, string searchQuery);
    
    Task<Result<OvertimeRequestDto>> GetOvertimeRequest(Guid id);
    
    Task<Result> UpdateOvertimeRequest(Guid id, CreateOvertimeRequest request);
    
    Task<Result> DeleteOvertimeRequest(Guid id, Guid userId);
}