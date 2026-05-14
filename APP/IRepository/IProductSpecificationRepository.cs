using APP.Utils;
using DOMAIN.Entities.ProductSpecifications;
using SHARED;

namespace APP.IRepository;

public interface IProductSpecificationRepository
{
    Task<Result<List<ProductSpecificationMappingDto>>> CreateProductSpecification(CreateProductSpecificationRequest request);
    Task<Result<Paginateable<IEnumerable<ProductSpecificationDto>>>> GetProductSpecifications(int page, int pageSize, string searchQuery, bool? isVerified = null);
    Task<Result<ProductSpecificationDto>> GetProductSpecification(Guid id);

    Task<Result<List<ProductSpecificationDto>>> GetProductSpecificationByProductId(Guid productId);
    Task<Result<List<ProductSpecificationDto>>> GetProductSpecificationBySpecificationNumber(string specificationNumber);
    Task<Result<List<ProductSpecificationMappingDto>>> UpdateProductSpecification(Guid id, UpdateProductSpecificationRequest request);
    Task<Result<List<ProductSpecificationMappingDto>>> AddRemoveProductsToSpecification(AddRemoveProductToSpecificationRequest request);
    Task<Result> DeleteProductSpecification(Guid id, Guid userId);
}