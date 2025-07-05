using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using SHARED;

namespace APP.IRepository;

public interface IMaterialSpecificationRepository
{
    Task<Result<Guid>> CreateMaterialSpecification(CreateMaterialSpecificationRequest request);
    Task<Result<Paginateable<IEnumerable<MaterialSpecificationDto>>>> GetMaterialSpecifications(int page, int pageSize, string searchQuery, MaterialKind materialKind);
    Task<Result<MaterialSpecificationDto>> GetMaterialSpecification(Guid id);
    Task<Result> UpdateMaterialSpecification(Guid id, CreateMaterialSpecificationRequest request);
    Task<Result> DeleteMaterialSpecification(Guid id, Guid userId);
}