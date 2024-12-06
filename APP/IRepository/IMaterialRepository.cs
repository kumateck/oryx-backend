using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace APP.IRepository;

public interface IMaterialRepository
{
    Task<Result<Guid>> CreateMaterial(CreateMaterialRequest request, Guid userId);
    Task<Result<MaterialDto>> GetMaterial(Guid materialId);
    Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery, MaterialKind kind); 
    Task<Result<List<MaterialDto>>> GetMaterials();
    Task<Result> UpdateMaterial(CreateMaterialRequest request, Guid materialId, Guid userId);
    Task<Result> DeleteMaterial(Guid materialId, Guid userId);
    Task<Result<int>> CheckStockLevel(Guid materialId);
    //Task<Result<bool>> CanFulfillRequisition(Guid materialId, Guid requisitionId);
    Task<Result> CreateMaterialBatch(List<CreateMaterialBatchRequest> request, Guid userId);
    Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId);
    Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(int page, int pageSize,
        string searchQuery);
    Task<Result> MoveMaterialBatch(Guid batchId, Guid fromLocationId, Guid toLocationId, int quantity,
        Guid userId);
    Task<Result<int>> GetMaterialStockInWarehouse(Guid materialId, Guid warehouseId); 
    Task<Result> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, int quantity, Guid userId); 
    Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(Guid materialId);
}