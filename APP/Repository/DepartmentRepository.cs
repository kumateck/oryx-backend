using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Departments.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class DepartmentRepository(ApplicationDbContext context, IMapper mapper) : IDepartmentRepository
{
    public async Task<Result<Guid>> CreateDepartment(CreateDepartmentRequest request, Guid userId)
    {
        var department = mapper.Map<Department>(request);
        department.CreatedById = userId;
        department.CreatedAt = DateTime.UtcNow;
        await context.Departments.AddAsync(department);
        await context.SaveChangesAsync();

        return department.Id;
    }

    public async Task<Result<DepartmentDto>> GetDepartment(Guid departmentId)
    {
        var department = await context.Departments
            .Include(d => d.Warehouse)
            .FirstOrDefaultAsync(d => d.Id == departmentId);

        return department is null
            ? Error.NotFound("Department.NotFound", "Department not found")
            : mapper.Map<DepartmentDto>(department);
    }
    
    public async Task<Result<Paginateable<IEnumerable<DepartmentDto>>>> GetDepartments(int page, int pageSize, string searchQuery)
    {
        var query = context.Departments
            .Include(d => d.Warehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, d => d.Name, d => d.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DepartmentDto>
        );
    }
    
    public async Task<Result> UpdateDepartment(CreateDepartmentRequest request, Guid departmentId, Guid userId)
    {
        var existingDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (existingDepartment is null)
        {
            return Error.NotFound("Department.NotFound", "Department not found");
        }

        mapper.Map(request, existingDepartment);
        existingDepartment.LastUpdatedById = userId;

        context.Departments.Update(existingDepartment);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Department (soft delete)
    public async Task<Result> DeleteDepartment(Guid departmentId, Guid userId)
    {
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
        {
            return Error.NotFound("Department.NotFound", "Department not found");
        }

        department.DeletedAt = DateTime.UtcNow;
        department.LastDeletedById = userId;

        context.Departments.Update(department);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
