using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class AnalyticalTestRequestRepository(ApplicationDbContext context, IMapper mapper) : IAnalyticalTestRequestRepository
{
    public async Task<Result<Guid>> CreateAnalyticalTestRequest(CreateAnalyticalTestRequest request)
    {
       var test = mapper.Map<AnalyticalTestRequest>(request);
       
       await context.AddAsync(test);
       await context.SaveChangesAsync();
       
       return test.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<AnalyticalTestRequestDto>>>> GetAnalyticalTestRequests(int page, int pageSize, string searchQuery, AnalyticalTestStatus? status)
    {
        var query = context.AnalyticalTestRequests
            .AsSplitQuery()
            .Include(s => s.Product)
            .Include(s => s.ProductionSchedule)
            .Include(s => s.ProductionActivityStep)
            .Include(s => s.BatchManufacturingRecord)
            .Include(s => s.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,
                q => q.Filled,
                q => q.SampledQuantity);
        }

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<AnalyticalTestRequestDto>);
    }

    public async Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequest(Guid id)
    {
        var test = await context.AnalyticalTestRequests
            .AsSplitQuery()
            .Include(s => s.Product)
            .Include(s => s.ProductionSchedule)
            .Include(s => s.ProductionActivityStep)
            .Include(s => s.BatchManufacturingRecord)
            .Include(s => s.CreatedBy)
            .Include(s => s.SampledBy)
            .Include(s => s.ReleasedBy)
            .Include(s => s.AcknowledgedBy)
            .FirstOrDefaultAsync(atr => atr.Id == id);
        return test is null ? Error.NotFound("ATR.NotFound", "Analytical test request not found") : mapper.Map<AnalyticalTestRequestDto>(test);
    }

    public async Task<Result> UpdateAnalyticalTestRequest(Guid id, CreateAnalyticalTestRequest request)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }
        
        mapper.Map(request, test);
        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
    
    public async Task<Result> UpdateAnalyticalTestRequest(Guid id, UpdateAnalyticalTestRequest request, Guid userId)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }

        if (request.Status == AnalyticalTestStatus.Acknowledged)
        {
            test.AcknowledgedAt = DateTime.UtcNow;
            test.Status = request.Status;
            test.AcknowledgedById = userId;
        }
        
        else if (request.Status == AnalyticalTestStatus.Sampled)
        {
            test.SampledAt = request.SampledAt;
            test.NumberOfContainers = request.NumberOfContainers;
            test.Status = request.Status;
            test.SampledById = userId;
            test.SampledQuantity = request.SampledQuantity;
        }
        
        else if (request.Status == AnalyticalTestStatus.Released)
        {
            test.ReleaseDate = request.ReleaseDate;
            test.ReleasedById = userId;
        }
        
        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAnalyticalTestRequest(Guid id, Guid userId)
    {
        var test = await context.AnalyticalTestRequests.FirstOrDefaultAsync(atr => atr.Id == id);

        if (test is null)
        {
            return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        }
        
        test.LastDeletedById = userId;
        test.DeletedAt = DateTime.UtcNow;
        
        context.AnalyticalTestRequests.Update(test);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result<AnalyticalTestRequestDto>> GetAnalyticalTestRequestByActivityStep(Guid activityStepId)
    {
        var analyticalTest = await context.AnalyticalTestRequests
            .FirstOrDefaultAsync(atr => atr.ProductionActivityStepId == activityStepId);
        if (analyticalTest is null) return Error.NotFound("ATR.NotFound", "Analytical test request not found");
        return mapper.Map<AnalyticalTestRequestDto>(analyticalTest);
    }
}