using APP.Utils;
using DOMAIN.Entities.LeaveEntitlements;
using SHARED;

namespace APP.IRepository;

public interface ILeaveEntitlementRepository
{
   Task<Result<Guid>> CreateLeaveEntitlement(LeaveEntitlementDto leaveEntitlementDto, Guid userId);
   Task<Result<LeaveEntitlementDto>> GetLeaveEntitlement(Guid leaveEntitlementId);
   Task<Result<Paginateable<IEnumerable<LeaveEntitlementDto>>>> GetLeaveEntitlements(int page, int pageSize, string searchQuery);
   
   Task<Result> UpdateLeaveEntitlement(Guid id, LeaveEntitlementDto leaveEntitlementDto, Guid userId);
   
   Task<Result> DeleteLeaveEntitlement(Guid id, Guid userId);
                                                                           
}