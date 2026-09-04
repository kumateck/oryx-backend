using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ShiftAssignments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ShiftCategoryRepository(ApplicationDbContext context, IMapper mapper) : IShiftCategoryRepository
{
    public async Task<Result<Guid>> CreateShiftCategory(CreateShiftCategoryRequest request)
    {
        var existing = await context.ShiftCategories
            .FirstOrDefaultAsync(c => c.Name == request.Name);

        if (existing is not null)
        {
            return Error.Validation("ShiftCategory.Exists", "Shift category already exists.");
        }

        var shiftCategory = mapper.Map<ShiftCategory>(request);

        await context.ShiftCategories.AddAsync(shiftCategory);
        await context.SaveChangesAsync();

        return shiftCategory.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ShiftCategoryDto>>>> GetShiftCategories(
        int page, int pageSize, string searchQuery)
    {
        var query = context.ShiftCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftCategoryDto>);
    }

    public async Task<Result<ShiftCategoryDto>> GetShiftCategory(Guid id)
    {
        var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (shiftCategory is null)
        {
            return Error.NotFound("ShiftCategory.NotFound", "Shift category is not found");
        }

        return Result.Success(mapper.Map<ShiftCategoryDto>(shiftCategory));
    }

    public async Task<Result> UpdateShiftCategory(Guid id, CreateShiftCategoryRequest request)
    {
        var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (shiftCategory is null)
        {
            return Error.NotFound("ShiftCategory.NotFound", "Shift category is not found");
        }

        mapper.Map(request, shiftCategory);

        context.ShiftCategories.Update(shiftCategory);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteShiftCategory(Guid id, Guid userId)
    {
        var shiftCategory = await context.ShiftCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (shiftCategory is null)
        {
            return Error.NotFound("ShiftCategory.NotFound", "Shift category is not found");
        }

        var inUse = await context.ShiftAssignments.AnyAsync(sa => sa.ShiftCategoryId == id);
        if (inUse)
        {
            return Error.Validation("ShiftCategory.InUse", "Shift category is in use by a shift assignment.");
        }

        shiftCategory.LastDeletedById = userId;
        shiftCategory.DeletedAt = DateTime.UtcNow;

        context.ShiftCategories.Update(shiftCategory);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
