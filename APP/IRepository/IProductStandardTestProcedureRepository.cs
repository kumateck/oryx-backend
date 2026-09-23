using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductStandardTestProcedures;
using DOMAIN.Entities.QualityRoutines;
using SHARED;

namespace APP.IRepository;

public interface IProductStandardTestProcedureRepository
{
    Task<Result<List<ProductStpMappingDto>>> CreateProductStandardTestProcedure(
        CreateProductStandardTestProcedureRequest request
    );
    Task<
        Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>
    > GetProductStandardTestProcedures(int page, int pageSize, string searchQuery);

    Task<
        Result<List<ProductStandardTestProcedureDto>>
    > GetProductStandardTestProcedureByStpNumber(string stpNumber);
    Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedure(Guid id);
    Task<Result<ProductStandardTestProcedureDto>> GetProductStandardTestProcedureByProduct(Guid id);
    Task<
        Result<Paginateable<IEnumerable<ProductListDto>>>
    > GetProductsNotUsedInStandardTestProcedure(int page, int pageSize, string searchQuery);
    Task<Result<List<ProductStpMappingDto>>> UpdateProductStandardTestProcedure(
        Guid id,
        UpdateProductStandardTestProcedureRequest request
    );
    Task<Result<List<ProductStpMappingDto>>> AddRemoveProductsToStp(AddRemoveProductToStpRequest request);
    Task<
        Result<Paginateable<IEnumerable<ProductStandardTestProcedureDto>>>
    > GetProductStandardTestProceduresNotLinkedToArd(
        int page, int pageSize, string searchQuery, TestStage stage, AnalysisType analysisType
    );

    Task<Result> DeleteProductStandardTestProcedure(Guid id, Guid userId);
}
