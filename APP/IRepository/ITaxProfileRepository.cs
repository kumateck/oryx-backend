using APP.Utils;
using DOMAIN.Entities.TaxProfiles;
using SHARED;

namespace APP.IRepository;

public interface ITaxProfileRepository
{
    Task<Result<Guid>> CreateTaxProfile(CreateTaxProfileRequest request);
    Task<Result<Paginateable<IEnumerable<TaxProfileDto>>>> GetTaxProfiles(int page, int pageSize,
        string searchQuery, Guid? countryId = null);
    Task<Result<TaxProfileDto>> GetTaxProfile(Guid id);
    Task<Result> UpdateTaxProfile(Guid id, CreateTaxProfileRequest request);
    Task<Result> DeleteTaxProfile(Guid id, Guid userId);
}
