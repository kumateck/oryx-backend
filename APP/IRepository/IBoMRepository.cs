using APP.Utils;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Products;
using SHARED;

namespace APP.IRepository;

public interface IBoMRepository
{
    Task<Result<Guid>> CreateBillOfMaterial(CreateBillOfMaterialRequest request, Guid userId);
    Task<Result<BillOfMaterialDto>> GetBillOfMaterial(Guid billOfMaterialId);
    Task<Result<Paginateable<IEnumerable<BillOfMaterialDto>>>> GetBillOfMaterials(int page, int pageSize,
        string searchQuery);
    Task<Result> UpdateBillOfMaterial(CreateProductBillOfMaterialRequest request, Guid billOfMaterialId, Guid userId);
    Task<Result> DeleteBillOfMaterial(Guid billOfMaterialId, Guid userId);
}