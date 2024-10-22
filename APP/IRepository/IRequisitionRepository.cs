using DOMAIN.Entities.Requisitions;
using SHARED;

namespace APP.IRepository;

public interface IRequisitionRepository
{ 
    Task<Result<Guid>> CreateRequisition(CreateRequisitionRequest request, Guid userId);
    Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId);
    Task<Result> ApproveRequisition(ApproveRequisitionRequest request, Guid requisitionId, Guid userId, List<Guid> roleIds);
     Task<Result> ProcessRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId);
}