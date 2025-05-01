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
        var designation = mapper.Map<Designation>(request);
        designation.CreatedById = userId;
        designation.CreatedAt = DateTime.UtcNow;
        
        var departments = await context.Departments
            .Where(d => request.DepartmentIds.Contains(d.Id)).ToListAsync();
        
        designation.Departments = departments;
        
        await context.Designations.AddAsync(designation);
        await context.SaveChangesAsync();
        
        return designation.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery)
    {
        var query = context.Designations.
            Include(d => d.Departments).AsQueryable();

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
        var designation = await context.Designations.
            FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);
        return designation is null ? 
            Error.NotFound("Designation.NotFound", "Designation not found") :
            Result.Success(mapper.Map<DesignationDto>(designation));
    }

    public async Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request, Guid userId)
    {
        var designation = await context.Designations.
            Include(d => d.Departments).
            FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);
        if (designation is null)
        {
            return Error.NotFound("Designation.NotFound", "Designation not found");
        }
        mapper.Map(request, designation);
        
        // Fetch the new Departments based on the request
        var departments = await context.Departments
            .Where(d => request.DepartmentIds.Contains(d.Id))
            .ToListAsync();
        
        if (departments.Count != request.DepartmentIds.Count)
        {
            return Error.Validation("Designation.InvalidDepartments", "One or more department IDs are invalid.");
        }
        
        designation.Departments.Clear(); 
        designation.Departments = departments;
        
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
        
        var employees = await context.Employees.FirstOrDefaultAsync(e => e.DesignationId == id);

        if (employees is not null)
        {
            return Error.Validation("Designation.InUse", "Designation is in use.");       
        }
        
        designation.DeletedAt = DateTime.UtcNow;
        designation.LastDeletedById = userId;
        
        context.Designations.Update(designation);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}