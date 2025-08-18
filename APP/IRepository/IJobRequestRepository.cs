using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IJobRequestRepository
{
    Task<Result<Guid>> CreateJobRequest(CreateJobRequest request);
    Task<Result<List<JobRequestDto>>> GetJobRequests();
}