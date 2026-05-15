using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IMaterialSpecificationRepository
{
    Task<Result<List<MaterialSpecificationMappingDto>>> CreateMaterialSpecification(
        CreateMaterialSpecificationRequest request
    );
    Task<Result<Paginateable<IEnumerable<MaterialSpecificationDto>>>> GetMaterialSpecifications(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind materialKind,
        bool? isVerified = null
    );
    Task<Result<MaterialSpecificationDto>> GetMaterialSpecification(Guid id);
    Task<Result<List<MaterialDto>>> GetMaterialsNotLinkedToSpecification(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? materialKind
    );
    Task<Result<MaterialSpecificationDto>> GetMaterialSpecificationByMaterial(Guid materialId);
    Task<Result<List<MaterialSpecificationDto>>> GetMaterialSpecificationBySpecificationNumber(
        string specificationNumber
    );
    Task<Result<List<MaterialSpecificationMappingDto>>> UpdateMaterialSpecification(
        Guid id,
        UpdateMaterialSpecificationRequest request
    );
    Task<Result<List<MaterialSpecificationMappingDto>>> AddRemoveMaterialsToSpecification(
        AddRemoveMaterialToSpecificationRequest request
    );
    Task<Result> DeleteMaterialSpecification(Guid id, Guid userId);
    Task<Result> ImportMaterialSpecificationsFromCsv(
        IFormFile file,
        MaterialKind kind,
        Guid userId
    );
}
