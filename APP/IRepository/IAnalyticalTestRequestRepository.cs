using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using SHARED;

namespace APP.IRepository;

public interface IAnalyticalTestRequestRepository
{
    Task<Result<Guid>> CreateAnalyticalTestRequest(CreateAnalyticalTestRequest request);
    
    Task<Result<Paginateable<IEnumerable<AnalyticalTestRequestDto>>>> GetAnalyticalTestRequests(int page, int pageSize, string searchQuery, AnalyticalTestStatus? status);
    
    Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequest(Guid id);
    
    Task<Result> UpdateAnalyticalTestRequest(Guid id, CreateAnalyticalTestRequest request);
    
    Task<Result> DeleteAnalyticalTestRequest(Guid id, Guid userId);
    Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequestByActivityStep(Guid activityStepId);
}