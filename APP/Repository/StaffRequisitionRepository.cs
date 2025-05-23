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
    public async Task<Result<Guid>> CreateStaffRequisition(CreateStaffRequisitionRequest request)
    {
        var staffRequisition = mapper.Map<StaffRequisition>(request);
        await context.StaffRequisitions.AddAsync(staffRequisition);
        
        await context.SaveChangesAsync();
        return staffRequisition.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<StaffRequisitionDto>>>> GetStaffRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context.StaffRequisitions
            .Where(sr => sr.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, sr => sr.EducationalBackground);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<StaffRequisitionDto>);
    }

    public async Task<Result<StaffRequisitionDto>> GetStaffRequisition(Guid id)
    {
        var requisition = await context.StaffRequisitions.FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);
        
        return requisition is null ? Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found")
            : mapper.Map<StaffRequisitionDto>(requisition);
    }

    public async Task<Result> UpdateStaffRequisition(Guid id, CreateStaffRequisitionRequest request)
    {
        
        //TODO: Update staff requisition with approvals
        var requisition = await context.StaffRequisitions.FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);

        if (requisition is null)
        {
            return Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found");
        }
        
        return Result.Success();
    }

    public async Task<Result> DeleteStaffRequisitionRequest(Guid id, Guid userId)
    {
        //TODO: approval condition for deletion
        var requisition = await context.StaffRequisitions.FirstOrDefaultAsync(sr => sr.Id == id && sr.LastDeletedById == null);

        if (requisition is null)
        {
            return Error.NotFound("StaffRequisition.NotFound", "Staff requisition not found");
        }
        
        requisition.DeletedAt = DateTime.UtcNow;
        requisition.LastDeletedById = userId;
        
        context.StaffRequisitions.Update(requisition);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
}