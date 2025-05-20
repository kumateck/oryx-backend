using APP.Utils;
using DOMAIN.Entities.StandardTestProcedures;
using SHARED;

namespace APP.IRepository;

public interface IStandardTestProcedureRepository
{
    Task<Result<Guid>> CreateStandardTestProcedure(CreateStandardTestProcedureRequest request);
    
    Task<Result<Paginateable<IEnumerable<StandardTestProcedureDto>>>> GetStandardTestProcedures(int page, int pageSize, string searchQuery);
    
    Task<Result<StandardTestProcedureDto>> GetStandardTestProcedure(Guid id);
    
    
    Task<Result> UpdateStandardTestProcedure(Guid id, CreateStandardTestProcedureRequest request);
    
    Task<Result> DeleteStandardTestProcedure(Guid id, Guid userId);
    
}