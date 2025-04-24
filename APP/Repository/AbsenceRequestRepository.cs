using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AbsenceRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class AbsenceRequestRepository(ApplicationDbContext context, IMapper mapper) : IAbsenceRequestRepository
{
    public async Task<Result<Guid>> CreateAbsenceRequest(CreateAbsenceRequest request, Guid userId)
    { 
        
        var totalDays = (request.EndDate - request.StartDate).TotalDays + 1;
        if (totalDays < 3)
            return Error.Validation("AbsenceRequest.InvalidDuration", "Absence requests must be at least 3 days.");

        // Check for existing absence request
        var exists = await context.AbsenceRequests
            .AnyAsync(a => a.EmployeeId == request.EmployeeId &&
                           a.StartDate == request.StartDate &&
                           a.EndDate == request.EndDate);
        if (exists)
            return Error.Validation("AbsenceRequest.Exists", "An absence request already exists for this period.");

        // Check employee
        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.LastDeletedById == null);
        if (employee is null)
            return Error.NotFound("Employee.NotFound", "Employee not found.");

        var absenceType = await context.LeaveTypes.FindAsync(request.LeaveTypeId);
        if (absenceType is null)
            return Error.NotFound("AbsenceType.NotFound", "Absence type not found.");

        var paidDays = 0;
        var unpaidDays = 0;

        if (absenceType.IsPaid)
        {
            var balanceDeducted = 0;
            if (absenceType.DeductFromBalance)
            {
                if (absenceType.DeductionLimit > 0)
                {
                    paidDays = (int)Math.Min(totalDays, absenceType.DeductionLimit ?? 0);
                    var remaining = (int) totalDays - paidDays;

                    balanceDeducted = Math.Min(employee.AnnualLeaveDays, remaining);
                    unpaidDays = remaining - balanceDeducted;

                    employee.AnnualLeaveDays -= balanceDeducted;
                }
                else
                {
                    balanceDeducted = Math.Min(employee.AnnualLeaveDays, (int)totalDays);
                    unpaidDays = (int)totalDays - balanceDeducted;

                    paidDays = balanceDeducted;
                    employee.AnnualLeaveDays -= balanceDeducted;
                }
            }
            else
            {
                paidDays = (int)totalDays;
                unpaidDays = 0;
                balanceDeducted = 0;
            }
        }
        else
        {
            paidDays = 0;
            unpaidDays = (int)totalDays;
        }
        var requestEntity = mapper.Map<AbsenceRequest>(request);
        requestEntity.CreatedById = userId;
        requestEntity.CreatedAt = DateTime.UtcNow;
            
        await context.AbsenceRequests.AddAsync(requestEntity);
        await context.SaveChangesAsync();
            
        return requestEntity.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<AbsenceRequestDto>>>> GetAbsenceRequests(int page, int pageSize, string searchQuery)
    {
        var query = context.AbsenceRequests
            .Include(l => l.Employee)
            .Where(l => l.LastDeletedById == null)
            .Include(l => l.LeaveType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Employee.FullName);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.LeaveType.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<AbsenceRequestDto>);
    }

    public async Task<Result<AbsenceRequestDto>> GetAbsenceRequest(Guid id)
    {
        var absenceRequest = await context.AbsenceRequests
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);
        
        return absenceRequest == null ? 
            Error.NotFound("AbsenceRequest.NotFound", "AbsenceRequest not found") : 
            Result.Success(mapper.Map<AbsenceRequestDto>(absenceRequest));
    }

    public async Task<Result> UpdateAbsenceRequest(Guid id, CreateAbsenceRequest request, Guid userId)
    {
        var absenceRequest = await context.AbsenceRequests
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);

        if (absenceRequest == null)
        {
            return Error.NotFound("AbsenceRequest.NotFound", "AbsenceRequest not found");
        }
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == request.LeaveTypeId && l.LastDeletedById == null);

        if (leaveType is null)
        {
            return Error.NotFound("AbsenceType.NotFound", "Absence type not found");
        }

        mapper.Map(request, absenceRequest);
        
        absenceRequest.LastUpdatedById = userId;
        absenceRequest.UpdatedAt = DateTime.UtcNow;
        
        context.AbsenceRequests.Update(absenceRequest);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAbsenceRequest(Guid id, Guid userId)
    {
        var absenceRequest = await context.AbsenceRequests
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);

        if (absenceRequest == null)
        {
            return Error.NotFound("AbsenceRequest.NotFound", "AbsenceRequest not found");
        }
        
        absenceRequest.DeletedAt = DateTime.UtcNow;
        absenceRequest.LastDeletedById = userId;
       
        context.AbsenceRequests.Update(absenceRequest);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}