using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.StaffRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
 
public class StaffRequisitionRepository(ApplicationDbContext context, IMapper mapper) : IStaffRequisitionRepository
{
    public async Task<Result<Guid>> CreateStaffRequisition(CreateStaffRequisitionRequest request, Guid userId)
    {
        var designation = await context.Designations.FindAsync(request.DesignationId);
        
        if (designation == null)
        {
            return Error.NotFound("Invalid.Designation","Invalid Designation");
        }
        
        var department = await context.Departments.FindAsync(request.DepartmentId);
        if (department == null)
        {
            return Error.NotFound("Invalid.Department","Invalid Department");
        }
        
        var staffRequisition = mapper.Map<StaffRequisition>(request);
        
        await context.StaffRequisitions.AddAsync(staffRequisition);
        
        await context.SaveChangesAsync();
        return staffRequisition.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<StaffRequisitionDto>>>> GetStaffRequisitions(int page, int pageSize, string searchQuery,
        DateTime? startDate, DateTime? endDate)
    {
        var query = context.StaffRequisitions
            .Include(sr => sr.Designation)
            .Where(sr => sr.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, sr => sr.EducationalQualification);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, sr => sr.Designation.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var hasBudgetMatch = Enum.TryParse<BudgetStatus>(searchQuery, true, out var parsedBudgetStatus);
            var hasAppointmentMatch = Enum.TryParse<AppointmentType>(searchQuery, true, out var parsedAppointmentType);

            if (hasBudgetMatch)
            {
                query = query.Where(sr => sr.BudgetStatus == parsedBudgetStatus);
            }

            if (hasAppointmentMatch)
            {
                query = query.Where(sr => sr.AppointmentType == parsedAppointmentType);
            }
        }

        if (startDate.HasValue)
        {
            query = query.Where(sr => sr.RequestUrgency >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(sr => sr.RequestUrgency <= endDate.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<StaffRequisitionDto>);
    }

    public async Task<Result<StaffRequisitionDto>> GetStaffRequisition(Guid id)
    {
        var requisition = await context.StaffRequisitions
            .Include(sr => sr.Designation)
            .FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);
        
        return requisition is null ? Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found")
            : mapper.Map<StaffRequisitionDto>(requisition);
    }

    public async Task<Result> UpdateStaffRequisition(Guid id, CreateStaffRequisitionRequest request)
    {
        
        var requisition = await context.StaffRequisitions.FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);

        if (requisition is null)
        {
            return Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found");
        }

        if (requisition.Approved)
        {
            return Error.Validation("StaffRequisition.NotEditable",
                "Cannot modify a staff requisition approved that is approved");
        }
        
        var department = await context.Departments.FindAsync(request.DepartmentId);
        if (department is null)
        {
            return Error.NotFound("Department.Invalid", "Invalid Department");
        }
        
        mapper.Map(request, requisition);
        context.StaffRequisitions.Update(requisition);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeleteStaffRequisitionRequest(Guid id, Guid userId)
    {
        //TODO: approval condition for deletion
        var requisition = await context.StaffRequisitions
            .FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);

        if (requisition is null)
        {
            return Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found");
        }

        if (requisition.Approved)
        {
            return Error.Validation("StaffRequisition.CannotDelete", "Cannot delete a staff requisition that is approved");
        }
        
        requisition.DeletedAt = DateTime.UtcNow;
        requisition.LastDeletedById = userId;
        
        context.StaffRequisitions.Update(requisition);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}