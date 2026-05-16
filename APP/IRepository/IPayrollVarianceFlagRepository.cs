using APP.Utils;
using DOMAIN.Entities.PayrollVarianceFlags;
using SHARED;

namespace APP.IRepository;

public interface IPayrollVarianceFlagRepository
{
    Task<Result<Paginateable<IEnumerable<PayrollVarianceFlagDto>>>> GetVarianceFlags(Guid payrollRunId, int page, int pageSize,
        string searchQuery, PayrollVarianceSeverity? severity = null, PayrollVarianceDisposition? disposition = null);
    Task<Result<PayrollVarianceFlagDto>> GetVarianceFlag(Guid id);
    Task<Result> ReviewVarianceFlag(Guid id, ReviewVarianceFlagRequest request, Guid userId);
}