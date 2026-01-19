using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ServiceMemoRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager,
    IJobRequestRepository jobRequestRepository) : IServiceMemoRepository
{
    public async Task<Result<Guid>> CreateServiceMemo(CreateServiceMemoRequest request)
    {
        var jobOrder = await context.JobOrders
            .Include(j => j.ServiceProformaInvoice)
            .FirstOrDefaultAsync(j => j.Id == request.JobOrderId);
        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        // Check if proforma invoice has been received and approved
        if (jobOrder.ServiceProformaInvoice == null)
            return Error.Validation("ProformaInvoice.NotRequested", 
                "Proforma invoice must be requested before creating service memo");

        if (jobOrder.ServiceProformaInvoice.Status != ServiceProformaInvoiceStatus.Approved)
            return Error.Validation("ProformaInvoice.NotApproved", 
                "Proforma invoice must be approved before creating service memo");

        var quotation = await context.ServiceQuotations
            .AsSplitQuery()
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == request.ServiceQuotationId);

        if (quotation is null)
            return Error.NotFound("ServiceQuotation.NotFound", "Service quotation not found");

        if (!quotation.IsSelected)
            return Error.Validation("Quotation.NotSelected", "This quotation has not been selected");

        var serviceProvider = await context.ServiceProviders.AnyAsync(sp => sp.Id == request.ServiceProviderId);
        if (!serviceProvider)
            return Error.Validation("ServiceProvider.Invalid", "Invalid service provider");

        var issuer = await userManager.FindByIdAsync(request.IssuedById.ToString());
        if (issuer is null) return Error.Validation("User.Invalid", "User Invalid");

        // Generate memo number
        var memoNumber = await GenerateServiceMemoNumber();

        var memo = mapper.Map<ServiceMemo>(request);
        memo.MemoNumber = memoNumber;

        await context.ServiceMemos.AddAsync(memo);

        // Update job order
        jobOrder.ServiceMemoId = memo.Id;
        jobOrder.Status = JobOrderStatus.MemoCreated;
        context.JobOrders.Update(jobOrder);

        await context.SaveChangesAsync();

        return memo.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ServiceMemoDto>>>> GetServiceMemos(int page, int pageSize,
        ServiceMemoStatus? status = null, Guid? jobOrderId = null, Guid? serviceProviderId = null)
    {
        var query = context.ServiceMemos
            .AsSplitQuery()
            .Include(m => m.JobOrder)
            .Include(m => m.ServiceProvider)
            .Include(m => m.IssuedBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        if (jobOrderId.HasValue)
        {
            query = query.Where(m => m.JobOrderId == jobOrderId.Value);
        }

        if (serviceProviderId.HasValue)
        {
            query = query.Where(m => m.ServiceProviderId == serviceProviderId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<ServiceMemoDto>);
    }

    public async Task<Result<ServiceMemoDto>> GetServiceMemo(Guid id)
    {
        var memo = await context.ServiceMemos
            .AsSplitQuery()
            .Include(m => m.JobOrder)
                .ThenInclude(j => j.JobRequest)
            .Include(m => m.ServiceQuotation)
                .ThenInclude(q => q.Items)
            .Include(m => m.ServiceProvider)
            .Include(m => m.IssuedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (memo is null)
            return Error.NotFound("ServiceMemo.NotFound", "Service memo not found");

        return mapper.Map<ServiceMemoDto>(memo);
    }

    public async Task<Result> UpdateServiceMemo(Guid id, UpdateServiceMemoRequest request)
    {
        var memo = await context.ServiceMemos.FirstOrDefaultAsync(m => m.Id == id);
        if (memo is null)
            return Error.NotFound("ServiceMemo.NotFound", "Service memo not found");

        if (memo.Status != ServiceMemoStatus.Draft)
            return Error.Validation("ServiceMemo.InvalidStatus", "Only draft memos can be updated");

        mapper.Map(request, memo);
        context.ServiceMemos.Update(memo);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result> MarkServiceMemoAsPaid(Guid id)
    {
        var memo = await context.ServiceMemos.FirstOrDefaultAsync(m => m.Id == id);
        if (memo is null)
            return Error.NotFound("ServiceMemo.NotFound", "Service memo not found");
        
        memo.Paid = true;
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> IssueServiceMemo(IssueServiceMemoRequest request)
    {
        var memo = await context.ServiceMemos
            .AsSplitQuery()
            .Include(m => m.JobOrder)
            .FirstOrDefaultAsync(m => m.Id == request.ServiceMemoId);

        if (memo is null)
            return Error.NotFound("ServiceMemo.NotFound", "Service memo not found");

        if (memo.Status != ServiceMemoStatus.Draft)
            return Error.Validation("ServiceMemo.InvalidStatus", "Only draft memos can be issued");

        memo.Status = ServiceMemoStatus.Issued;
        context.ServiceMemos.Update(memo);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(memo.JobOrder.JobRequestId, JobRequestStatus.JobStarted);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<string> GenerateServiceMemoNumber()
    {
        var count = await context.ServiceMemos.CountAsync();
        return $"SM-{DateTime.UtcNow:yyyyMM}-{(count + 1):D4}";
    }
}

