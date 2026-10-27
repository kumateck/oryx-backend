using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Designations;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class DesignationRepository(ApplicationDbContext context, IMapper mapper) : IDesignationRepository
{
    public async Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery, Guid? departmentId = null)
    {
        var query = context.Designations.Include(d => d.Departments).AsQueryable();

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
        var designation = await context.Designations.Include(d => d.Departments).
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

}
