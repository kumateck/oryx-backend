using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ShiftTypes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ShiftTypeRepository(ApplicationDbContext context, IMapper mapper) : IShiftTypeRepository
{
    public async Task<Result<Guid>> CreateShiftType(CreateShiftTypeRequest request, Guid userId)
    {
        var existingShiftType = await context.ShiftTypes
            .FirstOrDefaultAsync(s => s.ShiftName == request.ShiftName && s.DeletedAt == null );

        if (existingShiftType != null)
        {
            return Error.Validation("ShiftType.Exists", "Shift type already exists.");
        }

        if (request.StartTime > request.EndTime)
        {
            return Error.Validation("ShiftType.InvalidTime", "Start time must be before end time.");
        }

        if (request.ApplicableDays.Count == 0)
        {
            return Error.Validation("ShiftType.InvalidDays", "At least one day must be selected.");
        }
        
        var shiftType = mapper.Map<ShiftType>(request);
        shiftType.CreatedById = userId;
        shiftType.CreatedAt = DateTime.UtcNow;
        
        await context.ShiftTypes.AddAsync(shiftType);
        await context.SaveChangesAsync();
        
        return shiftType.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ShiftTypeDto>>>> GetShiftTypes(int page, int pageSize, string searchQuery)
    {
        var query = context.ShiftTypes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.ShiftName);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftTypeDto>);
    }

    public async Task<Result<ShiftTypeDto>> GetShiftType(Guid id)
    {
        var shiftType = await context.ShiftTypes.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
        if (shiftType is null)
        {
            return Error.NotFound("ShiftType.NotFound", "Shift type is not found");
        }
        var shiftTypeDto = mapper.Map<ShiftTypeDto>(shiftType);
        return Result.Success(shiftTypeDto);
    }

    public async Task<Result> UpdateShiftType(Guid id, CreateShiftTypeRequest request, Guid userId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteShiftType(Guid id, Guid userId)
    {
        var shiftType = await context.ShiftTypes.FirstOrDefaultAsync(s => s.Id == id);

        if (shiftType is null)
        {
            return Error.NotFound("ShiftType.NotFound", "Shift type is not found");
        }
        shiftType.LastDeletedById = userId;
        shiftType.DeletedAt = DateTime.UtcNow;
        
        context.ShiftTypes.Update(shiftType);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}