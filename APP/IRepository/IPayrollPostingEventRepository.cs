using APP.Utils;
using DOMAIN.Entities.PayrollPostingEvents;
using SHARED;

namespace APP.IRepository;

public interface IPayrollPostingEventRepository
{
    Task<Result<Paginateable<IEnumerable<PayrollPostingEventDto>>>> GetPostingEvents(int page, int pageSize,
        Guid? payrollRunId = null, PayrollPostingEventStatus? status = null);
    Task<Result<PayrollPostingEventDto>> GetPostingEvent(Guid id);
}