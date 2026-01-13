using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IJobOrderRepository
{
    Task<Result<Guid>> CreateJobOrder(CreateJobOrderRequest request);
    Task<Result<Paginateable<IEnumerable<JobOrderDto>>>> GetJobOrders(int page, int pageSize,
        JobOrderStatus? status = null, Guid? jobRequestId = null, Guid? serviceId = null);
    Task<Result<JobOrderDto>> GetJobOrder(Guid id);
    Task<Result> SendJobOrderToProviders(SendJobOrderToProvidersRequest request);
    Task<Result<IEnumerable<JobOrderServiceProviderDto>>> GetJobOrderResponseServiceProviders();
    Task<Result> SelectQuotation(SelectQuotationRequest request);
    Task<Result<Guid>> StartJobOrderExecution(StartJobOrderExecutionRequest request);
    Task<Result<Guid>> RecordJobOrderActivity(Guid jobOrderExecutionId, RecordJobActivityRequest request);
    Task<Result<Guid>> RecordJobOrderConsumedItem(Guid jobOrderExecutionId, RecordConsumedItemRequest request);
    Task<Result> CompleteJobOrderExecution(CompleteJobOrderExecutionRequest request);
    Task<Result> VerifyJobOrderExecution(VerifyJobOrderExecutionRequest request);
    Task<Result> ApproveJobOrderExecution(ApproveJobOrderExecutionRequest request);
}

