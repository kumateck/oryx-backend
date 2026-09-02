using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.GeneralInventory;
using DOMAIN.Entities.Reports.HrDashboardKpi;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Reports.Procurement;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Services;
using DOMAIN.Entities.Reports.Shipments;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/report")]
[ApiController]
[Authorize]
public class ReportController(IReportRepository repository) : ControllerBase
{
    private bool TryGetAuthenticatedDepartment(out Guid departmentId)
    {
        var value = HttpContext.Items["Department"] as string;
        return Guid.TryParse(value, out departmentId);
    }

    /// <summary>
    /// Gets the production report for a specific department.
    /// </summary>
    [HttpGet("production")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionReport([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetProductionReport(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a list of materials that are below the minimum stock level for a specific department.
    /// </summary>
    [HttpGet("production/materials-below-minimum")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialWithStockDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsBelowMinimumStockLevel()
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsBelowMinimumStockLevel(Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the warehouse report for a specific department.
    /// </summary>
    [HttpGet("warehouse")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouseReport([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetWarehouseReport(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the logistics reporting dashboard
    /// </summary>
    [HttpGet("logistics")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LogisticsReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetLogisticsReport([FromQuery] ReportFilter filter)
    {
        //var departmentId = (string)HttpContext.Items["Department"];
        //if (departmentId == null) return TypedResults.Unauthorized();

        var result = await repository.GetLogisticsReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets reserved material batches for a specific department.
    /// </summary>
    [HttpGet("reserved-material-batches")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<MaterialBatchReservedQuantityReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetReservedMaterialBatches([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetReservedMaterialBatchesForDepartment(
            filter,
            Guid.Parse(departmentId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the human resource report
    /// </summary>
    [HttpGet("human-resource")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HrDashboardDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetHumanResourceReport(
        [FromQuery] MovementReportFilter filter,
        [FromQuery] Guid? designationId,
        [FromQuery] EmployeeType? employeeType,
        [FromQuery] Gender? gender
    )
    {
        var result = await repository.GetHumanResourceDashboardReport(
            filter,
            designationId,
            employeeType,
            gender
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the report detailing the grade-wise count of permanent staff across departments.
    /// </summary>
    [HttpGet("staff-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PermanentStaffGradeReportDto))]
    public async Task<IResult> GetPermanentStaffGradeReport([FromQuery] Guid? departmentId)
    {
        var result = await repository.GetPermanentStaffGradeReport(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the employee movement report based on the specified filter.
    /// </summary>
    [HttpGet("employee-movement")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeMovementReportDto>))]
    public async Task<IResult> GetEmployeeMovementReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetEmployeeMovementReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the staff total report based on the specified filter.
    /// </summary>
    [HttpGet("staff-total-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StaffTotalReport))]
    public async Task<IResult> GetStaffTotalReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetStaffTotalReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the staff gender ratio report
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    [HttpGet("staff-gender-ratio-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StaffGenderRatioReport))]
    public async Task<IResult> GetStaffGenderRatioReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetStaffGenderRatioReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("staff-leave-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StaffLeaveSummaryReportDto))]
    public async Task<IResult> GetStaffLeaveSummaryReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetStaffLeaveSummaryReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("staff-turnover-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StaffTurnoverReportDto))]
    public async Task<IResult> GetStaffTurnoverReport([FromQuery] MovementReportFilter filter)
    {
        var result = await repository.GetStaffTurnoverReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a list of materials ready for checklist for a specific user.
    /// </summary>
    [HttpGet("materials-ready-for-checklist")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<DistributedRequisitionMaterialDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsReadyForChecklist([FromQuery] ReportFilter filter)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsReadyForChecklist(filter, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets a list of materials ready for assignment for a specific department.
    /// </summary>
    [HttpGet("materials-ready-for-assignment")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialBatchDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsReadyForAssignment([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsReadyForAssignment(
            filter,
            Guid.Parse(departmentId)
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("qa-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QaDashboardDto))]
    public async Task<IResult> GetQaDashboard(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId
    )
    {
        var result = await repository.GetQaDashboardReport(filter, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("qc-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QaDashboardDto))]
    public async Task<IResult> GetQcDashboard(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? materialId
    )
    {
        var result = await repository.GetQcDashboardReport(filter, productId, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("finished-goods-transfer-summary")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<FinishedGoodsTransferSummaryReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferSummaryReport(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null
    )
    {
        var result = await repository.GetFinishedGoodsTransferSummaryReport(
            filter,
            productId,
            warehouseId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("finished-goods-transfer-detailed")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<FinishedGoodsTransferDetailedReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferDetailedReport(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null
    )
    {
        var result = await repository.GetFinishedGoodsTransferDetailedReport(
            filter,
            productId,
            warehouseId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the product stock summary report.
    /// </summary>
    [HttpGet("product-stock-summary")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<ProductStockSummaryReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductStockSummaryReport(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? departmentId
    )
    {
        var result = await repository.GetProductStockSummaryReport(
            productId,
            warehouseId,
            departmentId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the detailed product stock report with batch and expiry information.
    /// </summary>
    [HttpGet("product-stock-detailed")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<ProductStockDetailedReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductStockDetailedReport(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? batchNumber = null,
        [FromQuery] DateTime? expiryDateFrom = null,
        [FromQuery] DateTime? expiryDateTo = null
    )
    {
        var result = await repository.GetProductStockDetailedReport(
            productId,
            warehouseId,
            departmentId,
            batchNumber,
            expiryDateFrom,
            expiryDateTo
        );

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the dashboard KPI report for finished goods transfer notes.
    /// </summary>
    [HttpGet("fgtn/dashboard-kpi")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardKpiReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDashboardKpiReport([FromQuery] DashboardFilterDto filter)
    {
        var result = await repository.GetDashboardKpiReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a report of supplier materials based on filters.
    /// </summary>
    [HttpGet("supplier-materials")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierMaterialReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplierMaterialReport(
        [FromQuery] SupplierMaterialFilters filters
    )
    {
        var result = await repository.GetSupplierMaterialAReport(filters);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of items
    /// </summary>
    [HttpGet("items-per-store-type")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ItemDto>))]
    public async Task<IResult> GetItemsPerStoreType(
        [FromQuery] Store? store,
        [FromQuery] InventoryClassification? inventoryClassification,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? categoryId
    )
    {
        var result = await repository.GetItemsPerStoreType(
            store,
            inventoryClassification,
            itemId,
            categoryId
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Provides a stock quantity overview per store type, showing total item quantities.
    /// </summary>
    [HttpGet("stock-summary-per-store-type")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StoreItemStockSummaryDto>))]
    public async Task<IResult> GetStockSummaryPerStoreType()
    {
        var result = await repository.GetStockSummaryPerStoreType();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Provides a stock quantity overview per store type, showing total item quantities.
    /// </summary>
    [HttpGet("vendor-item")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<VendorStoreItemStockSummaryDto>)
    )]
    public async Task<IResult> GetVendorItemMapping(
        [FromQuery] Store? store,
        [FromQuery] Guid? vendorId,
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? categoryId,
        [FromQuery] InventoryClassification? classification
    )
    {
        var result = await repository.GetVendorItemMapping(
            store,
            vendorId,
            itemId,
            categoryId,
            classification
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Provides a stock quantity overview per store type, showing total item quantities.
    /// </summary>
    [HttpGet("vendor-item/summary")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<VendorStoreItemStockSummaryDto>)
    )]
    public async Task<IResult> GetVendorItemMappingSummary(
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? categoryId,
        [FromQuery] InventoryClassification? classification,
        [FromQuery] Store? store
    )
    {
        var result = await repository.GetVendorItemMappingPerStoreTypeSummary(
            itemId,
            categoryId,
            classification,
            store
        );
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the shipment report.
    /// </summary>
    [HttpGet("shipment")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ShipmentReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShipmentReport([FromQuery] ShipmentReportFilter filter)
    {
        var result = await repository.GetShipmentReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the purchase order report based on the specified filter.
    /// </summary>
    [HttpGet("purchase-orders")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PurchaseOrderReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPurchaseOrderReport([FromQuery] PurchaseOrderFilter filter)
    {
        var result = await repository.GetPurchaseOrderReportAsync(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the purchased PO report based on the specified filter.
    /// </summary>
    [HttpGet("purchased-po-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PurchasedPoReportDto>))]
    public async Task<IResult> GetPurchasedPoReport([FromQuery] PurchaseOrderFilter filter)
    {
        var result = await repository.GetPurchasedPoReportAsync(filter);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the production dashboard report for a specific department.
    /// </summary>
    [HttpGet("production-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionDashboardDto))]
    public async Task<IResult> GetProductionDashboard()
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetProductionDashboard(Guid.Parse(departmentId));

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the procurement dashboard KPIs (requisitions, POs, quotations, distributions)
    /// </summary>
    [HttpGet("procurement-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProcurementDashboardDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetProcurementDashboard(
        [FromQuery] DateFilter filter = DateFilter.AllTime
    )
    {
        var result = await repository.GetProcurementDashboard(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseDashboardReportDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetWarehouseDashboard(
        [FromQuery] DateFilter filter = DateFilter.AllTime
    )
    {
        var departmentIdStr = (string)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var result = await repository.GetWarehouseDashboard(departmentId, filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets list of expired material batches in the current department's warehouses
    /// </summary>
    [HttpGet("expired-materials")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ExpiredMaterialReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetExpiredMaterials()
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var result = await repository.GetExpiredMaterials(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets currently reserved material quantities for production in the department
    /// </summary>
    [HttpGet("reserved-materials")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReservedMaterialReportDto>))]
    public async Task<IResult> GetReservedMaterials(Guid? materialId)
    {
        var departmentIdStr = (string)HttpContext.Items["Department"];
        Guid? departmentId = null;
        if (
            !string.IsNullOrWhiteSpace(departmentIdStr)
            && Guid.TryParse(departmentIdStr, out var deptId)
        )
        {
            departmentId = deptId;
        }

        var result = await repository.GetReservedMaterials(departmentId, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets materials whose current stock is at or below reorder level
    /// </summary>
    [HttpGet("materials-below-reorder")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialReorderReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialsBelowReorderLevel()
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsBelowReorderLevel(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets checklist of incoming distributed materials (checked vs not checked yet)
    /// </summary>
    [HttpGet("materials-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MaterialsChecklistReportDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialsChecklist()
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsChecklist(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets current status distribution of all shipments (New, At Port, Cleared, In Transit, Arrived)
    /// </summary>
    [HttpGet("shipment-status-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShipmentStatusReportDto))]
    public async Task<IResult> GetShipmentStatusSummary(
        [FromQuery] DateFilter filter = DateFilter.AllTime
    )
    {
        var result = await repository.GetShipmentStatusReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets general inventory dashboard summary
    /// </summary>
    [HttpGet("general-inventory-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GeneralInventoryDashboardDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetGeneralInventoryDashboard(
        [FromQuery] DateFilter filter = DateFilter.AllTime
    )
    {
        var result = await repository.GetGeneralInventoryDashboard(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets items whose available quantity is at or below reorder level
    /// </summary>
    [HttpGet("items-below-reorder")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ItemBelowReorderDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetItemBelowReorder()
    {
        var result = await repository.GetItemBelowReorder();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets services dashboard summary
    /// </summary>
    [HttpGet("services-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServicesDashboardReportDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetServicesDashboard(
        [FromQuery] DateFilter filter = DateFilter.AllTime
    )
    {
        var result = await repository.GetServicesDashboard(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets summary of invoiced products allocated to customers
    /// </summary>
    [HttpGet("invoiced-products-summary")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<InvoicedProductsSummaryReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInvoicedProductsSummary(
        [FromQuery] InvoicedProductFilters filters
    )
    {
        var result = await repository.GetInvoicedProductsSummary(filters);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets detailed list of invoiced products with allocation and batch information
    /// </summary>
    [HttpGet("invoiced-products-detailed")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(List<InvoicedProductsDetailedReportDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInvoicedProductsDetailed(
        [FromQuery] InvoicedProductFilters filters
    )
    {
        var result = await repository.GetInvoicedProductsDetailedReport(filters);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets warehouse capacity utilization — percentage of occupied vs total shelf slots.
    /// </summary>
    [HttpGet("warehouse-kpi/capacity-utilisation")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<WarehouseCapacityUtilisationDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetWarehouseCapacityUtilisation(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division
        };
        var result = await repository.GetWarehouseCapacityUtilisation(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets dock-to-stock time — average and median hours between material arrival and GRN generation.
    /// </summary>
    [HttpGet("warehouse-kpi/dock-to-stock")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<DockToStockTimeDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetDockToStockTime(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division,
        [FromQuery] DateFilter? datePreset,
        [FromQuery] DateTime? customStartDate,
        [FromQuery] DateTime? customEndDate
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division,
            DatePreset = datePreset,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        };
        var result = await repository.GetDockToStockTime(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets stock transfer fulfilment rate — percentage of transfers that have reached Issued status.
    /// </summary>
    [HttpGet("warehouse-kpi/stock-transfer-fulfilment")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<StockTransferFulfilmentRateDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStockTransferFulfilmentRate(
        [FromQuery] DateFilter? datePreset,
        [FromQuery] DateTime? customStartDate,
        [FromQuery] DateTime? customEndDate,
        [FromQuery] Guid? fromDepartmentId,
        [FromQuery] Guid? toDepartmentId
    )
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var filter = new WarehouseKpiFilterDto
        {
            DatePreset = datePreset,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate,
            FromDepartmentId = fromDepartmentId,
            ToDepartmentId = toDepartmentId
        };
        var result = await repository.GetStockTransferFulfilmentRate(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets receiving pipeline snapshot — count of inbound materials grouped by processing stage.
    /// </summary>
    [HttpGet("warehouse-kpi/receiving-pipeline")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<ReceivingPipelineSnapshotDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetReceivingPipeline(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division
        };
        var result = await repository.GetReceivingPipelineSnapshot(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets expiry risk index — material batches grouped by days until expiry.
    /// </summary>
    [HttpGet("warehouse-kpi/expiry-risk-index")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<ExpiryRiskIndexDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetExpiryRiskIndex(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division,
        [FromQuery] ExpiryWindowFilter? expiryWindow
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division,
            ExpiryWindow = expiryWindow
        };
        var result = await repository.GetExpiryRiskIndex(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets reorder alert count — materials whose stock is at or below reorder level.
    /// </summary>
    [HttpGet("warehouse-kpi/reorder-alert-count")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<ReorderAlertCountDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetReorderAlertCount(
        [FromQuery] MaterialKind? materialKind
    )
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (
            string.IsNullOrWhiteSpace(departmentIdStr)
            || !Guid.TryParse(departmentIdStr, out var departmentId)
        )
            return TypedResults.Unauthorized();

        var filter = new WarehouseKpiFilterDto
        {
            DepartmentId = departmentId,
            MaterialKind = materialKind
        };
        var result = await repository.GetReorderAlertCount(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets swap request activity — count of swap requests by approval status for a period.
    /// </summary>
    [HttpGet("warehouse-kpi/swap-request-activity")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<SwapRequestActivityDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetSwapRequestActivity(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division,
        [FromQuery] DateFilter? datePreset,
        [FromQuery] DateTime? customStartDate,
        [FromQuery] DateTime? customEndDate
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division,
            DatePreset = datePreset,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        };
        var result = await repository.GetSwapRequestActivity(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse-kpi/material-movement-count")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<MaterialMovementCountDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialMovementCount(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division,
        [FromQuery] DateFilter? datePreset = null,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division,
            DatePreset = datePreset,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate
        };
        var result = await repository.GetMaterialMovementCount(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets source-owned warehouse analytics freshness watermarks and source row counts.
    /// </summary>
    [HttpGet("warehouse-kpi/freshness")]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(IEnumerable<WarehouseDataFreshnessDto>)
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetWarehouseKpiFreshness(
        [FromQuery] Guid? warehouseId,
        [FromQuery] WarehouseType? warehouseType,
        [FromQuery] Division? division
    )
    {
        if (!TryGetAuthenticatedDepartment(out var departmentId))
            return TypedResults.Unauthorized();
        var filter = new WarehouseKpiFilterDto
        {
            WarehouseId = warehouseId,
            WarehouseType = warehouseType,
            Division = division
        };
        var result = await repository.GetWarehouseKpiFreshness(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 3 - BMR Release Rate.
    /// </summary>
    [HttpGet("kpi/bmr-release-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BmrReleaseRateDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetBmrReleaseRate([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetBmrReleaseRate(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 4 - Yield Performance.
    /// </summary>
    [HttpGet("kpi/yield-performance")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<YieldPerformanceDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetYieldPerformance([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetYieldPerformance(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 6 - Schedule Adherence (On-Time Completion Rate).
    /// </summary>
    [HttpGet("kpi/schedule-adherence")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ScheduleAdherenceDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetScheduleAdherence([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetScheduleAdherence(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 7 - Production Output Volume.
    /// </summary>
    [HttpGet("kpi/production-output-volume")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionOutputVolumeDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetProductionOutputVolume([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetProductionOutputVolume(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 8 - ATR Testing Backlog.
    /// </summary>
    [HttpGet("kpi/atr-testing-backlog")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AtrTestingBacklogDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetAtrTestingBacklog([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetAtrTestingBacklog(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 9 - Stock Requisition Pending for Production.
    /// </summary>
    [HttpGet("kpi/stock-requisition-pending")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockRequisitionPendingDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStockRequisitionPending([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetStockRequisitionPending(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 10 - FGTN Pending Approval.
    /// </summary>
    [HttpGet("kpi/fgtn-pending-approval")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FgtnPendingApprovalDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetFgtnPendingApproval([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetFgtnPendingApproval(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 11 - Production Order Delivery Status.
    /// </summary>
    [HttpGet("kpi/production-order-delivery-status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductionOrderDeliveryStatusDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetProductionOrderDeliveryStatus(
        [FromQuery] ProductionKpiFilter filter,
        [FromQuery] Guid? customerId = null
    )
    {
        var result = await repository.GetProductionOrderDeliveryStatus(filter, customerId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// KPI 12 - Material Return Rate.
    /// </summary>
    [HttpGet("kpi/material-return-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialReturnRateDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialReturnRate([FromQuery] ProductionKpiFilter filter)
    {
        var result = await repository.GetMaterialReturnRate(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/materials-stock-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialsStockSummaryDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialsStockSummary(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] MaterialKind? materialKind = null,
        [FromQuery] Guid? materialId = null)
    {
        var result = await repository.GetMaterialsStockSummary(departmentId, materialKind, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/materials-stock-batch-detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialsStockBatchDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialsStockBatchDetail(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] MaterialKind? materialKind = null,
        [FromQuery] string? batchNumber = null,
        [FromQuery] DateTime? expiryDateFrom = null,
        [FromQuery] DateTime? expiryDateTo = null)
    {
        var result = await repository.GetMaterialsStockBatchDetail(
            departmentId, materialKind, batchNumber, expiryDateFrom, expiryDateTo);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/shelf-utilisation-detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ShelfUtilisationDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetShelfUtilisationDetail(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] OccupancyStatus? occupancyStatus = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetShelfUtilisationDetail(warehouseId, locationId, occupancyStatus, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/goods-receiving-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GoodsReceivingRegisterDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetGoodsReceivingRegister(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? grnStatus = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetGoodsReceivingRegister(filter, grnStatus, supplierId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/receiving-performance-detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReceivingPerformanceDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetReceivingPerformanceDetail(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetReceivingPerformanceDetail(filter, warehouseId, supplierId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/putaway-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PutawayRegisterDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetPutawayRegister(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetPutawayRegister(filter, warehouseId, employeeId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/stock-adjustment-audit")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockAdjustmentAuditDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStockAdjustmentAudit(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? reasonCode = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetStockAdjustmentAudit(filter, reasonCode, warehouseId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/batch-traceability")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BatchTraceabilityDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetBatchTraceability(
        [FromQuery] Guid materialBatchId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? departmentId = null)
    {
        if (materialBatchId == Guid.Empty)
            return TypedResults.Problem(
                title: "Bad Request",
                detail: "MaterialBatchId is required.",
                statusCode: StatusCodes.Status400BadRequest);

        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetBatchTraceability(materialBatchId, filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/inter-warehouse-swap-requests")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InterWarehouseSwapRequestDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInterWarehouseSwapRequests(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetInterWarehouseSwapRequests(filter, warehouseId, status, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/stock-transfer-inter-department")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockTransferInterDepartmentDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStockTransferInterDepartmentDetail(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? fromDepartmentId = null,
        [FromQuery] Guid? toDepartmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetStockTransferInterDepartmentDetail(
            filter, fromDepartmentId, toDepartmentId, status, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/material-expiry-projection")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialExpiryProjectionDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMaterialExpiryProjection(
        [FromQuery] ExpiryWindowFilter? window = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetMaterialExpiryProjection(window, warehouseId, materialId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/slow-moving-inventory")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SlowMovingInventoryDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetSlowMovingInventory(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] InactivityThreshold threshold = InactivityThreshold.Days90)
    {
        var result = await repository.GetSlowMovingInventory(threshold, warehouseId, materialId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/bin-card-transaction-ledger")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BinCardTransactionLedgerDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetBinCardTransactionLedger(
        [FromQuery] Guid materialBatchId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? departmentId = null)
    {
        if (materialBatchId == Guid.Empty)
            return TypedResults.Problem(
                title: "Bad Request",
                detail: "MaterialBatchId is required.",
                statusCode: StatusCodes.Status400BadRequest);

        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetBinCardTransactionLedger(materialBatchId, filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/operations-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OperationsSummaryDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetOperationsSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var filter = new ReportFilter { StartDate = startDate, EndDate = endDate };
        var result = await repository.GetOperationsSummary(filter, warehouseId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/qc-pending")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<QcPendingDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetQcPendingReport(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? supplierId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetQcPendingReport(warehouseId, supplierId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/reorder-level-vs-stock")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReorderLevelVsStockDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetReorderLevelVsStock(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] MaterialKind? materialKind = null)
    {
        var result = await repository.GetReorderLevelVsStock(departmentId, materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/inventory-valuation-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InventoryValuationSummaryDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInventoryValuationSummary(
        [FromQuery] WarehouseType? warehouseType = null,
        [FromQuery] Division? division = null,
        [FromQuery] UnitOfMeasureCategory? uomGroup = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetInventoryValuationSummary(warehouseType, division, uomGroup, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/inventory-valuation-detail")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InventoryValuationDetailDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInventoryValuationDetail(
        [FromQuery] WarehouseType? warehouseType = null,
        [FromQuery] Division? division = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? valuationType = null,
        [FromQuery] Guid? materialId = null,
        [FromQuery] DateTime? expiryDateFrom = null,
        [FromQuery] DateTime? expiryDateTo = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetInventoryValuationDetail(
            warehouseType, division, warehouseId, valuationType, materialId, expiryDateFrom, expiryDateTo, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/employee-activity")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<WarehouseEmployeeActivityDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetWarehouseEmployeeActivity(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetWarehouseEmployeeActivity(filter, employeeId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("warehouse/arrival-location-status")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ArrivalLocationStatusDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetArrivalLocationStatus(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? agingThresholdDays = null,
        [FromQuery] Guid? departmentId = null)
    {
        var result = await repository.GetArrivalLocationStatus(warehouseId, status, agingThresholdDays ?? 3, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/employee-headcount")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeHeadcountSnapshotDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeHeadcount(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeType? employeeType,
        [FromQuery] EmployeeStatus? status)
    {
        var filter = new HrKpiFilterDto
        {
            EmployeeType = employeeType,
            Status = status,
        };
        var result = await repository.GetEmployeeHeadcountSnapshot(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/employee-gender-ratio")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeGenderRatioDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeGenderRatio(
        [FromQuery] Guid? departmentId)
    {
        var filter = new HrKpiFilterDto();
        var result = await repository.GetEmployeeGenderRatio(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/leave-request-pipeline")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveRequestPipelineDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetLeaveRequestPipeline(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetLeaveRequestPipeline(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/overtime-request-activity")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OvertimeRequestActivityDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetOvertimeRequestActivity(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetOvertimeRequestActivity(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/daily-attendance-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DailyAttendanceRateDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetDailyAttendanceRate(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? date)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = date,
        };
        var result = await repository.GetDailyAttendanceRate(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/staff-requisition-pipeline")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StaffRequisitionPipelineDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStaffRequisitionPipeline(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetStaffRequisitionPipeline(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/employee-grade-level-distribution")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeGradeLevelDistributionDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeGradeLevelDistribution(
        [FromQuery] Guid? departmentId)
    {
        var result = await repository.GetEmployeeGradeLevelDistribution(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/new-hires-this-period")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NewHiresThisPeriodDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetNewHiresThisPeriod(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetNewHiresThisPeriod(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/employee-turnover-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmployeeTurnoverRateDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeTurnoverRate(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new HrKpiFilterDto
        {
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetEmployeeTurnoverRate(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/leave-utilisation-rate")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveUtilisationRateDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetLeaveUtilisationRate(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? year)
    {
        var filter = new HrKpiFilterDto
        {
            Year = year,
        };
        var result = await repository.GetLeaveUtilisationRate(filter, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr-kpi/active-disciplinary-actions")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ActiveDisciplinaryActionsDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetActiveDisciplinaryActions(
        [FromQuery] Guid? departmentId)
    {
        var result = await repository.GetActiveDisciplinaryActions(departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value.AsEnumerable()) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-master-list")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeMasterListReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeMasterList(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeType? employeeType,
        [FromQuery] EmployeeLevel? gradeLevel,
        [FromQuery] EmployeeStatus? status)
    {
        var filter = new EmployeeMasterListFilter
        {
            DepartmentId = departmentId,
            EmployeeType = employeeType,
            GradeLevel = gradeLevel,
            Status = status,
        };
        var result = await repository.GetEmployeeMasterList(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-directory-by-department")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeDirectoryByDepartmentDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeDirectoryByDepartment(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeLevel? gradeLevel,
        [FromQuery] EmployeeType? employeeType)
    {
        var filter = new EmployeeDirectoryFilter
        {
            DepartmentId = departmentId,
            GradeLevel = gradeLevel,
            EmployeeType = employeeType,
        };
        var result = await repository.GetEmployeeDirectoryByDepartment(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-demographics")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeDemographicsReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeDemographics(
        [FromQuery] Guid? departmentId)
    {
        var filter = new EmployeeDemographicsFilter
        {
            DepartmentId = departmentId,
        };
        var result = await repository.GetEmployeeDemographics(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/staff-grade-level")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StaffGradeLevelReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStaffGradeLevel(
        [FromQuery] Guid? departmentId)
    {
        var filter = new StaffGradeLevelFilter
        {
            DepartmentId = departmentId,
        };
        var result = await repository.GetStaffGradeLevel(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/leave-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<LeaveRegisterReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetLeaveRegister(
        [FromQuery] Guid? departmentId,
        [FromQuery] RequestCategory? leaveCategory,
        [FromQuery] LeaveStatus? status,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new LeaveRegisterFilter
        {
            DepartmentId = departmentId,
            LeaveCategory = leaveCategory,
            Status = status,
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetLeaveRegister(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/leave-balance")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<LeaveBalanceReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetLeaveBalance(
        [FromQuery] Guid? departmentId,
        [FromQuery] int? leaveYear,
        [FromQuery] Guid? employeeId)
    {
        var filter = new LeaveBalanceFilter
        {
            DepartmentId = departmentId,
            LeaveYear = leaveYear,
            EmployeeId = employeeId,
        };
        var result = await repository.GetLeaveBalance(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/leave-approval-audit")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<LeaveApprovalAuditReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetLeaveApprovalAudit(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] LeaveStatus? status)
    {
        var filter = new LeaveApprovalAuditFilter
        {
            DepartmentId = departmentId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
        };
        var result = await repository.GetLeaveApprovalAudit(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/overtime-request-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<OvertimeRequestRegisterReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetOvertimeRequestRegister(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] OvertimeStatus? status)
    {
        var filter = new OvertimeRequestRegisterFilter
        {
            DepartmentId = departmentId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
        };
        var result = await repository.GetOvertimeRequestRegister(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/staff-requisition-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StaffRequisitionRegisterReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetStaffRequisitionRegister(
        [FromQuery] Guid? departmentId,
        [FromQuery] StaffRequisitionStatus? status,
        [FromQuery] AppointmentType? appointmentType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new StaffRequisitionRegisterFilter
        {
            DepartmentId = departmentId,
            Status = status,
            AppointmentType = appointmentType,
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetStaffRequisitionRegister(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-disciplinary-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeDisciplinaryReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeDisciplinaryReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeActiveStatus? disciplinaryStatus)
    {
        var filter = new EmployeeDisciplinaryFilter
        {
            DepartmentId = departmentId,
            DisciplinaryStatus = disciplinaryStatus,
        };
        var result = await repository.GetEmployeeDisciplinaryReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-exit-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeExitReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeExitReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmployeeInactiveStatus? exitReason,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new EmployeeExitFilter
        {
            DepartmentId = departmentId,
            ExitReason = exitReason,
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetEmployeeExitReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/employee-anniversary-birthday")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<EmployeeAnniversaryBirthdayReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetEmployeeAnniversaryBirthdayReport(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var filter = new EmployeeAnniversaryBirthdayFilter
        {
            DepartmentId = departmentId,
            EventType = eventType,
            StartDate = startDate,
            EndDate = endDate,
        };
        var result = await repository.GetEmployeeAnniversaryBirthdayReport(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("hr/shift-schedule-register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ShiftScheduleRegisterReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetShiftScheduleRegister(
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] ScheduleStatus? status)
    {
        var filter = new ShiftScheduleRegisterFilter
        {
            DepartmentId = departmentId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
        };
        var result = await repository.GetShiftScheduleRegister(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
