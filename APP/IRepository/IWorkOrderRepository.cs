using APP.Utils;
using DOMAIN.Entities.WorkOrders;
using SHARED;

namespace APP.IRepository;

public interface IWorkOrderRepository
{
    Task<Result<Guid>> CreateWorkOrder(CreateWorkOrderRequest request, Guid userId);
    Task<Result<WorkOrderDto>> GetWorkOrder(Guid workOrderId);
    Task<Result<Paginateable<IEnumerable<WorkOrderDto>>>> GetWorkOrders(int page, int pageSize,
        string searchQuery);
     Task<Result> UpdateWorkOrder(UpdateWorkOrderRequest request, Guid workOrderId, Guid userId);
    Task<Result> DeleteWorkOrder(Guid workOrderId, Guid userId);
}