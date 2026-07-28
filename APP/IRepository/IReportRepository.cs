using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.GeneralInventory;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Reports.Procurement;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Services;
using DOMAIN.Entities.Reports.Shipments;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.Reports.HrDashboardKpi;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace APP.IRepository;

public interface IReportRepository
{
    Task<Result<ProductionReportDto>> GetProductionReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialWithStockDto>>> GetMaterialsBelowMinimumStockLevel(Guid departmentId);
    Task<Result<HrDashboardDto>> GetHumanResourceDashboardReport(MovementReportFilter filter, Guid? designationId, EmployeeType? employeeType, Gender? gender);
    Task<Result<PermanentStaffGradeReportDto>> GetPermanentStaffGradeReport(Guid? departmentId);

    Task<Result<EmployeeMovementReportDto>> GetEmployeeMovementReport(MovementReportFilter filter);

    Task<Result<StaffTotalReport>> GetStaffTotalReport(MovementReportFilter filter);

    Task<Result<StaffGenderRatioReport>> GetStaffGenderRatioReport(MovementReportFilter filter);

    Task<Result<StaffLeaveSummaryReportDto>> GetStaffLeaveSummaryReport(MovementReportFilter filter);

    Task<Result<StaffTurnoverReportDto>> GetStaffTurnoverReport(ReportFilter filter);

    Task<Result<QaDashboardDto>> GetQaDashboardReport(ReportFilter filter, Guid? productId);

    Task<Result<QcDashboardDto>> GetQcDashboardReport(ReportFilter filter, Guid? productId, Guid? materialId);

    Task<Result<WarehouseReportDto>> GetWarehouseReport(ReportFilter filter, Guid departmentId);
    Task<Result<List<MaterialBatchReservedQuantityReportDto>>> GetReservedMaterialBatchesForDepartment(
        ReportFilter filter, Guid departmentId);
    Task<Result<IEnumerable<DistributedRequisitionMaterialDto>>> GetMaterialsReadyForChecklist(ReportFilter filter, Guid userId);
    Task<Result<List<MaterialBatchDto>>> GetMaterialsReadyForAssignment(ReportFilter filter,
        Guid departmentId);
    Task<Result<LogisticsReportDto>> GetLogisticsReport(ReportFilter filter);

    Task<Result<List<FinishedGoodsTransferSummaryReportDto>>> GetFinishedGoodsTransferSummaryReport(ReportFilter filter, Guid? productId = null, Guid? warehouseId = null);
    Task<Result<List<FinishedGoodsTransferDetailedReportDto>>> GetFinishedGoodsTransferDetailedReport(ReportFilter filter, Guid? productId = null, Guid? warehouseId = null);

    Task<Result<List<ProductStockSummaryReportDto>>> GetProductStockSummaryReport(Guid? productId = null, Guid? warehouseId = null, Guid? departmentId = null);
    Task<Result<DashboardKpiReportDto>> GetDashboardKpiReport(DashboardFilterDto filter);
   Task<Result<List<SupplierMaterialReportDto>>> GetSupplierMaterialAReport(
    SupplierMaterialFilters filters)
;
    Task<Result<List<ProductStockDetailedReportDto>>> GetProductStockDetailedReport(Guid? productId = null, Guid? warehouseId = null, Guid? departmentId = null,
        string batchNumber = null, DateTime? expiryDateFrom = null, DateTime? expiryDateTo = null);
    
    Task<Result<List<ItemDto>>> GetItemsPerStoreType(Store? store, InventoryClassification? inventoryClassification,
        Guid? itemId, Guid? categoryId);

    Task<Result<List<StoreItemStockSummaryDto>>> GetStockSummaryPerStoreType();

    Task<Result<List<VendorStoreItemStockSummaryDto>>>
        GetVendorItemMapping(
            Store? store,
            Guid? vendorId,
            Guid? itemId,
            Guid? categoryId,
            InventoryClassification? classification);

    Task<Result<List<VendorItemStoreSummaryDto>>>
        GetVendorItemMappingPerStoreTypeSummary(
            Guid? itemId,
            Guid? categoryId,
            InventoryClassification? classification,
            Store? store);
    Task<Result<List<ShipmentReportDto>>> GetShipmentReport(ShipmentReportFilter filter );
    Task<Result<List<PurchaseOrderReportDto>>> GetPurchaseOrderReportAsync(PurchaseOrderFilter filter);
    Task<Result<List<PurchasedPoReportDto>>> GetPurchasedPoReportAsync(PurchaseOrderFilter filter);
    Task<Result<ProductionDashboardDto>> GetProductionDashboard(Guid departmentId);
    Task<Result<ProcurementDashboardDto>> GetProcurementDashboard(DateFilter filter);
    Task<Result<WarehouseDashboardReportDto>> GetWarehouseDashboard(Guid departmentId,DateFilter filter);
    Task<Result<List<ExpiredMaterialReportDto>>> GetExpiredMaterials(Guid departmentId);

