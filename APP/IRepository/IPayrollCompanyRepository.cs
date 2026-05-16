using APP.Utils;
using DOMAIN.Entities.PayrollCompanies;
using SHARED;

namespace APP.IRepository;

public interface IPayrollCompanyRepository
{
    Task<Result<Guid>> CreatePayrollCompany(CreatePayrollCompanyRequest request);

    Task<Result<Paginateable<IEnumerable<PayrollCompanyDto>>>> GetPayrollCompanies(int page, int pageSize, string searchQuery);

    Task<Result<PayrollCompanyDto>> GetPayrollCompany(Guid payrollCompanyId);

    Task<Result> UpdatePayrollCompany(Guid payrollCompanyId, CreatePayrollCompanyRequest request);

    Task<Result> DeletePayrollCompany(Guid payrollCompanyId, Guid id);
}