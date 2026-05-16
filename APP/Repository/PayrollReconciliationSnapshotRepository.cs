using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollReconciliationSnapshot;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollReconciliationSnapshotRepository(ApplicationDbContext context, IMapper mapper) : IPayrollReconciliationSnapshotRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayrollReconciliationSnapshotDto>>>> GetReconciliationSnapshots(int page, int pageSize,
        string searchQuery, Guid? payrollCompanyId = null,
        Guid? payrollPeriodId = null, PayrollReconciliationStatus? status = null)
    {
        var query = context.PayrollReconciliationSnapshots.AsQueryable();
        
        if (payrollCompanyId.HasValue) query = query.Where(p => p.PayrollCompanyId == payrollCompanyId);

        if (Enum.TryParse<PayrollReconciliationStatus>(searchQuery, true, out var reconciliationStatus))
        {
            query = query.Where(p => p.Status == reconciliationStatus);
        }
        
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollReconciliationSnapshotDto>);
    }

    public async Task<Result<PayrollReconciliationSnapshotDto>> GetReconciliationSnapshot(Guid id)
    {
        var snapshot = await context.PayrollReconciliationSnapshots.FirstOrDefaultAsync(p => p.Id == id);
        return snapshot is null ? Error.NotFound("PayrollReconciliationSnapshot.NotFound",
                "Payroll reconciliation snapshot not found")
            : mapper.Map<PayrollReconciliationSnapshotDto>(snapshot);
    }
}