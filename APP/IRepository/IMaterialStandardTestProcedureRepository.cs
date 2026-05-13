using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using SHARED;

namespace APP.IRepository;

public interface IMaterialStandardTestProcedureRepository
{
    Task<Result<List<MaterialStpMappingDto>>> CreateMaterialStandardTestProcedure(CreateMaterialStandardTestProcedureRequest request);

    Task<Result<Paginateable<IEnumerable<MaterialStandardTestProcedureDto>>>> GetMaterialStandardTestProcedures(int page, int pageSize, string searchQuery, MaterialKind materialKind, bool unused);

    Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedure(Guid id);
    Task<Result<MaterialStandardTestProcedureDto>> GetMaterialStandardTestProcedureByMaterial(Guid id);

    Task<Result<List<MaterialStandardTestProcedureDto>>>
        GetMaterialStandardTestProcedureByStpNumber(string stpNumber);
    Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterialsNotUsedInStandardTestProcedure(
        int page, int pageSize, string searchQuery, MaterialKind kind);
    Task<Result<List<MaterialStpMappingDto>>> UpdateMaterialStandardTestProcedure(Guid id, CreateMaterialStandardTestProcedureRequest request);
    Task<Result<List<MaterialStpMappingDto>>> AddRemoveMaterialsToStp(AddRemoveMaterialToStpRequest request);
    Task<Result> DeleteMaterialStandardTestProcedure(Guid id, Guid userId);
}