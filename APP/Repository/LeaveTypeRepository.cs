using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveTypes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class LeaveTypeRepository(ApplicationDbContext context, IMapper mapper) : ILeaveTypeRepository
{
    public async Task<Result<Guid>> CreateLeaveType(CreateLeaveTypeRequest leaveTypeDto, Guid userId)
    {
        var existingLeaveType = await context.LeaveTypes.FirstOrDefaultAsync(l => l.Name == leaveTypeDto.Name);

        if (existingLeaveType != null)
        {
            return Error.Validation("LeaveType.Exists","Leave Type already exists.");
        }

        var designations = await context.Designations
            .Where(d => leaveTypeDto.DesignationList.Contains(d.Id))
            .ToListAsync();
        
        
        var leaveType = mapper.Map<LeaveType>(leaveTypeDto);
        leaveType.CreatedById = userId;
        leaveType.CreatedAt = DateTime.UtcNow;
        
        leaveType.Designations = designations;
        
        await context.LeaveTypes.AddAsync(leaveType);
        await context.SaveChangesAsync();
        
        return leaveType.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<LeaveTypeDto>>>> GetLeaveTypes(int page, int pageSize, string searchQuery = null)
    {
        var query = context.LeaveTypes
            .Include(d => d.Designations)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(l => l.Name.Contains(searchQuery));
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<LeaveTypeDto>
        );
    }

    public async Task<Result<LeaveTypeDto>> GetLeaveType(Guid id)
    {
        var leaveType = await context.LeaveTypes
            .Include(d => d.Designations)
            .FirstOrDefaultAsync(l=> l.Id == id && l.LastDeletedById == null);
        if (leaveType == null)
        {
            return Error.NotFound("LeaveType.NotFound", "LeaveType not found");
        }
        var leaveTypeDto = mapper.Map<LeaveTypeDto>(leaveType);
        return Result.Success(leaveTypeDto);
    }

    public async Task<Result> UpdateLeaveType(Guid id, CreateLeaveTypeRequest request, Guid userId)
    {
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);
        
        if (leaveType == null)
        {
            return Error.NotFound("LeaveType.NotFound", "LeaveType not found");
        }
        
        mapper.Map(request, leaveType);

        var desingations = await context.Designations
            .Where(d => request.DesignationList.Contains(d.Id))
            .ToListAsync();

        if (desingations.Count != request.DesignationList.Count)
        {
            return Error.Validation("LeaveType.InvalidDesignations", "One or more designation IDs are invalid.");
        }
        leaveType.UpdatedAt = DateTime.UtcNow;
        leaveType.LastDeletedById = userId;
        
        context.LeaveTypes.Update(leaveType);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteLeaveType(Guid id, Guid userId)
    {
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById  == null);

        if (leaveType == null)
        {
            return Error.NotFound("LeaveType.NotFound", "LeaveType not found");
        }
        leaveType.DeletedAt = DateTime.UtcNow;
        leaveType.LastDeletedById = userId;
       
        context.LeaveTypes.Update(leaveType);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}