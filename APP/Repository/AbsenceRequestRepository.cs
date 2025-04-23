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
        var existingLeaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l => l.StartDate == request.StartDate
                                      && l.EndDate == request.EndDate && l.EmployeeId == request.EmployeeId);
        
        if (existingLeaveRequest != null)
        {
            return Error.Validation("LeaveRequest.Exists", "Leave request already exists.");
        }

        if (request.StartDate > request.EndDate)
        {
            return Error.Validation("LeaveRequest.InvalidDates", "Start date must be before end date.");
        }
        
        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.LastDeletedById == null);;
        if (existingEmployee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
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