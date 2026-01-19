using APP.Utils;
using DOMAIN.Entities.JobRequests;
using SHARED;

namespace APP.IRepository;

public interface IServiceMemoRepository
{
    Task<Result<Guid>> CreateServiceMemo(CreateServiceMemoRequest request);
    Task<Result<Paginateable<IEnumerable<ServiceMemoDto>>>> GetServiceMemos(int page, int pageSize,
        ServiceMemoStatus? status = null, Guid? jobOrderId = null, Guid? serviceProviderId = null);
    Task<Result<ServiceMemoDto>> GetServiceMemo(Guid id);
    Task<Result> UpdateServiceMemo(Guid id, UpdateServiceMemoRequest request);
    Task<Result> MarkServiceMemoAsPaid(Guid id);
    Task<Result> IssueServiceMemo(IssueServiceMemoRequest request);
}

