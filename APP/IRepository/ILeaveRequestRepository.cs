using APP.Utils;
using DOMAIN.Entities.LeaveRequests;
using SHARED;

namespace APP.IRepository;

public interface ILeaveRequestRepository
{
    Task<Result<Guid>> CreateLeaveOrAbsenceRequest(CreateLeaveRequest leaveRequest, Guid userId);
    Task<Result<Paginateable<IEnumerable<LeaveRequestDto>>>> GetLeaveRequests(int page, int pageSize, string searchQuery);
    Task<Result<LeaveRequestDto>> GetLeaveRequest(Guid leaveRequestId);
    Task<Result> UpdateLeaveRequest(Guid leaveRequestId, CreateLeaveRequest leaveRequest, Guid userId);
    Task<Result> DeleteLeaveRequest(Guid leaveRequestId, Guid userId);
    
}