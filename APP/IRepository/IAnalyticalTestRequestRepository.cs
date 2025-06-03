using APP.Utils;
using DOMAIN.Entities.Analytical_Test_Requests;
using SHARED;

namespace APP.IRepository;

public interface IAnalyticalTestRequestRepository
{
    Task<Result<Guid>> CreateAnalyticalTestRequest(CreateAnalyticalTestRequest request);
    
    Task<Result<Paginateable<IEnumerable<AnalyticalTestRequestDto>>>> GetAnalyticalTestRequests(int page, int pageSize, string searchQuery);
    
    Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequest(Guid id);
    
    Task<Result> UpdateAnalyticalTestRequest(Guid id, CreateAnalyticalTestRequest request);
    
    Task<Result> DeleteAnalyticalTestRequest(Guid id, Guid userId);
}