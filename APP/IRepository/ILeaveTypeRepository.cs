using APP.Utils;
using DOMAIN.Entities.LeaveTypes;
using SHARED;

namespace APP.IRepository;

public interface ILeaveTypeRepository
{
    Task<Result<Guid>> CreateLeaveType(CreateLeaveTypeRequest leaveTypeDto);
    
    Task<Result<Paginateable<IEnumerable<LeaveTypeDto>>>> GetLeaveTypes(int page, int pageSize,
        string searchQuery = null);
    
    Task<Result<LeaveTypeDto>> GetLeaveType(Guid id);
    
    Task<Result> UpdateLeaveType(Guid id, CreateLeaveTypeRequest leaveTypeDto);
    
    Task<Result> DeleteLeaveType(Guid id, Guid userId);
}