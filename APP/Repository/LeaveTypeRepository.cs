using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveTypes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class LeaveTypeRepository(ApplicationDbContext context, IMapper mapper) : ILeaveTypeRepository
{
    public async Task<Result<Paginateable<IEnumerable<LeaveTypeDto>>>> GetLeaveTypes(int page, int pageSize, string searchQuery = null,
        Guid? designationId = null)
    {
        var query = context.LeaveTypes
            .AsSplitQuery()
            .Include(d => d.Designations)
            .Where(l => l.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(l => l.Name.Contains(searchQuery));
        }

        if (designationId.HasValue)
        {
            query = query.Where(l => l.Designations.Any(d => d.Id == designationId.Value));
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
            .FirstOrDefaultAsync(l => l.Id == id);
        if (leaveType == null)
        {
            return Error.NotFound("LeaveType.NotFound", "LeaveType not found");
        }
        var leaveTypeDto = mapper.Map<LeaveTypeDto>(leaveType);
        return Result.Success(leaveTypeDto);
    }

}
