using APP.Utils;
using DOMAIN.Entities.StaffRequisitions;
using SHARED;

namespace APP.IRepository;

public interface IStaffRequisitionRepository
{
    Task<Result<Guid>> CreateStaffRequisition(CreateStaffRequisitionRequest request, Guid userId);
    
    Task<Result<Paginateable<IEnumerable<StaffRequisitionDto>>>> GetStaffRequisitions(int page, int pageSize, string searchQuery,
        DateTime? startDate, DateTime? endDate);
    Task<Result<StaffRequisitionDto>> GetStaffRequisition(Guid id);
    Task<Result> UpdateStaffRequisition(Guid id, CreateStaffRequisitionRequest request);
    Task<Result> DeleteStaffRequisitionRequest(Guid id, Guid userId);
}