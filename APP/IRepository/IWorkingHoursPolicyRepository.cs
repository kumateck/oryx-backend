using DOMAIN.Entities.ShiftAssignments;
using SHARED;

namespace APP.IRepository;

public interface IWorkingHoursPolicyRepository
{
    Task<Result<Guid>> CreatePolicy(CreateWorkingHoursPolicyRequest request);
    Task<Result<List<WorkingHoursPolicyDto>>> GetPolicies();
    Task<Result> DeletePolicy(Guid id, Guid userId);
}
