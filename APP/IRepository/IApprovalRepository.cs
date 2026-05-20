using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users;
using SHARED;

namespace APP.IRepository;

public interface IApprovalRepository
{
    Task<Result<Guid>> CreateApproval(CreateApprovalRequest request, Guid userId);

    Task<Result<ApprovalDto>> GetApproval(Guid approvalId);

    Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(
        int page,
        int pageSize,
        string searchQuery
    );

    Task<Result> UpdateApproval(CreateApprovalRequest request, Guid approvalId, Guid userId);

    Task<Result> DeleteApproval(Guid approvalId, Guid userId);
    Task<Result> ApproveItem(
        string modelType,
        Guid modelId,
        Guid userId,
        List<Guid> roleIds,
        string comments = null
    );
    Task<Result> RejectItem(
        string modelType,
        Guid modelId,
        Guid userId,
        List<Guid> roleIds,
        string comments = null
    );
    Task<List<ApprovalEntity>> GetEntitiesRequiringApproval(
        Guid userId,
        List<Guid> roleIds,
        string modelType
    );

    Task<List<UserDto>> GetUsersWithPendingApprovals();

    Task<Dictionary<string, int>> GetStatisticsOfEntitiesRequiringApproval(
        Guid userId,
        List<Guid> roleIds
    );

    Task<Result<ApprovalEntity>> GetEntityRequiringApproval(string modelType, Guid modelId);
    List<ResponsibleApprovalStage> GetCurrentApprovalStage(
        List<ResponsibleApprovalStage> stages,
        Guid userId,
        Guid roleId
    );
    Task CreateInitialApprovalsAsync(string modelType, Guid modelId);
    Task ProcessApprovalEscalations(Guid userId, Guid roleId);

    Result DelegateApproval(DelegateApproval approval);
    Task<Result> TransferApprovalRights(TransferApprovalRequest request);
}
