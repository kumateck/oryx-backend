using APP.Utils;
using DOMAIN.Entities.ProductStandardTestProcedures;
using SHARED;

namespace APP.IRepository;

public interface IProductStandardTestProcedureRepository
{
    Task<Result<Guid>> CreateProductStandardTestProcedure(CreateProductStandardTestProcedureRequest request);
    
    Task<Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>> GetProductStandardTestProcedures(int page, int pageSize, string searchQuery);
    
    Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedure(Guid id);
    
    
    Task<Result> UpdateProductStandardTestProcedure(Guid id, CreateProductStandardTestProcedureRequest request);
    
    Task<Result> DeleteProductStandardTestProcedure(Guid id, Guid userId);
}