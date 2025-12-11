using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IJobExecutionRepository
{
    Task<Result<Paginateable<IEnumerable<JobExecutionDto>>>> GetJobExecutions(int page, int pageSize, 
        JobExecutionStatus? status = null, Guid? employeeId = null, Guid? jobRequestId = null);
    Task<Result<JobExecutionDto>> GetJobExecution(Guid id);
    Task<Result> AcknowledgeJobExecution(AcknowledgeJobExecutionRequest request);
    Task<Result> StartJobExecution(StartJobExecutionRequest request);
    Task<Result<Guid>> RecordJobActivity(Guid jobExecutionId, RecordJobActivityRequest request);
    Task<Result<Guid>> RecordConsumedItem(Guid jobExecutionId, RecordConsumedItemRequest request);
    Task<Result> CompleteJobExecution(CompleteJobExecutionRequest request);
    Task<Result> VerifyJobExecution(VerifyJobExecutionRequest request);
    Task<Result> ApproveJobExecution(ApproveJobExecutionRequest request);
}

