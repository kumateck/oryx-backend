using APP.Utils;
using DOMAIN.Entities.PayGroups;
using SHARED;

namespace APP.IRepository;

public interface IPayGroupRepository
{
    Task<Result<Guid>> CreatePayGroup(CreatePayGroupRequest request);
    Task<Result<Paginateable<IEnumerable<PayGroupDto>>>> GetPayGroups(int page, int pageSize, string searchQuery,
        Guid? payrollCompanyId = null);
    Task<Result<PayGroupDto>> GetPayGroup(Guid id);

    Task<Result> UpdatePayGroup(Guid id, CreatePayGroupRequest request);
    Task<Result> DeletePayGroup(Guid id, Guid userId);
}