using APP.Services.Transactions;
using DOMAIN.Entities.Designations;
using DOMAIN.Entities.LeaveTypes;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
public partial class LeaveTypeRepository
{
    private async Task<Result<List<Designation>>> ValidateLeaveType(CreateLeaveTypeRequest request, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200 || request.NumberOfDays is < 1 or > 366 ||
            request.DeductionLimit < 0 || request.DesignationList == null || request.DesignationList.Count == 0 ||
            request.DesignationList.Distinct().Count() != request.DesignationList.Count)
            return Error.Validation("LeaveType.Invalid", "Provide a name, 1–366 whole days, non-negative deduction limit and distinct existing designations.");
        request.Name = request.Name.Trim(); var name = request.Name.ToUpper();
        if (await context.LeaveTypes.AnyAsync(l => l.Id != id && l.Name.ToUpper() == name))
            return Error.Validation("LeaveType.Exists", "A leave type already uses this name.");
        var designations = await context.Designations.Include(d => d.LeaveTypes).Where(d => request.DesignationList.Contains(d.Id)).ToListAsync();
        if (designations.Count != request.DesignationList.Count) return Error.Validation("LeaveType.InvalidDesignations", "One or more designations no longer exist.");
        foreach (var designation in designations) {
            var configured = designation.LeaveTypes.Where(l => l.Id != id).Sum(l => l.NumberOfDays);
            if (configured + request.NumberOfDays > designation.MaximumLeaveDays)
                return Error.Validation("LeaveType.CumulativeDaysExceeded", $"Configured leave days for '{designation.Name}' would exceed {designation.MaximumLeaveDays} days.");
        }
        return designations;
    }
    public Task<Result<Guid>> CreateLeaveType(CreateLeaveTypeRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var selected = await ValidateLeaveType(request); if (selected.IsFailure) return Result.Failure<Guid>(selected.Error);
            var leave = mapper.Map<LeaveType>(request); leave.Designations = selected.Value;
            context.LeaveTypes.Add(leave); await context.SaveChangesAsync(); return Result.Success(leave.Id);
        });
    public Task<Result> UpdateLeaveType(Guid id, CreateLeaveTypeRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var leave = await context.LeaveTypes.Include(l => l.Designations).FirstOrDefaultAsync(l => l.Id == id);
            if (leave == null) return Result.Failure(Error.NotFound("LeaveType.NotFound", "Leave type not found."));
            var selected = await ValidateLeaveType(request, id); if (selected.IsFailure) return Result.Failure(selected.Error);
            mapper.Map(request, leave); leave.Designations = selected.Value;
            await context.SaveChangesAsync(); return Result.Success();
        });
    public Task<Result> DeleteLeaveType(Guid id, Guid userId) =>
        ReferenceMaintenanceTransaction.Run(context, async () => {
            var leave = await context.LeaveTypes.FirstOrDefaultAsync(l => l.Id == id);
            if (leave == null) return Result.Failure(Error.NotFound("LeaveType.NotFound", "Leave type not found."));
            if (await context.LeaveRequests.IgnoreQueryFilters().AnyAsync(l => l.LeaveTypeId == id))
                return Result.Failure(Error.Validation("LeaveType.InUse", "Existing or historical leave requests prevent deletion."));
            leave.DeletedAt = DateTime.UtcNow; leave.LastDeletedById = userId;
            await context.SaveChangesAsync(); return Result.Success();
        });
}
