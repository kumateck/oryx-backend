using APP.Utils;
using DOMAIN.Entities.Materials;
using SHARED;

namespace APP.IRepository;

public interface IMaterialRepository
{
    Task<Result<Guid>> CreateMaterial(CreateMaterialRequest request, Guid userId);
    Task<Result<MaterialDto>> GetMaterial(Guid materialId);
    Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery);
    Task<Result> UpdateMaterial(CreateMaterialRequest request, Guid materialId, Guid userId);
    Task<Result> DeleteMaterial(Guid materialId, Guid userId);
}