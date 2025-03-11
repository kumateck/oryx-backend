using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products.Production;
using SHARED;

namespace APP.IRepository;

public interface IProductionScheduleRepository
{
    Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId);
    Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId);
    Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page,
        int pageSize, string searchQuery);
    Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId,
        Guid userId);
    Task<Result> DeleteProductionSchedule(Guid scheduleId, Guid userId);
    Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(
        Guid scheduleId, Guid userId);

    Task<Result<Guid>> StartProductionActivity(Guid productionScheduleId, Guid productId, Guid userId);
    Task<Result> UpdateStatusOfProductionActivityStep(Guid productionStepId, ProductionStatus status,
        Guid userId); 
    Task<Result<Paginateable<IEnumerable<ProductionActivityListDto>>>> GetProductionActivities(
        ProductionFilter filter); 
    Task<Result<ProductionActivityDto>> GetProductionActivityById(Guid productionActivityId);
    Task<Result<ProductionActivityDto>> GetProductionActivityByProductionScheduleIdAndProductId(
        Guid productionScheduleId, Guid productId);
    Task<Result<Paginateable<IEnumerable<ProductionActivityStepDto>>>> GetProductionActivitySteps(
        ProductionFilter filter);

    Task<Result<ProductionActivityStepDto>> GetProductionActivityStepById(Guid productionActivityStepId);
    Task<Result<Dictionary<string, List<ProductionActivityDto>>>> GetProductionActivityGroupedByStatus();

    Task<Result<List<ProductionActivityGroupResultDto>>>  GetProductionActivityGroupedByOperation();

    Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByOperation();

    Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByStatus();
    Task<Result<List<ProductionScheduleProcurementDto>>> CheckMaterialStockLevelsForProductionSchedule(Guid productionScheduleId, Guid productId, MaterialRequisitionStatus? status, Guid userId);
    Task<Result<List<ProductionScheduleProcurementPackageDto>>> CheckPackageMaterialStockLevelsForProductionSchedule(Guid productionScheduleId, Guid productId,MaterialRequisitionStatus? status, Guid userId);
    
    Task<Result<Guid>> CreateBatchManufacturingRecord(CreateBatchManufacturingRecord request);
    Task<Result<Paginateable<IEnumerable<BatchManufacturingRecordDto>>>> GetBatchManufacturingRecords(
        int page, int pageSize, string searchQuery = null, ProductionStatus? status = null);
    Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecord(Guid id);
    Task<Result> UpdateBatchManufacturingRecord(UpdateBatchManufacturingRecord request, Guid id);
    Task<Result> IssueBatchManufacturingRecord(Guid id, Guid userId);
    Task<Result<Guid>> CreateBatchPackagingRecord(CreateBatchPackagingRecord request);
    Task<Result<Paginateable<IEnumerable<BatchPackagingRecordDto>>>> GetBatchPackagingRecords(int page,
        int pageSize, string searchQuery = null, ProductionStatus? status = null);
    Task<Result<BatchPackagingRecordDto>> GetBatchPackagingRecord(Guid id); 
    Task<Result> UpdateBatchPackagingRecord(UpdateBatchPackagingRecord request, Guid id);
    Task<Result> IssueBatchPackagingRecord(Guid id, Guid userId);

    Task<Result<Guid>> CreateStockTransfer(CreateStockTransferRequest request, Guid userId);
    Task<Result<IEnumerable<StockTransferDto>>> GetStockTransfers(Guid? fromDepartmentId = null,
        Guid? toDepartmentId = null, Guid? materialId = null);
    Task<Result<Paginateable<IEnumerable<StockTransferDto>>>> GetStockTransfersForUserDepartment(
        Guid userId, int page, int pageSize, string searchQuery = null, StockTransferStatus? status = null);
    Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>>
        GetStockTransferSourceForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null,
            StockTransferStatus? status = null);
    Task<Result> ApproveStockTransfer(Guid id, Guid userId);
    Task<Result> IssueStockTransfer(Guid id, List<BatchTransferRequest> batches, Guid userId);

    Task<Result<List<ProductionScheduleProcurementDto>>> GetMaterialsWithInsufficientStock(Guid productionScheduleId,
        Guid productId, Guid userId);
    Task<Result<List<ProductionScheduleProcurementPackageDto>>> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleId,
        Guid productId, Guid userId);
}