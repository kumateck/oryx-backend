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
    public async Task<Result<Guid>> CreateDesignation(CreateDesignationRequest request)
    {
        var requestedDeptIds = request.DepartmentIds.Distinct().ToList();

        // Load existing designations with the same name
        var existingDesignations = await context.Designations
            .Include(d => d.Departments)
            .Where(d => d.Name == request.Name)
            .ToListAsync();

        // Find conflicting departments
        var conflictingDepartments = existingDesignations
            .SelectMany(d => d.Departments)
            .Where(dep => requestedDeptIds.Contains(dep.Id))
            .Distinct()
            .ToList();

        if (conflictingDepartments.Count != 0)
        {
            var conflictNames = string.Join(", ", conflictingDepartments.Select(d => d.Name));

            return Error.Validation(
                "Designation.DepartmentConflict",
                $"The following department(s) are already assigned to the designation '{request.Name}': {conflictNames}."
            );
        }

        // Create a new designation
        var designation = mapper.Map<Designation>(request);

        var departments = await context.Departments
            .Where(d => requestedDeptIds.Contains(d.Id))
            .ToListAsync();

        designation.Departments = departments;

        await context.Designations.AddAsync(designation);
        await context.SaveChangesAsync();

        return designation.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery, Guid? departmentId = null)
    {
        var query = context.Designations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(q => q.Departments.Any(d => d.Id == departmentId.Value));
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
            FirstOrDefaultAsync(d => d.Id == id);
        return designation is null ?
            Error.NotFound("Designation.NotFound", "Designation not found") :
            Result.Success(mapper.Map<DesignationDto>(designation));
    }

    public async Task<Result<List<DesignationWithEmployeesDto>>> GetDesignationByDepartment(Guid departmentId)
    {
        var designations = await context.Designations
            .AsNoTracking()
            .Include(d => d.Departments)
            .Where(d => d.Departments.Any(dep => dep.Id == departmentId))
            .ToListAsync();

        var employeeQuery = context.Employees
            .AsNoTracking()
            .Include(e => e.ReportingManager)
            .Where(e => e.DesignationId != null);

        var employees = await employeeQuery.ToListAsync();

        var result = designations.Select(designation =>
        {
            var relatedEmployees = employees
                .Where(e => e.DesignationId == designation.Id)
                .Select(e => new EmployeeWithManagerDto
                {
                    Id = e.Id,
                    FullName = $"{e.FirstName} {e.LastName}",
                    Email = e.Email,
                    Manager = new ManagerDto
                    {
                        ReportingManagerId = e.ReportingManagerId,
                        ReportingManagerName = e.ReportingManager != null
                            ? $"{e.ReportingManager.FirstName} {e.ReportingManager.LastName}"
                            : null
                    }
                }).ToList();

            return new DesignationWithEmployeesDto
            {
                Id = designation.Id,
                Name = designation.Name,
                Employees = relatedEmployees
            };
        }).ToList();

        return Result.Success(result);
    }

    public async Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request)
    {
        var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == id);
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

        context.Designations.Update(designation);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteDesignation(Guid id, Guid userId)
    {
        var designation = await context.Designations
            .FirstOrDefaultAsync(d => d.Id == id);
        if (designation is null)
        {
            return Error.NotFound("Designation.NotFound", "Designation not found");
        }

        var employees = await context.Employees
            .FirstOrDefaultAsync(e => e.DesignationId == id);

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