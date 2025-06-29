using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using SHARED;

namespace APP.IRepository;

public interface IMaterialStandardTestProcedureRepository
{
    Task<Result<Guid>> CreateMaterialStandardTestProcedure(CreateMaterialStandardTestProcedureRequest request);
    
    Task<Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>> GetMaterialStandardTestProcedures(int page, int pageSize, string searchQuery, MaterialKind materialKind, bool unused);
    
    Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedure(Guid id);
    
    
    Task<Result> UpdateMaterialStandardTestProcedure(Guid id, CreateMaterialStandardTestProcedureRequest request);
    
    Task<Result> DeleteMaterialStandardTestProcedure(Guid id, Guid userId);
    
}