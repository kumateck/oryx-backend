using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;
public class ApprovalRepository(ApplicationDbContext context, IMapper mapper) : IApprovalRepository 
{ 
    public async Task<Result<Guid>> CreateApproval(CreateApprovalRequest request, Guid userId) 
    { 
        var approval = mapper.Map<Approval>(request); 
        approval.CreatedById = userId; 
        await context.Approvals.AddAsync(approval); 
        await context.SaveChangesAsync();
        
        return approval.Id;
    }
    
    public async Task<Result<ApprovalDto>> GetApproval(Guid approvalId) 
    { 
        var approval = await context.Approvals
            .Include(a => a.ApprovalStages)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == approvalId);

        return approval is null ? Error.NotFound("Approval.NotFound", "Approval was not found") : mapper.Map<ApprovalDto>(approval);
    }
    
    public async Task<Result<Paginateable<IEnumerable<ApprovalDto>>>> GetApprovals(int page, int pageSize, string searchQuery) 
    { 
        var query = context.Approvals
            .Include(a => a.ApprovalStages)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, a => a.RequisitionType.ToString());
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ApprovalDto>
        );
    }
    public async Task<Result> UpdateApproval(CreateApprovalRequest request, Guid approvalId, Guid userId) 
    { 
        var existingApproval = await context.Approvals.FirstOrDefaultAsync(a => a.Id == approvalId); 
        if (existingApproval is null)
        {
            return Error.NotFound("Approval.NotFound", "Approval was not found");
        }

        mapper.Map(request, existingApproval);
        existingApproval.LastUpdatedById = userId;

        context.Approvals.Update(existingApproval);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    public async Task<Result> DeleteApproval(Guid approvalId, Guid userId) 
    { 
        var approval = await context.Approvals.FirstOrDefaultAsync(a => a.Id == approvalId); 
        if (approval is null)
        {
            return Error.NotFound("Approval.NotFound", "Approval was not found");
        }

        approval.DeletedAt = DateTime.UtcNow;
        approval.LastDeletedById = userId;
        context.Approvals.Update(approval);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
