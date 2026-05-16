using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.EmployeePayrollProfiles;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class EmployeePayrollProfileRepository(ApplicationDbContext context, IMapper mapper) : IEmployeePayrollProfileRepository
{
    public async Task<Result<Guid>> CreateEmployeePayrollProfile(CreateEmployeePayrollProfileRequest request)
    {
        var employee = await context.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employee) return Error.NotFound("Employee.NotFound", "Employee not found");
        
        var existing = await context.EmployeePayrollProfiles
            .AnyAsync(p => p.EmployeeId == request.EmployeeId && p.EffectiveTo == null);

        if (existing)
            return Error.Validation("EmployeePayrollProfile.AlreadyExists", "Employee already has an active payroll profile.");

        var profile = mapper.Map<EmployeePayrollProfile>(request);
        await context.EmployeePayrollProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        return profile.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<EmployeePayrollProfileDto>>>> GetEmployeePayrollProfiles(int page, int pageSize,
        string searchQuery, Guid? payGroupId = null)
    {
        var query = context.EmployeePayrollProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.BankCode);
        }
        
        if (payGroupId.HasValue)
            query = query.Where(p => p.PayGroupId == payGroupId.Value);

        return await PaginationHelper
            .GetPaginatedResultAsync(query, page, pageSize,
                mapper.Map<EmployeePayrollProfileDto>);
    }

    public async Task<Result<EmployeePayrollProfileDto>> GetEmployeePayrollProfile(Guid id)
    {
        var employeeProfile = await context.EmployeePayrollProfiles.FirstOrDefaultAsync(p => p.Id == id);
        return employeeProfile is null
            ? Error.NotFound("EmployeePayrollProfile.NotFound",
                "Employee payroll profile not found") :
            mapper.Map<EmployeePayrollProfileDto>(employeeProfile);
    }

    public async Task<Result<EmployeePayrollProfileDto>> GetEmployeePayrollProfileByEmployee(Guid employeeId)
    {
        var profile = await context.EmployeePayrollProfiles
            .Where(p => p.EmployeeId == employeeId && p.EffectiveTo == null)
            .FirstOrDefaultAsync();

        return profile is null
            ? Error.NotFound("EmployeePayrollProfile.NotFound", "No active payroll profile found for this employee")
            : Result.Success(mapper.Map<EmployeePayrollProfileDto>(profile));
    }

    public async Task<Result> UpdateEmployeePayrollProfile(Guid id, CreateEmployeePayrollProfileRequest request)
    {
        var profile = await context.EmployeePayrollProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null)
            return Error.NotFound("EmployeePayrollProfile.NotFound", "Employee payroll profile not found");

        mapper.Map(request, profile);
        context.EmployeePayrollProfiles.Update(profile);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteEmployeePayrollProfile(Guid id, Guid userId)
    {
        var profile = await context.EmployeePayrollProfiles.FirstOrDefaultAsync(p => p.Id == id);
        if (profile is null)
            return Error.NotFound("EmployeePayrollProfile.NotFound", "Employee payroll profile not found");

        profile.DeletedAt = DateTime.UtcNow;
        profile.LastDeletedById = userId;
        
        await context.SaveChangesAsync();

        return Result.Success();
    }
}