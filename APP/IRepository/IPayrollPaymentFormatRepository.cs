using APP.Utils;
using DOMAIN.Entities.PayrollPaymentFormats;
using SHARED;

namespace APP.IRepository;

public interface IPayrollPaymentFormatRepository
{
    Task<Result<Guid>> CreatePayrollPaymentFormat(CreatePayrollPaymentFormatRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollPaymentFormatDto>>>> GetPayrollPaymentFormats(int page, int pageSize,
        string searchQuery, Guid? countryId = null);
    Task<Result<PayrollPaymentFormatDto>> GetPayrollPaymentFormat(Guid id);
    Task<Result> UpdatePayrollPaymentFormat(Guid id, CreatePayrollPaymentFormatRequest request);
    Task<Result> DeletePayrollPaymentFormat(Guid id, Guid userId);
}