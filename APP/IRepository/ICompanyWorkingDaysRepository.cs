using APP.Utils;
using DOMAIN.Entities.CompanyWorkingDays;
using SHARED;

namespace APP.IRepository;

public interface ICompanyWorkingDaysRepository
{
    Task<Result> CreateCompanyWorkingDays(List<CompanyWorkingDaysRequest> companyWorkingDays, Guid userId);
    Task<Result<Paginateable<IEnumerable<CompanyWorkingDaysDto>>>> GetCompanyWorkingDays(int page, int pageSize, string searchQuery);
    Task<Result<CompanyWorkingDaysDto>> GetCompanyWorkingDay(Guid id);
    Task<Result> UpdateCompanyWorkingDays(Guid id, CompanyWorkingDaysRequest companyWorkingDaysDto, Guid userId);
}