using APP.Utils;
using DOMAIN.Entities.Products;
using SHARED;

namespace APP.IRepository;

public interface IProductRepository
{
    Task<Result<Guid>> CreateProduct(CreateProductRequest request, Guid userId);
    Task<Result<ProductDto>> GetProduct(Guid productId);
    Task<Result<Paginateable<IEnumerable<ProductDto>>>> GetProducts(int page, int pageSize,
        string searchQuery);
    Task<Result> UpdateProduct(UpdateProductRequest request, Guid productId, Guid userId);
    Task<Result> DeleteProduct(Guid productId, Guid userId);
}