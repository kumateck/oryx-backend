using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollPostingEvents;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollPostingEventRepository(ApplicationDbContext context, IMapper mapper) : IPayrollPostingEventRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayrollPostingEventDto>>>> GetPostingEvents(int page, int pageSize, 
        string searchQuery, Guid? payrollRunId = null, PayrollPostingEventStatus? status = null)
    {
        var query = context.PayrollPostingEvents.AsQueryable();
        if (payrollRunId.HasValue) 
        {
            query = query.Where(p => p.PayrollRunId == payrollRunId);
        }

        if (Enum.TryParse<PayrollPostingEventStatus>(searchQuery, true, out var statusEnum))
        {
            query = query.Where(p => p.Status == statusEnum);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollPostingEventDto>);
    }

    public async Task<Result<PayrollPostingEventDto>> GetPostingEvent(Guid id)
    {
        var postingEvent = await context.PayrollPostingEvents.FirstOrDefaultAsync(e => e.Id == id);
        return postingEvent is null ? Error.NotFound("PayrollPostingEvent.NotFound", "Posting event not found")
            : mapper.Map<PayrollPostingEventDto>(postingEvent);
    }
}