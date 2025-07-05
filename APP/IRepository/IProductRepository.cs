using APP.Utils;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Routes;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IProductRepository
{
    Task<Result<Guid>> CreateProduct(CreateProductRequest request, Guid userId);
    Task<Result<ProductDto>> GetProduct(Guid productId);
    Task<Result<Paginateable<IEnumerable<ProductListDto>>>> GetProducts(int page, int pageSize,
        string searchQuery);
    Task<Result> UpdateProduct(UpdateProductRequest request, Guid productId, Guid userId);
    Task<Result> DeleteProduct(Guid productId, Guid userId);
    Task<Result<ProductBillOfMaterialDto>> GetBillOfMaterialByProductId(Guid productId);
    Task<Result> CreateRoute(List<CreateRouteRequest> request, Guid productId, Guid userId);
    Task<Result<RouteDto>> GetRoute(Guid routeId);
    Task<Result<IEnumerable<RouteDto>>> GetRoutes(Guid productId);
    Task<Result> DeleteRoute(Guid routeId, Guid userId);
    Task<Result<Guid>> CreateProductPackage(List<CreateProductPackageRequest> request, Guid productId, Guid userId);
    Task<Result<ProductPackageDto>> GetProductPackage(Guid productPackageId);
    Task<Result<IEnumerable<ProductPackageDto>>> GetProductPackages(Guid productId);
    Task<Result> UpdateProductPackage(CreateProductPackageRequest request, Guid productPackageId, Guid userId);
    Task<Result> DeleteProductPackage(Guid productPackageId, Guid userId);
    Task<Result<Guid>> CreateFinishedProduct(List<CreateFinishedProductRequest> request, Guid productId,
        Guid userId);
     Task<Result> ArchiveBillOfMaterial(Guid productId, Guid userId);
     Task<Result> UpdateProductPackageDescription(UpdateProductPackageDescriptionRequest request,
         Guid productId, Guid userId);
     
    Task<Result<Guid>> CreateEquipment(CreateEquipmentRequest request, Guid userId);
    Task<Result<EquipmentDto>> GetEquipment(Guid equipmentId);
    Task<Result<Paginateable<IEnumerable<EquipmentDto>>>> GetEquipments(int page, int pageSize, string searchQuery);
    Task<Result<List<EquipmentDto>>> GetEquipments();
    Task<Result> UpdateEquipment(CreateEquipmentRequest request, Guid equipmentId, Guid userId);
    Task<Result> DeleteEquipment(Guid equipmentId, Guid userId);
    Task<Result> ImportProductsFromExcel(IFormFile file);
    Task<Result> ImportProductBomFromExcel(IFormFile file);
    Task<Result> ImportProductPackagesFromExcel(IFormFile file);
}