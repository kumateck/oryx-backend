using APP.Utils;
using DOMAIN.Entities.StatutoryProfiles;
using SHARED;

namespace APP.IRepository;

public interface IStatutoryProfileRepository
{
    Task<Result<Guid>> CreateStatutoryProfile(CreateStatutoryProfileRequest request);
    Task<Result<Paginateable<IEnumerable<StatutoryProfileDto>>>> GetStatutoryProfiles(int page, int pageSize, string searchQuery,
        Guid? countryId = null);
    Task<Result<StatutoryProfileDto>> GetStatutoryProfile(Guid id);
    Task<Result> UpdateStatutoryProfile(Guid id, CreateStatutoryProfileRequest request);
    Task<Result> DeleteStatutoryProfile(Guid id, Guid userId);
}