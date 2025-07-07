using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
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
        GetIncomingStockTransferRequestForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null,
            StockTransferStatus? status = null,  Guid? toDepartmentId = null);
    Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetOutgoingStockTransferRequestForUserDepartment(
        Guid userId, int page, int pageSize, string searchQuery = null,
        StockTransferStatus? status = null, Guid? fromDepartmentId = null);
    Task<Result<DepartmentStockTransferDto>> GetStockTransferSource(Guid stockTransferId);
    Task<Result> ApproveStockTransfer(Guid id, Guid userId);
    Task<Result> RejectStockTransfer(Guid id, Guid userId);

    Task<Result<List<BatchToSupply>>> BatchesToSupplyForStockTransfer(Guid stockTransferId);
    Task<Result> IssueStockTransfer(Guid id, List<BatchTransferRequest> batches, Guid userId);

    Task<Result<List<ProductionScheduleProcurementDto>>> GetMaterialsWithInsufficientStock(Guid productionScheduleId,
        Guid productId, Guid userId);
    Task<Result<List<ProductionScheduleProcurementPackageDto>>> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleId,
        Guid productId, Guid userId);
    Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecordByProductionAndScheduleId(Guid productionId, Guid productionScheduleId);
    Task<Result> CreateFinishedGoodsTransferNote(CreateFinishedGoodsTransferNoteRequest request, Guid userId);
    
    Task<Result<FinishedGoodsTransferNoteDto>> GetFinishedGoodsTransferNote(Guid id);
    Task<Result> ApproveTransferNote(Guid id, ApproveTransferNoteRequest request);
    
    Task<Result> UpdateTransferNote(Guid id, CreateFinishedGoodsTransferNoteRequest request);
    
    
    Task<Result<Guid>> CreateFinalPacking(CreateFinalPacking request);
    Task<Result<FinalPackingDto>> GetFinalPacking(Guid finalPackingId);
    Task<Result<FinalPackingDto>> GetFinalPackingByScheduleAndProduct(Guid productionScheduleId, Guid productId);
    Task<Result<Paginateable<IEnumerable<FinalPackingDto>>>> GetFinalPackings(int page, int pageSize, string searchQuery);
    Task<Result> UpdateFinalPacking(CreateFinalPacking request, Guid finalPackingId);
    Task<Result> DeleteFinalPacking(Guid finalPackingId, Guid userId);
    Task<Result<RequisitionDto>> GetStockRequisitionForPackaging(Guid productionScheduleId, Guid productId);
    Task<Result<ProductionScheduleProductDto>> GetProductDetailsInProductionSchedule(
        Guid productionScheduleId, Guid productId);

    Task<Result> ReturnStockBeforeProductionBegins(Guid productionScheduleId, Guid productId, string reason);
    Task<Result> ReturnLeftOverStockAfterProductionEnds(Guid productionScheduleId, Guid productId,
        List<PartialMaterialToReturn> returns);
    Task<Result<Paginateable<IEnumerable<MaterialReturnNoteDto>>>> GetMaterialReturnNotes(int page,
        int pageSize,
        string searchQuery);
    Task<Result<MaterialReturnNoteDto>> GetMaterialReturnNoteById(Guid materialReturnNoteId);
    Task<Result> CompleteMaterialReturn(Guid materialReturnNoteId);

    Task<Result> CreateExtraPacking(Guid productionScheduleId, Guid productId,
        List<CreateProductionExtraPacking> extraPackings);
   Task<Result<Paginateable<IEnumerable<ProductionExtraPackingWithBatchesDto>>>> GetProductionExtraPackings(int page,
        int pageSize, string searchQuery);
   Task<Result<ProductionExtraPackingWithBatchesDto>> GetProductionExtraPackingById(
       Guid productionExtraPackingId);
  Task<Result<List<ProductionExtraPackingWithBatchesDto>>> GetProductionExtraPackingByProduct(
       Guid productionScheduleId, Guid productId);
   Task<Result<List<BatchToSupply>>> BatchesToSupplyForExtraPackingMaterial(Guid extraPackingMaterialId);
   Task<Result> ApproveProductionExtraPacking(Guid productionExtraPackingId,
       List<BatchTransferRequest> batches, Guid userId);
   Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>> GetFinishedGoodsTransferNote(int page, int pageSize,
       string searchQuery = null);
   Task<Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>> GetProductBinCardInformation(
       int page, int pageSize,
       string searchQuery, Guid productId);
   Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>> GetFinishedGoodsTransferNoteByProduct(int page, int pageSize, 
       string searchQuery, Guid productId);
   Task<Result<IEnumerable<ProductionScheduleReportDto>>> GetProductionScheduleSummaryReport(
       ProductionScheduleReportFilter filter);
   Task<Result<IEnumerable<ProductionScheduleDetailedReportDto>>> GetProductionScheduleDetailedReport(
       ProductionScheduleReportFilter filter);
}