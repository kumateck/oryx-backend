using APP.Utils;
using DOMAIN.Entities.PayrollElements;
using SHARED;

namespace APP.IRepository;

public interface IPayrollElementRepository
{
    Task<Result<Guid>> CreatePayrollElement(CreatePayrollElementRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollElementDto>>>> GetPayrollElements(int page, int pageSize,
        Guid? payrollCompanyId = null, PayrollElementType? type = null);
    Task<Result<PayrollElementDto>> GetPayrollElement(Guid id);
    Task<Result> UpdatePayrollElement(Guid id, CreatePayrollElementRequest request);
    Task<Result> DeletePayrollElement(Guid id, Guid userId);
}