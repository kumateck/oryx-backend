using APP.Utils;
using DOMAIN.Entities.PayrollCalendars;
using SHARED;

namespace APP.IRepository;

public interface IPayrollCalenderRepository
{
    Task<Result<Guid>> CreatePayrollCalendar(CreatePayrollCalendarRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollCalendarDto>>>> GetPayrollCalendars(int page, int pageSize, string searchQuery, Guid? payrollCompanyId = null);
    Task<Result<PayrollCalendarDto>> GetPayrollCalendar(Guid id);
    Task<Result> UpdatePayrollCalendar(Guid id, CreatePayrollCalendarRequest request);
    Task<Result> DeletePayrollCalendar(Guid id, Guid userId);
}