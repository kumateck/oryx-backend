using APP.Utils;
using DOMAIN.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PayrollRepository
{
    public async Task<Result<Guid>> CreatePayrollRun(CreatePayrollRunRequest request, Guid userId)
    {
        if (request.PeriodEnd <= request.PeriodStart)
        {
            return Error.Validation("PayrollRun.InvalidPeriod", "Period end must be after period start");
        }

        var overlapping = await context.PayrollRuns.AnyAsync(r =>
            r.Status != PayrollRunStatus.Cancelled
            && r.PeriodStart < request.PeriodEnd
            && r.PeriodEnd > request.PeriodStart);

        if (overlapping)
        {
            return Error.Conflict("PayrollRun.Overlapping", "A payroll run already exists for an overlapping period");
        }

        var payrollRun = mapper.Map<PayrollRun>(request);
        payrollRun.CreatedById = userId;

        await context.PayrollRuns.AddAsync(payrollRun);
        await context.SaveChangesAsync();

        return payrollRun.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollRunDto>>>> GetPayrollRuns(
        int page, int pageSize, PayrollRunStatus? status)
    {
        var query = context.PayrollRuns
            .Include(r => r.Payslips)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        query = query.OrderByDescending(r => r.PeriodStart);

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PayrollRunDto>);
    }

    public async Task<Result<PayrollRunDto>> GetPayrollRun(Guid id)
    {
        var payrollRun = await context.PayrollRuns
            .Include(r => r.Payslips)
            .FirstOrDefaultAsync(r => r.Id == id);

        return payrollRun is null
            ? Error.NotFound("PayrollRun.NotFound", "Payroll run not found")
            : mapper.Map<PayrollRunDto>(payrollRun);
    }

    public async Task<Result> SubmitPayrollRunForApproval(Guid id, Guid userId)
    {
        var payrollRun = await context.PayrollRuns
            .Include(r => r.Payslips)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (payrollRun is null)
        {
            return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        }

        if (payrollRun.Status != PayrollRunStatus.Draft)
        {
            return Error.Validation("PayrollRun.InvalidStatus", "Only a draft payroll run can be submitted for approval");
        }

        var activeEmployeeIds = await context.Employees
            .Where(e => e.Status == DOMAIN.Entities.Employees.EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync();

        if (activeEmployeeIds.Count == 0)
        {
            return Error.Validation("PayrollRun.NoEmployees", "There are no active employees to generate payslips for");
        }

        var newPayslips = new List<Payslip>();
        foreach (var employeeId in activeEmployeeIds)
        {
            try
            {
                var payslip = await calculationService.CalculatePayslip(employeeId, payrollRun.PeriodStart, payrollRun.PeriodEnd);
                payslip.PayrollRunId = payrollRun.Id;
                newPayslips.Add(payslip);
            }
            catch (InvalidOperationException)
            {
                // Employee has no compensation record covering this period - skip them.
            }
        }

        if (newPayslips.Count == 0)
        {
            return Error.Validation("PayrollRun.NoPayslips", "No employees have a compensation record covering this period");
        }

        await context.Payslips.AddRangeAsync(newPayslips);
        payrollRun.Status = PayrollRunStatus.PendingApproval;
        context.PayrollRuns.Update(payrollRun);
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(nameof(PayrollRun), payrollRun.Id);

        return Result.Success();
    }

    public async Task<Result> MarkPayrollRunProcessed(Guid id)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (payrollRun is null)
        {
            return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        }

        if (payrollRun.Status != PayrollRunStatus.Approved)
        {
            return Error.Validation("PayrollRun.InvalidStatus", "Only an approved payroll run can be marked as processed");
        }

        payrollRun.Status = PayrollRunStatus.Processed;
        context.PayrollRuns.Update(payrollRun);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CancelPayrollRun(Guid id, Guid userId)
    {
        var payrollRun = await context.PayrollRuns.FirstOrDefaultAsync(r => r.Id == id);
        if (payrollRun is null)
        {
            return Error.NotFound("PayrollRun.NotFound", "Payroll run not found");
        }

        if (payrollRun.Status is PayrollRunStatus.Processed)
        {
            return Error.Validation("PayrollRun.CannotCancel", "Cannot cancel a payroll run that has already been processed");
        }

        payrollRun.Status = PayrollRunStatus.Cancelled;
        payrollRun.LastUpdatedById = userId;
        context.PayrollRuns.Update(payrollRun);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
