using APP.Utils;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using SHARED;

namespace APP.IRepository;

public interface IRequisitionRepository
{ 
    Task<Result<Guid>> CreateRequisition(CreateRequisitionRequest request, Guid userId);
    Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId);
    Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(int page, int pageSize,
        string searchQuery);
    Task<Result> ApproveRequisition(ApproveRequisitionRequest request, Guid requisitionId, Guid userId, List<Guid> roleIds);
     Task<Result> ProcessRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId);
}