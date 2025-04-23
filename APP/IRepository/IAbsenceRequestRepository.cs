using APP.Utils;
using DOMAIN.Entities.AbsenceRequests;
using SHARED;

namespace APP.IRepository;

public interface IAbsenceRequestRepository
{
    Task<Result<Guid>> CreateAbsenceRequest(CreateAbsenceRequest request, Guid userId);
    
    Task<Result<Paginateable<IEnumerable<AbsenceRequestDto>>>> GetAbsenceRequests(int page, int pageSize, string searchQuery);
    
    Task<Result<AbsenceRequestDto>> GetAbsenceRequest(Guid id);
    
    Task<Result> UpdateAbsenceRequest(Guid id, CreateAbsenceRequest request, Guid userId);
    
    Task<Result> DeleteAbsenceRequest(Guid id, Guid userId);
}