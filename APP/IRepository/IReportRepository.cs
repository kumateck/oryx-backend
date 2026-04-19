using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.GeneralInventory;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Reports.Procurement;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Services;
using DOMAIN.Entities.Reports.Shipments;
using DOMAIN.Entities.Reports.Warehouse;
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
}
