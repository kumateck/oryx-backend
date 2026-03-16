using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Reports.FinishedGoodsTransferNotes;
using DOMAIN.Entities.Reports.HumanResource;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Reports.Procurement;
using APP.Utils;

using DOMAIN.Entities.Reports.GeneralInventory;
using DOMAIN.Entities.Reports.PurchaseOrder;
using DOMAIN.Entities.Reports.Services;
using DOMAIN.Entities.Reports.Shipments;
using DOMAIN.Entities.Reports.Warehouse;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/report")]
[ApiController]
[Authorize]
public class ReportController(IReportRepository repository) : ControllerBase
{
    /// <summary>
    /// Gets the production report for a specific department.
    /// </summary>
    [HttpGet("production")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionReportDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionReport([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

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
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

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
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

        var result = await repository.GetWarehouseReport(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the logistics reporting dashboard
    /// </summary>
    [HttpGet("logistics")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseReportDto))]
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<MaterialBatchReservedQuantityReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetReservedMaterialBatches([FromQuery] ReportFilter filter)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

        var result = await repository.GetReservedMaterialBatchesForDepartment(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets the human resource report
    /// </summary>
    [HttpGet("human-resource")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HrDashboardDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetHumanResourceReport([FromQuery] MovementReportFilter filter,
        [FromQuery] Guid? designationId, [FromQuery] EmployeeType? employeeType,
        [FromQuery] Gender? gender)
    {
        var result = await repository.GetHumanResourceDashboardReport(filter, designationId, employeeType, gender);
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DistributedRequisitionMaterialDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMaterialsReadyForChecklist([FromQuery] ReportFilter filter)
    {
        var userId = (string)HttpContext.Items["User"];
        if (userId == null) return TypedResults.Unauthorized();

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
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsReadyForAssignment(filter, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("qa-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QaDashboardDto))]
    public async Task<IResult> GetQaDashboard([FromQuery] ReportFilter filter, [FromQuery] Guid? productId)
    {
        var result = await repository.GetQaDashboardReport(filter, productId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("qc-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(QaDashboardDto))]
    public async Task<IResult> GetQcDashboard([FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId, [FromQuery] Guid? materialId)
    {
        var result = await repository.GetQcDashboardReport(filter, productId, materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    [HttpGet("finished-goods-transfer-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FinishedGoodsTransferSummaryReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferSummaryReport(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null)
    {
        var result = await repository.GetFinishedGoodsTransferSummaryReport(filter, productId, warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("finished-goods-transfer-detailed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<FinishedGoodsTransferDetailedReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFinishedGoodsTransferDetailedReport(
        [FromQuery] ReportFilter filter,
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null)
    {
        var result = await repository.GetFinishedGoodsTransferDetailedReport(filter, productId, warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the product stock summary report.
    /// </summary>
    [HttpGet("product-stock-summary")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(List<ProductStockSummaryReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductStockSummaryReport(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? departmentId)
    {
        var result = await repository.GetProductStockSummaryReport(productId, warehouseId, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves the detailed product stock report with batch and expiry information.
    /// </summary>
    [HttpGet("product-stock-detailed")]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(List<ProductStockDetailedReportDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductStockDetailedReport(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] string? batchNumber = null,
        [FromQuery] DateTime? expiryDateFrom = null,
        [FromQuery] DateTime? expiryDateTo = null)
    {
        var result = await repository.GetProductStockDetailedReport(productId, warehouseId, departmentId, batchNumber, expiryDateFrom, expiryDateTo);

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
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
/// <summary>
/// Retrieves a report of supplier materials based on filters.
/// </summary>
[HttpGet("supplier-materials")]
[AllowAnonymous]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierMaterialReportDto))]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IResult> GetSupplierMaterialReport([FromQuery] SupplierMaterialFilters filters
        )
    {
        var result = await repository.GetSupplierMaterialAReport(filters );

    return result.IsSuccess
        ? TypedResults.Ok(result.Value)
        : result.ToProblemDetails();
}
    
    /// <summary>
    /// Retrieves a paginated list of items
    /// </summary>
    [HttpGet("items-per-store-type")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ItemDto>))]
    public async Task<IResult> GetItemsPerStoreType([FromQuery] Store? store, [FromQuery] InventoryClassification? inventoryClassification,
        [FromQuery] Guid? itemId, [FromQuery] Guid? categoryId)
    {
        var result = await repository.GetItemsPerStoreType(store, inventoryClassification, itemId, categoryId);
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VendorStoreItemStockSummaryDto>))]
    public async Task<IResult> GetVendorItemMapping([FromQuery] Store? store, [FromQuery] Guid? vendorId,
        [FromQuery] Guid? itemId, [FromQuery] Guid? categoryId, [FromQuery] InventoryClassification? classification)
    {
        var result = await repository.GetVendorItemMapping(store,vendorId,itemId,categoryId,classification);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Provides a stock quantity overview per store type, showing total item quantities.
    /// </summary>
    [HttpGet("vendor-item/summary")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VendorStoreItemStockSummaryDto>))]
    public async Task<IResult> GetVendorItemMappingSummary([FromQuery] Guid? itemId, [FromQuery] Guid? categoryId,
        [FromQuery] InventoryClassification? classification, [FromQuery] Store? store)
    {
        var result = await repository.GetVendorItemMappingPerStoreTypeSummary(itemId, categoryId, classification, store);
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
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
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
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves the purchased PO report based on the specified filter.
    /// </summary>
    [HttpGet("purchased-po-report")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PurchasedPoReportDto>))]
   
    public async Task<IResult> GetPurchasedPoReport([FromQuery] PurchaseOrderFilter filter)
    {
        var result = await repository.GetPurchasedPoReportAsync(filter);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
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

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets the procurement dashboard KPIs (requisitions, POs, quotations, distributions)
    /// </summary>
    [HttpGet("procurement-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProcurementDashboardDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetProcurementDashboard([FromQuery] DateFilter filter = DateFilter.AllTime)
    {
        var result = await repository.GetProcurementDashboard(filter);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    
    [HttpGet("warehouse-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseDashboardReportDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetWarehouseDashboard([FromQuery] DateFilter filter = DateFilter.AllTime)
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (string.IsNullOrWhiteSpace(departmentIdStr) || !Guid.TryParse(departmentIdStr, out var departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetWarehouseDashboard(departmentId,filter);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
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
        if (string.IsNullOrWhiteSpace(departmentIdStr) || !Guid.TryParse(departmentIdStr, out var departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetExpiredMaterials(departmentId);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets currently reserved material quantities for production in the department
    /// </summary>
    [HttpGet("reserved-materials")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ReservedMaterialReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetReservedMaterials()
    {
        var departmentIdStr = (string?)HttpContext.Items["Department"];
        if (string.IsNullOrWhiteSpace(departmentIdStr) || !Guid.TryParse(departmentIdStr, out var departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetReservedMaterials(departmentId);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
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
        if (string.IsNullOrWhiteSpace(departmentIdStr) || !Guid.TryParse(departmentIdStr, out var departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsBelowReorderLevel(departmentId);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
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
        if (string.IsNullOrWhiteSpace(departmentIdStr) || !Guid.TryParse(departmentIdStr, out var departmentId))
            return TypedResults.Unauthorized();

        var result = await repository.GetMaterialsChecklist(departmentId);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    /// <summary>
    /// Gets current status distribution of all shipments (New, At Port, Cleared, In Transit, Arrived)
    /// </summary>
    [HttpGet("shipment-status-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShipmentStatusReportDto))]
    public async Task<IResult> GetShipmentStatusSummary([FromQuery] DateFilter filter = DateFilter.AllTime)
    {
        var result = await repository.GetShipmentStatusReport(filter);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets general inventory dashboard summary
    /// </summary>
    [HttpGet("general-inventory-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GeneralInventoryDashboardDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetGeneralInventoryDashboard([FromQuery] DateFilter filter = DateFilter.AllTime)
    {
        var result = await repository.GetGeneralInventoryDashboard(filter);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
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
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets services dashboard summary
    /// </summary>
    [HttpGet("services-dashboard")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServicesDashboardReportDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetServicesDashboard([FromQuery] DateFilter filter = DateFilter.AllTime)
    {
        var result = await repository.GetServicesDashboard(filter);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Gets summary of invoiced products allocated to customers
    /// </summary>
    [HttpGet("invoiced-products-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InvoicedProductsSummaryReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInvoicedProductsSummary([FromQuery] InvoicedProductFilters filters)
    {
        var result = await repository.GetInvoicedProductsSummary(filters);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets detailed list of invoiced products with allocation and batch information
    /// </summary>
    [HttpGet("invoiced-products-detailed")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<InvoicedProductsDetailedReportDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetInvoicedProductsDetailed([FromQuery] InvoicedProductFilters filters)
    {
        var result = await repository.GetInvoicedProductsDetailedReport(filters);
        return result.IsSuccess 
            ? TypedResults.Ok(result.Value) 
            : result.ToProblemDetails();
    }
}