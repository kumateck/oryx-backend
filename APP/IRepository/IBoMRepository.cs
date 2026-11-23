using APP.Utils;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Products;
using SHARED;
using APP.Services.Authorization;

namespace APP.IRepository;

public interface IBoMRepository
{
    Task<Result<Guid>> CreateBillOfMaterial(CreateBillOfMaterialRequest request, Guid userId, ProductContext productContext = ProductContext.Production,
        Guid? technologyTransferId = null);
    Task<Result<BillOfMaterialDto>> GetBillOfMaterial(Guid billOfMaterialId);
    Task<Result<Paginateable<IEnumerable<BillOfMaterialDto>>>> GetBillOfMaterials(int page, int pageSize,
        string searchQuery);
    Task<Result> UpdateBillOfMaterial(CreateProductBillOfMaterialRequest request, Guid billOfMaterialId, Guid userId);
    Task<Result> ArchiveBillOfMaterial(Guid billOfMaterialId, Guid userId);
    Task<Result> DeleteBillOfMaterial(Guid billOfMaterialId, Guid userId);
}
