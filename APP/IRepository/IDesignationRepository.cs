using APP.Utils;
using DOMAIN.Entities.Designations;
using SHARED;

namespace APP.IRepository;

public interface IDesignationRepository
{
    Task<Result<Guid>> CreateDesignation(CreateDesignationRequest request, Guid userId);
    
    Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery);
    
    Task<Result<DesignationDto>> GetDesignation(Guid id);
    
    Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request, Guid userId);
    
    Task<Result> DeleteDesignation(Guid id, Guid userId);
}