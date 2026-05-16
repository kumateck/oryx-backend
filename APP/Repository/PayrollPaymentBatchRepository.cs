using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollPaymentBatches;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollPaymentBatchRepository(ApplicationDbContext context, IMapper mapper) : IPayrollPaymentBatchRepository
{
    public async Task<Result<Guid>> CreatePayrollPaymentBatch(Guid payrollRunId)
    {
        var run = await context.PayrollRuns.FirstOrDefaultAsync(p => p.Id == payrollRunId);
        if (run is null) return Error.NotFound("PayrollRun.NotFound","Payroll run not found");

        var batch = new PayrollPaymentBatch
        {
            PayrollRunId = payrollRunId,
            TotalAmount = run.NetTotal
        };
        
        await context.PayrollPaymentBatches.AddAsync(batch);
        await context.SaveChangesAsync();
        
        return batch.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollPaymentBatchDto>>>> GetPaymentBatches(int page,
        int pageSize, string searchQuery,
        Guid payrollRunId,
        PayrollPaymentBatchStatus? status = null)
    {
        var query = context.PayrollPaymentBatches
            .Where(q=> q.PayrollRunId == payrollRunId)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,q=>q.Code);
        }

        if (Enum.TryParse<PayrollPaymentBatchStatus>(searchQuery, true, out var statusEnum))
        {
            query = query.Where(p => p.BatchStatus == statusEnum);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollPaymentBatchDto>);
    }

    public async Task<Result<PayrollPaymentBatchDto>> GetPaymentBatch(Guid id, Guid payrollRunId)
    {
        var batch = await context.PayrollPaymentBatches.FirstOrDefaultAsync(p => p.Id == id && p.PayrollRunId == payrollRunId);
        return batch is null ? Error.NotFound("PayrollPaymentBatch.NotFound", "Payroll payment batch not found") :
            mapper.Map<PayrollPaymentBatchDto>(batch);
    }

    public async Task<Result> ReleasePaymentBatch(Guid id, Guid payrollRunId, ReleasePaymentBatchRequest request,
        Guid userId)
    {
        var batch = await context.PayrollPaymentBatches.FirstOrDefaultAsync(p => p.Id == id && p.PayrollRunId == payrollRunId);
        if (batch is null) return Error.NotFound("PayrollPaymentBatch.NotFound", "Payroll payment batch not found");
        
        if (batch.BatchStatus != PayrollPaymentBatchStatus.PendingRelease)
            return Error.Validation("PayrollPaymentBatch.NotPendingRelease",
                "Payroll payment batch must be in pending release status");
        
        batch.FailureReason = request.Comment;
        batch.BatchStatus = PayrollPaymentBatchStatus.Released;
        batch.ReleasedAt = DateTime.UtcNow;
        batch.ReleasedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CancelPaymentBatch(Guid id, Guid payrollRunId, Guid userId)
    {
        var batch = await context.PayrollPaymentBatches.FirstOrDefaultAsync(p => p.Id == id && p.PayrollRunId == payrollRunId);
        if (batch is null) return Error.NotFound("PayrollPaymentBatch.NotFound","Payroll payment batch not found");

        if (batch.BatchStatus is PayrollPaymentBatchStatus.Settled or PayrollPaymentBatchStatus.Cancelled)
            return Error.Validation("PayrollPaymentBatch.CannotCancel", "Settled or already cancelled bat hes cannot be cancelled");

        batch.BatchStatus = PayrollPaymentBatchStatus.Cancelled;
        batch.ReleasedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();

    }
}