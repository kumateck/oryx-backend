using APP.Utils;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using SHARED;

namespace APP.IRepository;

public interface IProductStandardTestProcedureRepository
{
    Task<Result<Guid>> CreateProductStandardTestProcedure(CreateProductStandardTestProcedureRequest request);
    Task<Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>> GetProductStandardTestProcedures(int page, int pageSize, string searchQuery);
    Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedure(Guid id);
    Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedureByProduct(Guid id);
    Task<Result<Paginateable<IEnumerable<ProductListDto>>>> GetProductsNotUsedInStandardTestProcedure(
        int page, int pageSize, string searchQuery);
    Task<Result> UpdateProductStandardTestProcedure(Guid id, CreateProductStandardTestProcedureRequest request);
    Task<Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>> GetProductStandardTestProceduresNotLinkedToArd(
        int page, int pageSize, string searchQuery);

    Task<Result> DeleteProductStandardTestProcedure(Guid id, Guid userId);
}