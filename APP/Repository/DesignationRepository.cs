using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Designations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class DesignationRepository(ApplicationDbContext context, IMapper mapper) : IDesignationRepository
{
    public async Task<Result<Guid>> CreateDesignation(CreateDesignationRequest request, Guid userId)
    {
        var existingDesignation = await context.Designations.FirstOrDefaultAsync(d => d.Name == request.Name);
        if (existingDesignation != null)
        {
            return Error.Validation("Name", "Designation already exists");
        }
        var designation = mapper.Map<Designation>(request);
        designation.CreatedById = userId;
        
        context.Designations.Add(designation);
        await context.SaveChangesAsync();
        
        return designation.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery)
    {
        var query = context.Designations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DesignationDto>);
    }

    public async Task<Result<DesignationDto>> GetDesignation(Guid id)
    {
        var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == id);
        return designation is null ? Error.NotFound("Designation.NotFound", "Designation not found") :
            Result.Success(mapper.Map<DesignationDto>(designation));
    }

    public async Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request, Guid userId)
    {
        var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == id);
        if (designation is null)
        {
            return Error.NotFound("Designation.NotFound", "Designation not found");
        }
        mapper.Map(request, designation);
        designation.LastUpdatedById = userId;
        designation.UpdatedAt = DateTime.UtcNow;
        context.Designations.Update(designation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteDesignation(Guid id, Guid userId)
    {
        var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == id);
        if (designation is null)
        {
            return Error.NotFound("Designation.NotFound", "Designation not found");
        }
        designation.DeletedAt = DateTime.UtcNow;
        designation.LastDeletedById = userId;
        context.Designations.Update(designation);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}