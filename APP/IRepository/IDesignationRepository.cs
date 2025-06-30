using APP.Utils;
using DOMAIN.Entities.Designations;
using SHARED;

namespace APP.IRepository;

public interface IDesignationRepository
{
    Task<Result<Guid>> CreateDesignation(CreateDesignationRequest request);
    
    Task<Result<Paginateable<IEnumerable<DesignationDto>>>> GetDesignations(int page, int pageSize, string searchQuery);
    
    Task<Result<DesignationDto>> GetDesignation(Guid id);
    
    Task<Result<List<DesignationDto>>> GetDesignationByDepartment(Guid departmentId);
    
    Task<Result> UpdateDesignation(Guid id, CreateDesignationRequest request);
    
    Task<Result> DeleteDesignation(Guid id, Guid userId);
}