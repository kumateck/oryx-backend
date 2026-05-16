using APP.Utils;
using DOMAIN.Entities.PayrollCountryPack;
using SHARED;

namespace APP.IRepository;

public interface IPayrollCountryPackRepository
{
    Task<Result<Guid>> CreatePayrollCountryPack(CreatePayrollCountryPackRequest request);
    Task<Result<Paginateable<IEnumerable<PayrollCountryPackDto>>>> GetPayrollCountryPacks(int page, int pageSize, string searchQuery, Guid? countryId = null);
    Task<Result<PayrollCountryPackDto>> GetPayrollCountryPack(Guid id);
    Task<Result> UpdatePayrollCountryPack(Guid id, CreatePayrollCountryPackRequest request);
    Task<Result> DeletePayrollCountryPack(Guid id, Guid userId);
}