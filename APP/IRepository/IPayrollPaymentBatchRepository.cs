using APP.Utils;
using DOMAIN.Entities.PayrollPaymentBatches;
using SHARED;

namespace APP.IRepository;

public interface IPayrollPaymentBatchRepository
{
    Task<Result<Guid>> CreatePayrollPaymentBatch(Guid payrollRunId);
    Task<Result<Paginateable<IEnumerable<PayrollPaymentBatchDto>>>> GetPaymentBatches(int page, int pageSize,
        string searchQuery, Guid payrollRunId, PayrollPaymentBatchStatus? status = null);
    Task<Result<PayrollPaymentBatchDto>> GetPaymentBatch(Guid id, Guid payrollRunId);
    Task<Result> ReleasePaymentBatch(Guid id, Guid payrollRunId, ReleasePaymentBatchRequest request, Guid userId);
    Task<Result> CancelPaymentBatch(Guid id, Guid payrollRunId, Guid userId);
}