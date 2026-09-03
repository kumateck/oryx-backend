using APP.Utils;
using DOMAIN.Entities.Payroll;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PayrollRepository
{
    public async Task<Result<Guid>> CreateOrUpdateCompensation(CreateEmployeeCompensationRequest request, Guid userId)
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var current = await context.EmployeeCompensations
            .FirstOrDefaultAsync(c => c.EmployeeId == request.EmployeeId && c.EffectiveTo == null);

        if (current is not null)
        {
            current.EffectiveTo = request.EffectiveFrom;
            context.EmployeeCompensations.Update(current);
        }

        var compensation = mapper.Map<EmployeeCompensation>(request);
        compensation.CreatedById = userId;

        await context.EmployeeCompensations.AddAsync(compensation);
        await context.SaveChangesAsync();

        return compensation.Id;
    }

    public async Task<Result<EmployeeCompensationDto>> GetCurrentCompensation(Guid employeeId)
    {
        var compensation = await context.EmployeeCompensations
            .Include(c => c.Employee)
            .Include(c => c.PayGrade)
            .Include(c => c.Allowances)
            .Where(c => c.EmployeeId == employeeId && c.EffectiveTo == null)
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        return compensation is null
            ? Error.NotFound("Compensation.NotFound", "No active compensation record found for this employee")
            : mapper.Map<EmployeeCompensationDto>(compensation);
    }

    public async Task<Result<Paginateable<IEnumerable<EmployeeCompensationDto>>>> GetCompensationHistory(
        Guid employeeId, int page, int pageSize)
    {
        var query = context.EmployeeCompensations
            .Include(c => c.Employee)
            .Include(c => c.PayGrade)
            .Include(c => c.Allowances)
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.EffectiveFrom)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<EmployeeCompensationDto>);
    }

    public async Task<Result<Guid>> CreatePayrollDeduction(CreatePayrollDeductionRequest request)
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var deduction = mapper.Map<PayrollDeduction>(request);
        await context.PayrollDeductions.AddAsync(deduction);
        await context.SaveChangesAsync();

        return deduction.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollDeductionDto>>>> GetPayrollDeductions(
        Guid? employeeId, int page, int pageSize)
    {
        var query = context.PayrollDeductions
            .Include(d => d.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(d => d.EmployeeId == employeeId.Value);
        }

        query = query.OrderByDescending(d => d.EffectiveFrom);

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PayrollDeductionDto>);
    }

    public async Task<Result> DeletePayrollDeduction(Guid id, Guid userId)
    {
        var deduction = await context.PayrollDeductions.FirstOrDefaultAsync(d => d.Id == id);
        if (deduction is null)
        {
            return Error.NotFound("PayrollDeduction.NotFound", "Payroll deduction not found");
        }

        deduction.DeletedAt = DateTime.UtcNow;
        deduction.LastDeletedById = userId;
        context.PayrollDeductions.Update(deduction);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreatePayrollAddition(CreatePayrollAdditionRequest request)
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var addition = mapper.Map<PayrollAddition>(request);
        await context.PayrollAdditions.AddAsync(addition);
        await context.SaveChangesAsync();

        return addition.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollAdditionDto>>>> GetPayrollAdditions(
        Guid? employeeId, int page, int pageSize)
    {
        var query = context.PayrollAdditions
            .Include(a => a.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        query = query.OrderByDescending(a => a.EffectiveFrom);

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PayrollAdditionDto>);
    }

    public async Task<Result> DeletePayrollAddition(Guid id, Guid userId)
    {
        var addition = await context.PayrollAdditions.FirstOrDefaultAsync(a => a.Id == id);
        if (addition is null)
        {
            return Error.NotFound("PayrollAddition.NotFound", "Payroll addition not found");
        }

        addition.DeletedAt = DateTime.UtcNow;
        addition.LastDeletedById = userId;
        context.PayrollAdditions.Update(addition);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateTaxRelief(CreateEmployeeTaxReliefRequest request)
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var relief = mapper.Map<EmployeeTaxRelief>(request);
        await context.EmployeeTaxReliefs.AddAsync(relief);
        await context.SaveChangesAsync();

        return relief.Id;
    }

    public async Task<Result<List<EmployeeTaxReliefDto>>> GetTaxReliefs(Guid employeeId)
    {
        var reliefs = await context.EmployeeTaxReliefs
            .Include(r => r.Employee)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync();

        return mapper.Map<List<EmployeeTaxReliefDto>>(reliefs);
    }

    public async Task<Result> DeleteTaxRelief(Guid id, Guid userId)
    {
        var relief = await context.EmployeeTaxReliefs.FirstOrDefaultAsync(r => r.Id == id);
        if (relief is null)
        {
            return Error.NotFound("EmployeeTaxRelief.NotFound", "Tax relief not found");
        }

        relief.DeletedAt = DateTime.UtcNow;
        relief.LastDeletedById = userId;
        context.EmployeeTaxReliefs.Update(relief);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
