using APP.Utils;
using DOMAIN.Entities.Approvals;
using SHARED;

namespace APP.IRepository;

public interface IApprovalRepository
{
    Task<Result<Guid>> CreateApproval(CreateApprovalRequest request, Guid userId);

    Task<Result<ApprovalDto>> GetApproval(Guid approvalId);

    Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(int page, int pageSize, string searchQuery);

    Task<Result> UpdateApproval(CreateApprovalRequest request, Guid approvalId, Guid userId);

    Task<Result> DeleteApproval(Guid approvalId, Guid userId);
    
}