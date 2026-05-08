using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using SHARED;

namespace APP.IRepository;

public interface IProductionScheduleRepository
{
    Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId);
    Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId);
    Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(
        Guid roleId,
        int page,
        int pageSize, 
        string searchQuery, 
        Guid departmentId);
    Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId,
        Guid userId);
    Task<Result> AddProductToSchedule(Guid scheduleId, AddProductsToScheduleRequest request, Guid userId);
    Task<Result> RemoveProductFromSchedule(Guid productionScheduleProductId);
    Task<Result> DeleteProductionSchedule(Guid scheduleId, Guid userId);
    Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(
        Guid scheduleId, Guid userId);

    Task<Result<Guid>> StartProductionActivity(Guid productionScheduleProductId, Guid userId);
    Task<Result> UpdateStatusOfProductionActivityStep(Guid productionStepId, ProductionStatus status,
        Guid userId);
    Task<Result<Paginateable<IEnumerable<ProductionActivityListDto>>>> GetProductionActivities(
        ProductionFilter filter);
    Task<Result<ProductionActivityDto>> GetProductionActivityById(Guid productionActivityId);
    Task<Result<ProductionActivityDto>> GetProductionActivityByProductionScheduleProduct(
        Guid productionScheduleProductId);
    Task<Result<Paginateable<IEnumerable<ProductionActivityStepDto>>>> GetProductionActivitySteps(
        ProductionFilter filter);

    Task<Result<ProductionActivityStepDto>> GetProductionActivityStepById(Guid productionActivityStepId);
    Task<Result<Dictionary<string, List<ProductionActivityDto>>>> GetProductionActivityGroupedByStatus();

    Task<Result<List<ProductionActivityGroupResultDto>>> GetProductionActivityGroupedByOperation(Guid? departmentId);

    Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByOperation();

    Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByStatus();
    Task<Result<List<ProductionScheduleProcurementDto>>> CheckMaterialStockLevelsForProductionSchedule(Guid productionScheduleProductId, MaterialRequisitionStatus? status);
    Task<Result<List<ProductionScheduleProcurementPackageDto>>> CheckPackageMaterialStockLevelsForProductionSchedule(Guid productionScheduleProductId, MaterialRequisitionStatus? status);

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
            StockTransferStatus? status = null, Guid? toDepartmentId = null);
    Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetOutgoingStockTransferRequestForUserDepartment(
        Guid userId, int page, int pageSize, string searchQuery = null,
        StockTransferStatus? status = null, Guid? fromDepartmentId = null);
    Task<Result<DepartmentStockTransferDto>> GetStockTransferSource(Guid stockTransferId);
    Task<Result> ApproveStockTransfer(Guid id, Guid userId);
    Task<Result> RejectStockTransfer(Guid id, Guid userId);

    Task<Result<List<BatchToSupply>>> BatchesToSupplyForStockTransfer(Guid stockTransferId);
    Task<Result> IssueStockTransfer(Guid id, List<BatchTransferRequest> batches, Guid userId);

    Task<Result<List<ProductionScheduleProcurementDto>>> GetMaterialsWithInsufficientStock(Guid productionScheduleProductId);
    Task<Result<List<ProductionScheduleProcurementPackageDto>>> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleProductId);
    Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecordByProductionAndScheduleId(Guid productionScheduleProductId);
    Task<Result> CreateFinishedGoodsTransferNoteQuantity(
        CreateFinishedGoodsTransferNoteQuantityRequest request,
        Guid userId);
    Task<decimal> GetRemainderOfFinishedGoodsQuantityFromBmr(Guid batchManufacturingRecordId);
    Task<Result> CreateFinishedGoodsTransferNote(CreateFinishedGoodsTransferNoteRequest request, Guid userId);

    Task<Result<FinishedGoodsTransferNoteDto>> GetFinishedGoodsTransferNote(Guid id);
    Task<Result<List<FinishedGoodsTransferNoteDto>>> GetFinishedGoodsTransferNotesByBmr(
        Guid batchManufacturingRecordId);
    Task<Result> ApproveTransferNote(Guid id, ApproveTransferNoteRequest request);

    Task<Result> UpdateTransferNote(Guid id, CreateFinishedGoodsTransferNoteRequest request);
    Task<Result<IEnumerable<ApprovedProductDto>>> GetApprovedProducts(Guid roleId,
        Guid departmentId);
    Task<Result<ApprovedProductDetailDto>> GetApprovedProduct(Guid productId);
    Task<Result<IEnumerable<FinishedGoodsTransferNoteDto>>> GetApprovedProductDetails(Guid productId);
    Task<Result<Guid>> CreateFinalPacking(CreateFinalPacking request);
    Task<Result<FinalPackingDto>> GetFinalPacking(Guid finalPackingId);
    Task<Result<FinalPackingDto>> GetFinalPackingByScheduleProduct(Guid productionScheduleProductId);
    Task<Result<Paginateable<IEnumerable<FinalPackingDto>>>> GetFinalPackings(int page, int pageSize, string searchQuery);
    Task<Result> UpdateFinalPacking(CreateFinalPacking request, Guid finalPackingId);
    Task<Result> DeleteFinalPacking(Guid finalPackingId, Guid userId);
    Task<Result<RequisitionDto>> GetStockRequisitionForRaw(Guid productionScheduleProductId);
    Task<Result<RequisitionDto>> GetStockRequisitionForPackaging(Guid productionScheduleProductId);
    Task<Result<ProductionScheduleProductDto>> GetProductDetailsInProductionSchedule(
        Guid productionScheduleProductId);

    Task<Result> ReturnStockBeforeProductionBegins(Guid productionScheduleProductId, string reason);
    Task<Result> ReturnLeftOverStockAfterProductionEnds(Guid productionScheduleProductId,
        List<PartialMaterialToReturn> returns);
    Task<Result<Paginateable<IEnumerable<MaterialReturnNoteDto>>>> GetMaterialReturnNotes(int page,
        int pageSize,
        string searchQuery);
    Task<Result<MaterialReturnNoteDto>> GetMaterialReturnNoteById(Guid materialReturnNoteId);
    Task<Result> CompleteMaterialReturn(Guid materialReturnNoteId);

    Task<Result> CreateExtraPacking(Guid productionScheduleProductId,
        List<CreateProductionExtraPacking> extraPackings);
    Task<Result<Paginateable<IEnumerable<ProductionExtraPackingWithBatchesDto>>>> GetProductionExtraPackings(int page,
         int pageSize, string searchQuery, MaterialKind? kind);
    Task<Result<ProductionExtraPackingWithBatchesDto>> GetProductionExtraPackingById(
        Guid productionExtraPackingId);
    Task<Result<List<ProductionExtraPackingWithBatchesDto>>> GetProductionExtraPackingByProduct(
        Guid productionScheduleProductId);
    Task<Result<List<BatchToSupply>>> BatchesToSupplyForExtraPackingMaterial(Guid extraPackingMaterialId);
    Task<Result> ApproveProductionExtraPacking(Guid productionExtraPackingId,
        List<BatchTransferRequest> batches, Guid userId);

    Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>> GetFinishedGoodsTransferNote(
        Guid roleId,
        Guid departmentId,
        int page,
        int pageSize,
        string searchQuery = null,
        Division? division = null,
        bool? onlyApproved = null,
        bool? partial = null,
        bool? fulfilled = null);
    Task<Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>> GetProductBinCardInformation(
        int page, int pageSize,
        string searchQuery, Guid productId);
    Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>> GetFinishedGoodsTransferNoteByProduct(Guid departmentId,
        int page, int pageSize,
        string searchQuery, Guid productId);
    Task<Result<IEnumerable<ProductionScheduleReportDto>>> GetProductionScheduleSummaryReport(
        ProductionScheduleReportFilter filter);
    Task<Result<IEnumerable<ProductionScheduleDetailedReportDto>>> GetProductionScheduleDetailedReport
        (ProductionScheduleReportFilter filter);
    Task<Result<List<ForecastMaterialDto>>> ForecastProductionScheduleProduct(Guid productId,
        int numberOfBatches,
        Guid productPackingId,
        BatchSize batchSize,
        Guid userId);
}