    Task<Result<List<ReservedMaterialReportDto>>> GetReservedMaterials(Guid? departmentId = null, Guid? materialId = null);

    Task<Result<List<MaterialReorderReportDto>>> GetMaterialsBelowReorderLevel(Guid departmentId);

    Task<Result<MaterialsChecklistReportDto>> GetMaterialsChecklist(Guid departmentId);

    Task<Result<ShipmentStatusReportDto>> GetShipmentStatusReport(DateFilter filter);
    Task<Result<GeneralInventoryDashboardDto>> GetGeneralInventoryDashboard(DateFilter filter);
    Task<Result<List<ItemBelowReorderDto>>> GetItemBelowReorder( );
    Task<Result<ServicesDashboardReportDto>> GetServicesDashboard(DateFilter filter);

    Task<Result<List<InvoicedProductsSummaryReportDto>>> GetInvoicedProductsSummary(
        InvoicedProductFilters filters);
    Task<Result<List<InvoicedProductsDetailedReportDto>>> GetInvoicedProductsDetailedReport(
        InvoicedProductFilters filters);

    Task<Result<IEnumerable<WarehouseCapacityUtilisationDto>>> GetWarehouseCapacityUtilisation(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<DockToStockTimeDto>>> GetDockToStockTime(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<StockTransferFulfilmentRateDto>>> GetStockTransferFulfilmentRate(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<ReceivingPipelineSnapshotDto>>> GetReceivingPipelineSnapshot(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<ExpiryRiskIndexDto>>> GetExpiryRiskIndex(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<ReorderAlertCountDto>>> GetReorderAlertCount(
        WarehouseKpiFilterDto filter);

    Task<Result<IEnumerable<SwapRequestActivityDto>>> GetSwapRequestActivity(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<MaterialMovementCountDto>>> GetMaterialMovementCount(
        WarehouseKpiFilterDto filter, Guid? departmentId);

    Task<Result<List<MaterialsStockSummaryDto>>> GetMaterialsStockSummary(
        Guid? departmentId = null, MaterialKind? materialKind = null, Guid? materialId = null);

    Task<Result<List<MaterialsStockBatchDetailDto>>> GetMaterialsStockBatchDetail(
        Guid? departmentId = null, MaterialKind? materialKind = null,
        string batchNumber = null, DateTime? expiryDateFrom = null, DateTime? expiryDateTo = null);

    Task<Result<List<ShelfUtilisationDetailDto>>> GetShelfUtilisationDetail(
        Guid? warehouseId = null, Guid? locationId = null, OccupancyStatus? occupancyStatus = null,
        Guid? departmentId = null);

    Task<Result<List<GoodsReceivingRegisterDto>>> GetGoodsReceivingRegister(
        ReportFilter filter, string grnStatus = null, Guid? supplierId = null,
        Guid? departmentId = null);

    Task<Result<List<ReceivingPerformanceDetailDto>>> GetReceivingPerformanceDetail(
        ReportFilter filter, Guid? warehouseId = null, Guid? supplierId = null,
        Guid? departmentId = null);

    Task<Result<List<PutawayRegisterDto>>> GetPutawayRegister(
        ReportFilter filter, Guid? warehouseId = null, Guid? employeeId = null,
        Guid? departmentId = null);

    Task<Result<List<StockAdjustmentAuditDto>>> GetStockAdjustmentAudit(
        ReportFilter filter, string reasonCode = null, Guid? warehouseId = null,
        Guid? departmentId = null);

    Task<Result<List<BatchTraceabilityDto>>> GetBatchTraceability(
        Guid materialBatchId, ReportFilter filter, Guid? departmentId = null);

    Task<Result<List<InterWarehouseSwapRequestDto>>> GetInterWarehouseSwapRequests(
        ReportFilter filter, Guid? warehouseId = null, string status = null,
        Guid? departmentId = null);

    Task<Result<List<StockTransferInterDepartmentDetailDto>>> GetStockTransferInterDepartmentDetail(
        ReportFilter filter, Guid? fromDepartmentId = null, Guid? toDepartmentId = null,
        string status = null, Guid? departmentId = null);

    Task<Result<List<MaterialExpiryProjectionDto>>> GetMaterialExpiryProjection(
        ExpiryWindowFilter? window = null, Guid? warehouseId = null, Guid? materialId = null,
        Guid? departmentId = null);

    Task<Result<List<SlowMovingInventoryDto>>> GetSlowMovingInventory(
        InactivityThreshold threshold = InactivityThreshold.Days90, Guid? warehouseId = null,
        Guid? materialId = null, Guid? departmentId = null);

    Task<Result<List<BinCardTransactionLedgerDto>>> GetBinCardTransactionLedger(
        Guid materialBatchId, ReportFilter filter, Guid? departmentId = null);

    Task<Result<IEnumerable<OperationsSummaryDto>>> GetOperationsSummary(
        ReportFilter filter, Guid? warehouseId = null, Guid? departmentId = null);

    Task<Result<List<QcPendingDto>>> GetQcPendingReport(
        Guid? warehouseId = null, Guid? supplierId = null, Guid? departmentId = null);

    Task<Result<MaterialMovementCountDto>> GetMaterialMovementCount(
        WarehouseKpiFilterDto filter);

    // Production Dashboard KPI Widgets (KPI 6 - 12)
    Task<Result<List<ScheduleAdherenceDto>>> GetScheduleAdherence(ProductionKpiFilter filter);
    Task<Result<List<ProductionOutputVolumeDto>>> GetProductionOutputVolume(ProductionKpiFilter filter);
    Task<Result<List<AtrTestingBacklogDto>>> GetAtrTestingBacklog(ProductionKpiFilter filter);
    Task<Result<List<StockRequisitionPendingDto>>> GetStockRequisitionPending(ProductionKpiFilter filter);
    Task<Result<List<FgtnPendingApprovalDto>>> GetFgtnPendingApproval(ProductionKpiFilter filter);
    Task<Result<List<ProductionOrderDeliveryStatusDto>>> GetProductionOrderDeliveryStatus(ProductionKpiFilter filter, Guid? customerId);
    Task<Result<List<MaterialReturnRateDto>>> GetMaterialReturnRate(ProductionKpiFilter filter);
    Task<Result<List<ReorderLevelVsStockDto>>> GetReorderLevelVsStock(
        Guid? departmentId = null, MaterialKind? materialKind = null);

    Task<Result<List<InventoryValuationSummaryDto>>> GetInventoryValuationSummary(
        WarehouseType? warehouseType = null, Division? division = null,
        UnitOfMeasureCategory? uomGroup = null, Guid? departmentId = null);

    Task<Result<List<InventoryValuationDetailDto>>> GetInventoryValuationDetail(
        WarehouseType? warehouseType = null, Division? division = null,
        Guid? warehouseId = null, string valuationType = null,
        Guid? materialId = null, DateTime? expiryDateFrom = null, DateTime? expiryDateTo = null,
        Guid? departmentId = null);

    Task<Result<List<WarehouseEmployeeActivityDto>>> GetWarehouseEmployeeActivity(
        ReportFilter filter, Guid? employeeId = null, Guid? departmentId = null);

    Task<Result<List<ArrivalLocationStatusDto>>> GetArrivalLocationStatus(
        Guid? warehouseId = null, string status = null, int agingThresholdDays = 3,
        Guid? departmentId = null);

    Task<Result<IEnumerable<EmployeeHeadcountSnapshotDto>>> GetEmployeeHeadcountSnapshot(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<EmployeeGenderRatioDto>>> GetEmployeeGenderRatio(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<LeaveRequestPipelineDto>>> GetLeaveRequestPipeline(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<OvertimeRequestActivityDto>>> GetOvertimeRequestActivity(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<DailyAttendanceRateDto>>> GetDailyAttendanceRate(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<StaffRequisitionPipelineDto>>> GetStaffRequisitionPipeline(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<EmployeeGradeLevelDistributionDto>>> GetEmployeeGradeLevelDistribution(
        Guid? departmentId);

    Task<Result<IEnumerable<NewHiresThisPeriodDto>>> GetNewHiresThisPeriod(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<EmployeeTurnoverRateDto>>> GetEmployeeTurnoverRate(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<LeaveUtilisationRateDto>>> GetLeaveUtilisationRate(
        HrKpiFilterDto filter, Guid? departmentId);

    Task<Result<IEnumerable<ActiveDisciplinaryActionsDto>>> GetActiveDisciplinaryActions(
        Guid? departmentId);
}
