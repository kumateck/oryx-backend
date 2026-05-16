using APP.Utils;
using DOMAIN.Entities.PayrollReconciliationSnapshot;
using SHARED;

namespace APP.IRepository;

public interface IPayrollReconciliationSnapshotRepository
{
    Task<Result<Paginateable<IEnumerable<PayrollReconciliationSnapshotDto>>>> GetReconciliationSnapshots(int page, int pageSize,
        string searchQuery, Guid? payrollCompanyId = null,
        Guid? payrollPeriodId = null, PayrollReconciliationStatus? status = null);
    Task<Result<PayrollReconciliationSnapshotDto>> GetReconciliationSnapshot(Guid id);
}