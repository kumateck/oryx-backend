using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollRuns;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollRunRepository(ApplicationDbContext context, IMapper mapper) : IPayrollRunRepository
{
    public async Task<Result<Guid>> CreatePayrollRun(CreatePayrollRunRequest request)
    {
        var company = await context.PayrollCompanies.AnyAsync(p => p.Id == request.PayrollCompanyId);
        if (!company) return Error.Validation("PayrollCompany.NotFound","Payroll company not found");
        
        var payrollPeriod = await context.PayrollPeriods.AnyAsync(p => p.Id == request.PayrollPeriodId);
        if (!payrollPeriod) return Error.Validation("PayrollPeriod.NotFound", "Payroll period not found");
        
        var payGroup = await context.PayGroups.AnyAsync(p => p.Id == request.PayGroupId);
        if (!payGroup) return Error.Validation("PayGroup.NotFound", "Paygroup not found");
        
        var parentRun = await context.PayrollRuns.AnyAsync(p => p.Id == request.ParentRunId);
        if (!parentRun) return Error.Validation("ParentRun.NotFound", "ParentRun not found");
        
        var payrollRun = mapper.Map<PayrollRun>(request);
        await context.PayrollRuns.AddAsync(payrollRun);
        await context.SaveChangesAsync();
        return payrollRun.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollRunSummaryDto>>>> GetPayrollRuns(int page, int pageSize,
        string searchQuery, Guid? payrollCompanyId = null, Guid? payGroupId = null,
        PayrollRunStatus? status = null)
    {
        var query = context.PayrollRuns.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }
        
        if (payrollCompanyId.HasValue) query = query.Where(p => p.PayrollCompanyId == payrollCompanyId.Value);
        if (payGroupId.HasValue) query = query.Where(p => p.PayGroupId == payGroupId.Value);
        if (status.HasValue) query = query.Where(p => p.RunStatus == status.Value);
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, 
            mapper.Map<PayrollRunSummaryDto>);
    }

    public async Task<Result<PayrollRunDto>> GetPayrollRun(Guid id)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(p => p.Id == id);
        return payrollRun is null ? Error.Validation("PayrollRun.NotFound", "PayrollRun not found")
            : mapper.Map<PayrollRunDto>(payrollRun);
    }

    public async Task<Result> TransitionPayrollRun(Guid id, PayrollRunTransitionRequest request, Guid userId)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(p => p.Id == id);
        if (payrollRun is null) return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        
        var now = DateTime.UtcNow;

        switch (request.TargetStatus)
        {
            case PayrollRunStatus.PrecheckPassed:
                if (payrollRun.RunStatus != PayrollRunStatus.Draft 
                    && payrollRun.RunStatus != PayrollRunStatus.PrecheckFailed)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must be in Draft or PrecheckFailed to precheck");
                payrollRun.PrecheckCompletedAt = now;
                break;
            case PayrollRunStatus.Calculated:
                if (payrollRun.RunStatus != PayrollRunStatus.PrecheckPassed)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must pass precheck before calculation");
                
                payrollRun.CalculatedAt = now;
                break;
            case PayrollRunStatus.ReadyForApproval:
                if (payrollRun.RunStatus != PayrollRunStatus.Calculated
                    && payrollRun.RunStatus != PayrollRunStatus.ValidationFailed)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must be calculated or in validation-failed to submit for approval");
                payrollRun.ValidatedAt = now;
                break;
            case PayrollRunStatus.Approved:
                if (payrollRun.RunStatus != PayrollRunStatus.ReadyForApproval)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must be ready for approval");
                payrollRun.ApprovedAt = now;
                break;
            case PayrollRunStatus.PaymentReleased:
                if (payrollRun.RunStatus != PayrollRunStatus.Approved)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must be approved before payment release");
                payrollRun.PaymentReleasedAt = now;
                break;
            case PayrollRunStatus.Posted:
                if (payrollRun.RunStatus != PayrollRunStatus.PaymentReleased)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must have payment released before posting");
                payrollRun.PostedAt = now;
                break;
            case PayrollRunStatus.Closed:
                if (payrollRun.RunStatus != PayrollRunStatus.Posted)
                    return Error.Validation("PayrollRun.InvalidTransition",
                        "Run must be posted before closing");
                payrollRun.ClosedAt = now;
                break;
            default:
                return Error.Validation("PayrollRun.InvalidTransition",
                    $"Cannot transition to {request.TargetStatus.ToString()} ");
        }
        
        payrollRun.RunStatus = request.TargetStatus;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> CancelPayrollRun(Guid id, CancelPayrollRunRequest request, Guid userId)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(p => p.Id == id);
        if (payrollRun is null) return Error.NotFound("PayrollRun.NotFound",
            "Payroll run not found");

        if (payrollRun.RunStatus is PayrollRunStatus.Closed or PayrollRunStatus.Cancelled)
            return Error.Validation("PayrollRun.CannotCancel", "Closed or already" +
                    "cancelled runs cannot be cancelled");

        payrollRun.RunStatus = PayrollRunStatus.Cancelled;
        payrollRun.CancelledAt = DateTime.UtcNow;
        payrollRun.Notes = request.Reason;
        
        await context.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> DeletePayrollRun(Guid id, Guid userId)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(p => p.Id == id);
        if (payrollRun is null) return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        
        if (payrollRun.RunStatus != PayrollRunStatus.Draft)
            return Error.Validation("PayrollRun.CannotDelete",
                "Only draft runs can be deleted");
        
        payrollRun.LastDeletedById = userId;
        payrollRun.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}