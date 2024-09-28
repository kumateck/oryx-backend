using APP.Utils;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Routes;
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
    Task<Result> CreateRoute(List<CreateRouteRequest> request, Guid productId, Guid userId);
    Task<Result<RouteDto>> GetRoute(Guid routeId);
    Task<Result<Paginateable<IEnumerable<RouteDto>>>> GetRoutes(int page, int pageSize,
        string searchQuery = null);
    Task<Result> DeleteRoute(Guid routeId, Guid userId);
    Task<Result<Guid>> CreateProductPackage(List<CreateProductPackageRequest> request, Guid productId, Guid userId);
    Task<Result<ProductPackageDto>> GetProductPackage(Guid productPackageId);
    Task<Result<Paginateable<IEnumerable<ProductPackageDto>>>> GetProductPackages(int page, int pageSize, string searchQuery = null);
    Task<Result> UpdateProductPackage(CreateProductPackageRequest request, Guid productPackageId, Guid userId);
    Task<Result> DeleteProductPackage(Guid productPackageId, Guid userId);
}