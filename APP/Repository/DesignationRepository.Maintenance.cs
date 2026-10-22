using APP.Services.Transactions;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Designations;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
public partial class DesignationRepository
{
    private async Task<Result<List<Department>>> ValidateDesignation(CreateDesignationRequest request, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.MaximumLeaveDays is < 1 or > 366 ||
            (request.Description?.Length ?? 0) > 1000 || request.DepartmentIds == null || request.DepartmentIds.Count == 0 ||
            request.DepartmentIds.Distinct().Count() != request.DepartmentIds.Count)
            return Error.Validation("Designation.Invalid", "Provide a name, 1–366 whole leave days, description up to 1,000 characters and distinct existing departments.");
        request.Name = request.Name.Trim();
        var departments = await context.Departments.Where(d => request.DepartmentIds.Contains(d.Id)).ToListAsync();
        if (departments.Count != request.DepartmentIds.Count) return Error.Validation("Designation.InvalidDepartments", "One or more departments no longer exist.");
        var name = request.Name.ToUpper();
        if (await context.Designations.AnyAsync(d => d.Id != id && d.Name.ToUpper() == name && d.Departments.Any(dep => request.DepartmentIds.Contains(dep.Id))))
            return Error.Validation("Designation.DepartmentConflict", "This designation name is already assigned to a selected department.");
        if (id.HasValue && await context.Employees.AnyAsync(e => e.DesignationId == id && e.DepartmentId.HasValue && !request.DepartmentIds.Contains(e.DepartmentId.Value)))
            return Error.Validation("Designation.DepartmentInUse", "A current employee uses this designation in a department being removed.");
        if (id.HasValue) {
            var existing = await context.Designations.Include(d => d.LeaveTypes).FirstAsync(d => d.Id == id);
            if (existing.MaximumLeaveDays != request.MaximumLeaveDays && existing.LeaveTypes.Sum(l => l.NumberOfDays) > request.MaximumLeaveDays)
                return Error.Validation("Designation.LeaveLimit", "Existing configured leave days exceed the proposed maximum.");
        }
        return departments;
    }
    public Task<Result<Guid>> CreateDesignation(CreateDesignationRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var selected = await ValidateDesignation(request); if (selected.IsFailure) return Result.Failure<Guid>(selected.Error);
            var designation = mapper.Map<Designation>(request); designation.Departments = selected.Value;
            context.Designations.Add(designation); await context.SaveChangesAsync(); return Result.Success(designation.Id);
        });
    public Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var designation = await context.Designations.Include(d => d.Departments).FirstOrDefaultAsync(d => d.Id == id);
            if (designation == null) return Result.Failure(Error.NotFound("Designation.NotFound", "Designation not found."));
            var selected = await ValidateDesignation(request, id); if (selected.IsFailure) return Result.Failure(selected.Error);
            mapper.Map(request, designation); designation.Departments = selected.Value;
            await context.SaveChangesAsync(); return Result.Success();
        });
    public Task<Result> DeleteDesignation(Guid id, Guid userId) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var designation = await context.Designations.FirstOrDefaultAsync(d => d.Id == id);
            if (designation == null) return Result.Failure(Error.NotFound("Designation.NotFound", "Designation not found."));
            if (await context.Employees.IgnoreQueryFilters().AnyAsync(e => e.DesignationId == id) ||
                await context.StaffRequisitions.IgnoreQueryFilters().AnyAsync(s => s.DesignationId == id) ||
                await context.LeaveTypes.IgnoreQueryFilters().AnyAsync(l => l.Designations.Any(d => d.Id == id)))
                return Result.Failure(Error.Validation("Designation.InUse", "Employee, staff requisition or leave configuration history prevents deletion."));
            designation.DeletedAt = DateTime.UtcNow; designation.LastDeletedById = userId;
            await context.SaveChangesAsync(); return Result.Success();
        });
}
