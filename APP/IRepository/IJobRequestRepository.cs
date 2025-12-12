using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IJobRequestRepository
{
    Task<Result<Guid>> CreateJobRequest(CreateJobRequest request);
    Task<Result<Paginateable<IEnumerable<JobRequestDto>>>> GetJobRequests(int page, int pageSize, string searchQuery = null, 
        JobRequestStatus? status = null, JobHandlingType? handlingType = null, Guid? departmentId = null);
    Task<Result<JobRequestDto>> GetJobRequest(Guid id);
    Task<Result> UpdateJobRequest(Guid id, UpdateJobRequestRequest request);
    Task<Result> DeleteJobRequest(Guid id, Guid userId);
    Task<Result<Guid>> AssignInternalJob(AssignInternalJobRequest request);
    Task<Result> UpdateJobRequestStatus(Guid id, JobRequestStatus status);
}