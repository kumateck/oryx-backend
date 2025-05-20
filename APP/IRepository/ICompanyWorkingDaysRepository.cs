using APP.Utils;
using DOMAIN.Entities.CompanyWorkingDays;
using SHARED;

namespace APP.IRepository;

public interface ICompanyWorkingDaysRepository
{
    Task<Result> CreateCompanyWorkingDays(List<CompanyWorkingDaysRequest> companyWorkingDays);
    Task<Result<Paginateable<IEnumerable<CompanyWorkingDaysDto>>>> GetCompanyWorkingDays(int page, int pageSize, string searchQuery);

}