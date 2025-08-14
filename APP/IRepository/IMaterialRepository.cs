using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
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
    Task<Result> UpdateReOrderLevel(Guid materialId, int reOrderLevel, Guid userId);
    Task<Result> DeleteMaterial(Guid materialId, Guid userId);
    Task<Result<decimal>> CheckStockLevel(Guid materialId);
    //Task<Result<bool>> CanFulfillRequisition(Guid materialId, Guid requisitionId);
    Task<Result> CreateMaterialBatch(List<CreateMaterialBatchRequest> request, Guid userId);
    Task<Result> CreateMaterialBatchWithoutBatchMovement(List<CreateMaterialBatchRequest> request,
        Guid userId);
    Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId);
    Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(int page, int pageSize,
        string searchQuery);
    Task<Result<List<MaterialBatchDto>>> GetMaterialBatchesByMaterialId(Guid materialId);
    Task<Result<decimal>> GetMaterialsInTransit(Guid materialId);
    Task<Result> MoveMaterialBatchByMaterial(MoveMaterialBatchRequest request, Guid userId);
    Task<Result> ApproveMaterialBatch(Guid batchId, Guid userId);
    Task<Result> MoveMaterialBatch(Guid batchId, Guid fromLocationId, Guid toLocationId, decimal quantity,
        Guid userId);
    //Task<Result<decimal>> GetMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<List<DepartmentDto>>> GetDepartmentsWithEnoughStock(Guid materialId, decimal quantity);
    Task<Result<decimal>> GetMassMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<decimal>> GetShelfMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<decimal>> GetFrozenMaterialStockInWarehouse(Guid materialId, Guid warehouseId);
    Task<Result<List<MaterialBatchDto>>> GetFrozenMaterialBatchesInWarehouse(Guid materialId,
        Guid warehouseId);
    Task<Result> FreezeMaterialBatchAsync(Guid batchId);
    Task<Result> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, decimal quantity, Guid userId);
    Task<Result> ConsumeMaterialAtLocation(Material material, Guid locationId, decimal quantity,
        Guid userId);
    Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(Guid materialId); 
    Task<Result> ImportMaterialsFromExcel(IFormFile file, MaterialKind kind);
    Result<List<BatchLocation>> BatchesNeededToBeConsumed(Guid materialId, Guid warehouseId, decimal quantity);
    Task<Result> UpdateBatchStatus(UpdateBatchStatusRequest request, Guid userId);
    Task<Result> MoveMaterialBatchV2(MoveShelfMaterialBatchRequest request, Guid userId);
    Task<Result> SupplyMaterialBatchToWarehouse(SupplyMaterialBatchRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<MaterialDetailsDto>>>> GetApprovedMaterials(int page, int pageSize, string searchQuery, MaterialKind kind, Guid userId);

    Task<Result<Paginateable<IEnumerable<ShelfMaterialBatchDto>>>> GetMaterialBatchesByMaterialIdV2(int page,
        int pageSize, Guid materialId, Guid userId);
    Task<List<MaterialStockByWarehouseDto>> GetStockByWarehouse(Guid materialId);
    Task<List<MaterialStockByDepartmentDto>> GetStockByDepartment(Guid materialId);
    Task<Result<List<BatchToSupply>>> BatchesToSupplyForGivenQuantity(Guid materialId,
        Guid warehouseId, decimal quantity);

    Task<Result<decimal>> GetProductStockInWarehouseByBatch(Guid batchId, Guid warehouseId);
    Task<Result<List<BatchToSupply>>> GetFrozenBatchesForRequisitionItem(Guid materialId, Guid warehouseId,
        decimal requestedQuantity);
    Task ReserveQuantityFromBatchForProduction(Guid batchId, Guid warehouseId, Guid productionScheduleId, Guid productId, decimal quantity, Guid? uomId);
    Task<List<MaterialBatchReservedQuantityDto>> GetReservedBatchesAndQuantityForProductionWarehouse(Guid materialId, 
        Guid warehouseId, Guid productionScheduleId, Guid productId);

    Task<Result<decimal>> GetMaterialStockInWarehouseByBatch(Guid batchId, Guid warehouseId);
    Task<Result> CreateMaterialDepartment(List<CreateMaterialDepartment> materialDepartments,
        Guid userId);
    Task<Result<Paginateable<IEnumerable<MaterialWithWarehouseStockDto>>>> GetMaterialsThatHaveNotBeenLinked(int page, int pageSize, string searchQuery, MaterialKind? kind, Guid userId);
    Task<Result<Paginateable<IEnumerable<MaterialDepartmentWithWarehouseStockDto>>>> GetMaterialDepartments(int page,
        int pageSize,
        string searchQuery, MaterialKind? kind, Guid userId);
    Task<Result<UnitOfMeasureDto>> GetUnitOfMeasureForMaterialDepartment(Guid materialId, Guid userId);
    Task<Result<Paginateable<IEnumerable<HoldingMaterialTransferDto>>>> GetHoldingMaterialTransfers(
        int page,
        int pageSize, string searchQuery, bool withProcessed, Guid? userId);
    Task<Result> MoveMaterialBatchToWarehouseFromHolding(Guid holdingMaterialId, 
        MoveShelfMaterialBatchRequest request, Guid userId);
   Task<Result> ImportMaterialBatchesFromExcel(IFormFile file, Guid userId);
  Task<Result<List<MaterialBatchDto>>> GetExpiredMaterialBatches(MaterialFilter filter);

  Task<Result<List<MaterialDto>>> GetMaterialsNotLinkedToSpec(MaterialKind kind);

}