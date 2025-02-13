using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Http;
using SHARED;

namespace APP.IRepository;

public interface IMaterialRepository
{
    Task<Result<Guid>> CreateMaterial(CreateMaterialRequest request, Guid userId);
    Task<Result<MaterialDto>> GetMaterial(Guid materialId);
    Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery, MaterialKind kind);
    Task<Result<List<MaterialCategoryDto>>> GetMaterialCategories(MaterialKind? materialKind);
    Task<Result<List<MaterialDto>>> GetMaterials();
    Task<Result> UpdateMaterial(CreateMaterialRequest request, Guid materialId, Guid userId);
    Task<Result> DeleteMaterial(Guid materialId, Guid userId);
    Task<Result<decimal>> CheckStockLevel(Guid materialId);
    //Task<Result<bool>> CanFulfillRequisition(Guid materialId, Guid requisitionId);
    Task<Result> CreateMaterialBatch(List<CreateMaterialBatchRequest> request, Guid userId);
    Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId);
    Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(int page, int pageSize,
        string searchQuery);
    Task<Result<List<MaterialBatchDto>>> GetMaterialBatchesByMaterialId(Guid materialId);
    Task<Result<decimal>> GetMaterialsInTransit(Guid materialId);
    Task<Result> MoveMaterialBatchByMaterial(MoveMaterialBatchRequest request, Guid userId);
    Task<Result> MoveMaterialBatch(Guid batchId, Guid fromLocationId, Guid toLocationId, decimal quantity,
        Guid userId);
    Task<Result<decimal>> GetMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<decimal>> GetFrozenMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<List<MaterialBatchDto>>> GetFrozenMaterialBatchesInWarehouse(Guid materialId,
        Guid warehouseId);
    Task<Result> FreezeMaterialBatchAsync(Guid batchId);
    Task<Result> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, decimal quantity, Guid userId);
    Task<Result> ConsumeMaterialAtLocation(Material material, Guid locationId, decimal quantity,
        Guid userId);
    Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(Guid materialId); 
    Task<Result> ImportMaterialsFromExcel(IFormFile file, MaterialKind kind); 
    Task<Result> ImportMaterialsFromExcel(string filePath, MaterialKind kind);
    Result<List<BatchLocation>> BatchesNeededToBeConsumed(Guid materialId, Guid warehouseId, decimal quantity);
}