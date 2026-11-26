using System.Text.RegularExpressions;
using DOMAIN.Entities.Permissions;

namespace APP.Utils;

public static class PermissionModules
{
    public const string Procurement = "Procurement";
    public const string Logistics = "Logistics";
    public const string Warehouse = "Warehouse";
    public const string Production = "Production";
    public const string QualityControl = "Quality Control";
    public const string QualityAssurance = "Quality Assurance";
    public const string FinishedGoodsWarehouse = "Finished Goods Warehouse";
    public const string Stores = "STORES";
    public const string ServiceAndStoreProcurement = "Service and Store Procurement";
    public const string HumanResources = "Human Resources";
    public const string ItSupport = "IT Support";
    public const string Settings = "Settings";
    public const string InventoryManagement = "Inventory Management";
    public const string OrganizationalStructure = "Organizational Structure";
    public const string Reports = "Reports";
    public const string Finance = "Finance";
    public const string Audit = "Audit";
    public const string ResearchAndDevelopment = "Research & Development";
}

public static class PermissionSubmodules
{
    // Procurement
    public const string PurchaseRequisition = "Purchase Requisition";
    public const string QuotationsRequest = "Quotations Request";
    public const string QuotationsResponses = "Quotations Responses";
    public const string PriceComparison = "Price Comparison";
    public const string ProformaRequest = "Proforma Request";
    public const string ProformaResponses = "Proforma Responses";
    public const string CreatePurchaseOrdersPage = "Create Purchase Orders Page";
    public const string CheckPurchaseOrderPage = "Check Purchase Order Page";
    public const string ApprovePurchaseOrderPage = "Approve Purchase Order Page";
    public const string PurchaseOrderListPage = "Purchase Order List Page";
    public const string MaterialDistribution = "Material Distribution";

    // Logistics
    public const string ShipmentInvoice = "Shipment Invoice";
    public const string ShipmentDocument = "Shipment Document";
    public const string BillingSheet = "Billing Sheet";
    public const string Waybill = "Waybill";
    public const string AvailableStock = "Available Stock";
    public const string Payments = "Payments";
    public const string CashflowReports = "Cashflow Reports";
    public const string Costing = "Costing";

    // Warehouse
    public const string ReceivingArea = "Receiving Area";
    public const string QuarantineAreaGrn = "Quarantine Area / GRN";
    public const string DistributedArea = "Distributed Area";
    public const string ApprovedMaterials = "Approved Materials";
    public const string LinkedMaterials = "Linked Materials";
    public const string UnlinkedMaterials = "Unlinked Materials";
    public const string Materials = "Materials";
    public const string RejectedMaterials = "Rejected Materials";
    public const string IssueStockRequisitions = "Issue Stock Requisitions";
    public const string ExtraPackagingRequisitions = "Extra Packaging Requisitions";
    public const string MaterialReturnNote = "Material Return Note";
    public const string ExpiredMaterials = "Expired Materials";
    public const string MaterialLocation = "Material Location";
    public const string StockAdjustment = "Stock Adjustment";
    public const string StockTransferIssues = "Stock Transfer Issues";
    public const string StockTransferredAllocated = "Stock Transferred Allocated";

    // Production
    public const string Requisitions = "REQUISITIONS";
    public const string CreatePurchaseRequisitions = "Create Purchase Requisitions";
    public const string Products = "Products";
    public const string ProductSchedule = "Product Schedule";
    public const string StockTransferRequests = "Stock Transfer Requests";
    public const string WhitePlacebo = "White Placebo";

    // Quality Control
    public const string GoodsReceiptNote = "Goods Receipt Note (GRN)";
    public const string AnalyticalTestRequestProducts = "Analytical Test Request (Products)";
    public const string Testing = "Testing";
    public const string MaterialStp = "Material STP";
    public const string MaterialSpecification = "Material Specification";
    public const string MaterialArd = "Material ARD";
    public const string ProductStp = "Product STP";
    public const string ProductSpecification = "Product Specification";
    public const string ProductArd = "Product ARD";
    public const string RoutineTests = "Routine Tests";
    public const string RoutineSetup = "Routine Setup";
    public const string MicrobialRequirements = "Microbial Requirements";
    public const string WaterQuality = "Water Quality";
    public const string QualityCertificates = "Quality Certificates";

    // Quality Assurance
    public const string IssueBmr = "Issue BMR";
    public const string AnalyticalTestRequests = "Analytical Test Requests";
    public const string PendingApprovals = "Pending Approvals";
    public const string OosInvestigations = "OOS Investigations";
    public const string Verification = "Verification";
    public const string TransferApprovalRights = "Transfer Approval Rights";

    // Audit
    public const string QualityAudits = "Quality Audits";

    // Research & Development
    public const string RndProjects = "R&D Projects";
    public const string RndFormulations = "R&D Formulations";
    public const string RndFormulaBuilder = "R&D Formula Builder";
    public const string RndTrialBatches = "R&D Trial Batches";
    public const string RndAnalyticalMethods = "R&D Analytical Methods";
    public const string RndStabilityStudies = "R&D Stability Studies";
    public const string RndTechnologyTransfers = "R&D Technology Transfers";

    // Finished Goods Warehouse
    public const string CustomerManagement = "Customer Management";
    public const string ProductionOrders = "Production Orders";
    public const string PackingList = "Packing List";
    public const string ProformaInvoice = "Proforma Invoice";
    public const string Invoice = "Invoice";
    public const string Allocations = "Allocations";
    public const string FinishedGoodsTransferNote = "Finished Goods Transfer Note";
    public const string ApprovedProducts = "Approved Products";

    // STORES
    public const string GeneralInventory = "General Inventory";
    public const string StoresReceivingArea = "Receiving Area";
    public const string StoresAvailableStocks = "Available Stocks";
    public const string StoresPurchaseRequisition = "Purchase Requisition";
    public const string StoresStockRequisition = "Stock Requisition";
    public const string StoresIssueStockRequisition = "Issue Stock Requisition";
    public const string StoresReturnMaterials = "Return Materials";
    public const string DamageMissingItems = "Damage / Missing Items";
    public const string VendorManagement = "Vendor Management";

    // Service and Store Procurement
    public const string PurchaseRequisitionSourcing = "Purchase Requisition Sourcing";
    public const string ServiceQuotationRequests = "Quotation Requests";
    public const string ServiceQuotationResponses = "Quotation Responses";
    public const string OpenMarketQuotationSheet = "Open Market Quotation Sheet";
    public const string ServicePriceComparison = "Price Comparison";
    public const string ServiceProformaRequests = "Proforma Requests";
    public const string ServiceProformaResponses = "Proforma Responses";
    public const string Memos = "Memos";
    public const string ServicePurchaseOrders = "Purchase Orders";
    public const string ServiceManagement = "Service Management";
    public const string JobRequests = "Job Requests";
    public const string JobOrders = "Job Orders";

    // Human Resources
    public const string EmployeeManagement = "Employee Management";
    public const string DepartmentEmployeeExport = "Department Employee Export";
    public const string DesignationManagement = "Designation Management";
    public const string StaffRequisition = "Staff Requisition";
    public const string LeaveManagement = "Leave Management";
    public const string LeaveTypeConfiguration = "Leave Type Configuration";
    public const string AttendanceReportUpload = "Attendance Report Upload";
    public const string ShiftScheduleReportUpload = "Shift Schedule Report Upload";
    public const string OvertimeManagement = "Overtime Management";
    public const string EmployeeCompensation = "Employee Compensation";
    public const string PayrollConfiguration = "Payroll Configuration";
    public const string PayrollManagement = "Payroll Management";
    public const string PerformanceCycleManagement = "Performance Cycle Management";
    public const string GoalManagement = "Goal Management";
    public const string PerformanceReviewManagement = "Performance Review Management";

    // IT SUPPORT
    public const string UserManagement = "Manage User Directory";
    public const string AuditTrail = "Audit Trail";
    public const string ManageRoles = "Manage Roles";
    public const string ManagePermissions = "Manage Permissions";
    public const string SupportTickets = "Support Tickets";

    // Settings
    public const string GeneralSettings = "General Settings";
    public const string ProductsCategory = "Product Category";
    public const string Procedures = "Procedures";
    public const string Site = "Site";
    public const string CountryAddress = "Address";
    public const string TermsOfPayment = "Terms of Payment";
    public const string DeliveryMode = "Delivery Mode";
    public const string Charges = "Charges";
    public const string Threshold = "Threshold";
    public const string CodeSettings = "CODE SETTINGS";
    public const string WorkflowBuilder = "Workflow Builder";
    public const string AlertsNotifications = "Alerts & Notifications";
    public const string Approvals = "Approvals";
    public const string DocumentReviews = "Document Reviews";
    public const string PendingReviews = "Pending Reviews";
    public const string RequisitionReviews = "Requisition Reviews";
    public const string SignatureSettings = "Signature Settings";
    public const string ChangePassword = "Change Password";

    // Inventory Management
    public const string Manufacturers = "Manufacturers";
    public const string Suppliers = "Suppliers";
    public const string Warehouses = "Warehouses";
    public const string Locations = "Locations";
    public const string Racks = "Racks";
    public const string Shelves = "Shelves";
    public const string Equipment = "Equipment";
    public const string QcEquipment = "QC Equipment";
    public const string InstrumentCalibration = "Instrument Calibration";
    public const string UnitOfMeasure = "Unit of Measure";

    // Organizational Structure
    public const string Departments = "Departments";
    public const string WorkingDays = "Working Days";
    public const string Holidays = "Holidays";
    public const string ShiftsType = "Shifts Type";
    public const string ShiftsSchedule = "Shifts Schedule";
    public const string ShiftCategory = "Shift Category";
    public const string WorkingHoursPolicy = "Working Hours Policy";

    // Reports
    public const string Reports = "Reports";
    public const string SupplierReports = "Suppliers and Manufacturers Reports";
    public const string PurchasingReports = "Purchasing and Shipment Reports";
    public const string MaterialReports = "Material Reports";
    public const string ProductionReports = "Production and Batch Record Reports";
    public const string ProductReports = "Product and Finished Goods Reports";
    public const string GeneralStoreReports = "General Store Reports";
    public const string PeopleReports = "People Reports";
}

/// <summary>Lists of report keys that several endpoints check together.</summary>
public static class ReportPermissions
{
    /// <summary>One key per report in the reports hub. Filter lookups that every report uses accept any of them.</summary>
    public static readonly IReadOnlyList<string> Hub =
    [
        PermissionKeys.CanViewApprovedManufacturerListReport,
        PermissionKeys.CanViewSupplierMappingSummaryReport,
        PermissionKeys.CanViewSupplierMappingDetailReport,
        PermissionKeys.CanViewPurchaseOrdersReport,
        PermissionKeys.CanViewInvoicedPurchaseOrdersReport,
        PermissionKeys.CanViewShipmentsReport,
        PermissionKeys.CanViewMaterialListReport,
        PermissionKeys.CanViewMaterialsPerDepartmentReport,
        PermissionKeys.CanViewMaterialStockSummaryReport,
        PermissionKeys.CanViewMaterialStockDetailReport,
        PermissionKeys.CanViewStockCountSheetReport,
        PermissionKeys.CanViewExpiredStockReport,
        PermissionKeys.CanViewInventoryValuationReport,
        PermissionKeys.CanViewMonthlyProductionScheduleReport,
        PermissionKeys.CanViewDailyProductionScheduleReport,
        PermissionKeys.CanViewProductReadinessReport,
        PermissionKeys.CanViewBmrIssuedReport,
        PermissionKeys.CanViewBmrIssuedByProductReport,
        PermissionKeys.CanViewStockIssuedReport,
        PermissionKeys.CanViewProductionMonthlyReport,
        PermissionKeys.CanViewProductionScheduleSummaryReport,
        PermissionKeys.CanViewProductionBatchValueReport,
        PermissionKeys.CanViewProductListReport,
        PermissionKeys.CanViewDepartmentProductListReport,
        PermissionKeys.CanViewProductStockSummaryReport,
        PermissionKeys.CanViewProductStockDetailReport,
        PermissionKeys.CanViewFinishedGoodsTransferSummaryReport,
        PermissionKeys.CanViewFinishedGoodsTransferDetailReport,
        PermissionKeys.CanViewInvoicedProductsReport,
        PermissionKeys.CanViewStoreItemsReport,
        PermissionKeys.CanViewStoreStockReport,
        PermissionKeys.CanViewVendorItemMappingSummaryReport,
        PermissionKeys.CanViewVendorItemMappingDetailReport,
        PermissionKeys.CanViewEmployeeMasterListReport,
        PermissionKeys.CanViewStaffGradeReport,
        PermissionKeys.CanViewStaffTotalReport,
        PermissionKeys.CanViewStaffGenderRatioReport,
        PermissionKeys.CanViewNewHiresAndExitsReport,
        PermissionKeys.CanViewStaffTurnoverReport,
        PermissionKeys.CanViewLeaveOverviewReport,
        PermissionKeys.CanViewDailyAttendanceReport,
        PermissionKeys.CanViewDepartmentAttendanceReport,
    ];
}

public static class PermissionKeys
{
    /// <summary>Correct the manufacture date, expiry date, and GRN reference of migrated stock.</summary>
    public const string CanUpdateMigrationBatchDetails = "CanUpdateMigrationBatchDetails";
    public const string CanUpdateMigrationRetestDate = "CanUpdateMigrationRetestDate";
    // Warehouse and sourcing tasks no page offers yet; granted to nobody (permissions audit phase 4).
    /// <summary>Create an arrival location.</summary>
    public const string CanCreateArrivalLocation = "CanCreateArrivalLocation";
    /// <summary>Change an arrival location.</summary>
    public const string CanEditArrivalLocation = "CanEditArrivalLocation";
    /// <summary>Confirm that distributed material has arrived.</summary>
    public const string CanConfirmMaterialArrival = "CanConfirmMaterialArrival";
    /// <summary>Create a material batch by hand.</summary>
    public const string CanCreateMaterialBatch = "CanCreateMaterialBatch";
    /// <summary>Import material batches from a file.</summary>
    public const string CanImportMaterialBatches = "CanImportMaterialBatches";
    /// <summary>Move a material batch between locations (legacy move).</summary>
    public const string CanMoveMaterialBatch = "CanMoveMaterialBatch";
    /// <summary>Consume material at a location.</summary>
    public const string CanConsumeMaterialAtLocation = "CanConsumeMaterialAtLocation";
    /// <summary>Change a material batch status.</summary>
    public const string CanChangeMaterialBatchStatus = "CanChangeMaterialBatchStatus";
    /// <summary>Request a retest of a material batch.</summary>
    public const string CanRequestMaterialBatchRetest = "CanRequestMaterialBatchRetest";
    /// <summary>Complete a material batch retest.</summary>
    public const string CanCompleteMaterialBatchRetest = "CanCompleteMaterialBatchRetest";
    /// <summary>Run the stock inconsistency repair.</summary>
    public const string CanFixStockInconsistencies = "CanFixStockInconsistencies";
    /// <summary>Change a source requisition.</summary>
    public const string CanEditSourceRequisition = "CanEditSourceRequisition";
    /// <summary>Delete a source requisition.</summary>
    public const string CanDeleteSourceRequisition = "CanDeleteSourceRequisition";
    // QC/QA tasks no page offers yet; granted to nobody (permissions audit phase 4).
    /// <summary>Change an analytical test request.</summary>
    public const string CanEditAnalyticalTestRequest = "CanEditAnalyticalTestRequest";
    /// <summary>Change the status of an analytical test request.</summary>
    public const string CanChangeAnalyticalTestRequestStatus = "CanChangeAnalyticalTestRequestStatus";
    /// <summary>Delete an analytical test request.</summary>
    public const string CanDeleteAnalyticalTestRequest = "CanDeleteAnalyticalTestRequest";
    /// <summary>Create a uniformity-of-weight record.</summary>
    public const string CanCreateUniformityOfWeight = "CanCreateUniformityOfWeight";
    /// <summary>Change a uniformity-of-weight record.</summary>
    public const string CanEditUniformityOfWeight = "CanEditUniformityOfWeight";
    /// <summary>Delete a uniformity-of-weight record.</summary>
    public const string CanDeleteUniformityOfWeight = "CanDeleteUniformityOfWeight";
    /// <summary>Submit uniformity-of-weight results.</summary>
    public const string CanSubmitUniformityOfWeight = "CanSubmitUniformityOfWeight";
    // Production work orders: no page offers them yet; granted to nobody (permissions audit phase 4).
    /// <summary>Create a production work order.</summary>
    public const string CanCreateWorkOrder = "CanCreateWorkOrder";
    /// <summary>Change a production work order.</summary>
    public const string CanEditWorkOrder = "CanEditWorkOrder";
    /// <summary>Delete a production work order.</summary>
    public const string CanDeleteWorkOrder = "CanDeleteWorkOrder";
    // Reference lists, uploads and invoices no page offers yet; granted to nobody (permissions audit phase 4).
    /// <summary>Create, change or delete items in reference lists no settings page manages (currencies, package types, market types, instruments, reagents, warehouse location names, shipment discrepancy types).</summary>
    public const string CanManageOtherCollectionTypes = "CanManageOtherCollectionTypes";
    /// <summary>Import equipment from a file.</summary>
    public const string CanUploadEquipment = "CanUploadEquipment";
    /// <summary>Import product stock from a file.</summary>
    public const string CanUploadProductStock = "CanUploadProductStock";
    /// <summary>Create or change a purchase order invoice.</summary>
    public const string CanCreatePurchaseOrderInvoice = "CanCreatePurchaseOrderInvoice";
    /// <summary>Delete a purchase order invoice.</summary>
    public const string CanDeletePurchaseOrderInvoice = "CanDeletePurchaseOrderInvoice";
    // Store item GRNs: no page offers them yet; granted to nobody (permissions audit phase 4).
    /// <summary>Create a store item goods received note.</summary>
    public const string CanCreateItemGrn = "CanCreateItemGrn";
    /// <summary>Change a store item goods received note.</summary>
    public const string CanEditItemGrn = "CanEditItemGrn";
    /// <summary>Delete a store item goods received note.</summary>
    public const string CanDeleteItemGrn = "CanDeleteItemGrn";
    // Dashboards: one key each (permissions audit phase 4).
    /// <summary>View the HR dashboard.</summary>
    public const string CanViewHrDashboard = "CanViewHrDashboard";
    /// <summary>View the HR executive summary.</summary>
    public const string CanViewHrExecutiveSummary = "CanViewHrExecutiveSummary";
    /// <summary>View the production health dashboard.</summary>
    public const string CanViewProductionHealthDashboard = "CanViewProductionHealthDashboard";
    /// <summary>View the FGTN dashboard.</summary>
    public const string CanViewFgtnDashboard = "CanViewFgtnDashboard";
    /// <summary>View the warehouse dashboard.</summary>
    public const string CanViewWarehouseDashboard = "CanViewWarehouseDashboard";
    /// <summary>View the inventory command centre.</summary>
    public const string CanViewInventoryCommandCenter = "CanViewInventoryCommandCenter";
    /// <summary>View the logistics dashboard.</summary>
    public const string CanViewLogisticsDashboard = "CanViewLogisticsDashboard";
    /// <summary>View the procurement dashboard.</summary>
    public const string CanViewProcurementDashboard = "CanViewProcurementDashboard";
    /// <summary>View the QC dashboard.</summary>
    public const string CanViewQcDashboard = "CanViewQcDashboard";
    /// <summary>View the QA dashboard.</summary>
    public const string CanViewQaDashboard = "CanViewQaDashboard";
    /// <summary>View the services dashboard.</summary>
    public const string CanViewServicesDashboard = "CanViewServicesDashboard";
    /// <summary>View the stores dashboard.</summary>
    public const string CanViewStoresDashboard = "CanViewStoresDashboard";
    // Products: one key per task and section, per context (permissions audit phase 2).
    /// <summary>View the Production product list.</summary>
    public const string CanViewProductionProducts = "CanViewProductionProducts";
    /// <summary>Import products from a spreadsheet.</summary>
    public const string CanImportProductionProducts = "CanImportProductionProducts";
    /// <summary>Export the product list.</summary>
    public const string CanExportProductionProducts = "CanExportProductionProducts";
    /// <summary>View a product's general information.</summary>
    public const string CanViewProductionProductInfo = "CanViewProductionProductInfo";
    /// <summary>Create a product.</summary>
    public const string CanCreateProductionProduct = "CanCreateProductionProduct";
    /// <summary>Edit a product's general information.</summary>
    public const string CanEditProductionProductInfo = "CanEditProductionProductInfo";
    /// <summary>Delete a product.</summary>
    public const string CanDeleteProductionProduct = "CanDeleteProductionProduct";
    /// <summary>View a product's bill of materials.</summary>
    public const string CanViewProductionProductBom = "CanViewProductionProductBom";
    /// <summary>Add materials to a bill of materials.</summary>
    public const string CanAddProductionProductBom = "CanAddProductionProductBom";
    /// <summary>Edit bill of materials lines.</summary>
    public const string CanEditProductionProductBom = "CanEditProductionProductBom";
    /// <summary>Remove bill of materials lines or a bill of materials.</summary>
    public const string CanDeleteProductionProductBom = "CanDeleteProductionProductBom";
    /// <summary>Archive the active bill of materials.</summary>
    public const string CanArchiveProductionProductBom = "CanArchiveProductionProductBom";
    /// <summary>Import bills of materials.</summary>
    public const string CanImportProductionProductBom = "CanImportProductionProductBom";
    /// <summary>View a product's packing materials.</summary>
    public const string CanViewProductionProductPackingMaterials = "CanViewProductionProductPackingMaterials";
    /// <summary>Add packing materials.</summary>
    public const string CanAddProductionProductPackingMaterials = "CanAddProductionProductPackingMaterials";
    /// <summary>Edit packing materials.</summary>
    public const string CanEditProductionProductPackingMaterials = "CanEditProductionProductPackingMaterials";
    /// <summary>Remove packing materials.</summary>
    public const string CanDeleteProductionProductPackingMaterials = "CanDeleteProductionProductPackingMaterials";
    /// <summary>Import packing materials.</summary>
    public const string CanImportProductionProductPackingMaterials = "CanImportProductionProductPackingMaterials";
    /// <summary>View packing styles.</summary>
    public const string CanViewProductionProductPackingStyles = "CanViewProductionProductPackingStyles";
    /// <summary>Add packing styles.</summary>
    public const string CanAddProductionProductPackingStyles = "CanAddProductionProductPackingStyles";
    /// <summary>Edit packing styles.</summary>
    public const string CanEditProductionProductPackingStyles = "CanEditProductionProductPackingStyles";
    /// <summary>Remove packing styles.</summary>
    public const string CanDeleteProductionProductPackingStyles = "CanDeleteProductionProductPackingStyles";
    /// <summary>View the packing order.</summary>
    public const string CanViewProductionProductPackingOrder = "CanViewProductionProductPackingOrder";
    /// <summary>Edit the packing order.</summary>
    public const string CanEditProductionProductPackingOrder = "CanEditProductionProductPackingOrder";
    /// <summary>View the production procedure.</summary>
    public const string CanViewProductionProductProcedure = "CanViewProductionProductProcedure";
    /// <summary>Add procedure steps.</summary>
    public const string CanAddProductionProductProcedure = "CanAddProductionProductProcedure";
    /// <summary>Edit procedure steps.</summary>
    public const string CanEditProductionProductProcedure = "CanEditProductionProductProcedure";
    /// <summary>Remove procedure steps.</summary>
    public const string CanDeleteProductionProductProcedure = "CanDeleteProductionProductProcedure";
    /// <summary>View the product specification on the product.</summary>
    public const string CanViewProductionProductSpecifications = "CanViewProductionProductSpecifications";
    /// <summary>View the product STPs on the product.</summary>
    public const string CanViewProductionProductStps = "CanViewProductionProductStps";
    /// <summary>View the product ARDs on the product.</summary>
    public const string CanViewProductionProductArds = "CanViewProductionProductArds";
    /// <summary>Print a product.</summary>
    public const string CanPrintProductionProduct = "CanPrintProductionProduct";
    /// <summary>View the approved vendor (manufacturer) list and its revisions.</summary>
    public const string CanViewApprovedVendorList = "CanViewApprovedVendorList";
    /// <summary>Prepare the first approved vendor list.</summary>
    public const string CanCreateApprovedVendorList = "CanCreateApprovedVendorList";
    /// <summary>Save an approved vendor list draft's header and lines.</summary>
    public const string CanEditApprovedVendorListDraft = "CanEditApprovedVendorListDraft";
    /// <summary>Submit an approved vendor list draft: it becomes the fixed version and goes for review.</summary>
    public const string CanSubmitApprovedVendorList = "CanSubmitApprovedVendorList";
    /// <summary>Correct an approved vendor list while it is in review, and send a returned one back for review.</summary>
    public const string CanEditApprovedVendorList = "CanEditApprovedVendorList";
    /// <summary>Delete an approved vendor list revision in any status while retaining its history.</summary>
    public const string CanDeleteApprovedVendorList = "CanDeleteApprovedVendorList";
    /// <summary>Prepare a new revision of the current approved vendor list.</summary>
    public const string CanReviseApprovedVendorList = "CanReviseApprovedVendorList";
    /// <summary>Check an approved vendor list: confirm its review, which sends it for approval.</summary>
    public const string CanReviewApprovedVendorList = "CanReviewApprovedVendorList";
    /// <summary>Return an approved vendor list in review to its preparer for correction.</summary>
    public const string CanReturnApprovedVendorList = "CanReturnApprovedVendorList";
    /// <summary>Approve an approved vendor list at the caller's approval stage.</summary>
    public const string CanApproveApprovedVendorList = "CanApproveApprovedVendorList";
    /// <summary>Reject an approved vendor list at the caller's approval stage.</summary>
    public const string CanRejectApprovedVendorList = "CanRejectApprovedVendorList";
    /// <summary>Print an approved vendor list.</summary>
    public const string CanPrintApprovedVendorList = "CanPrintApprovedVendorList";
    /// <summary>See which suppliers and manufacturers the current approved vendor list allows for materials, on procurement screens.</summary>
    public const string CanViewApprovedVendorEligibility = "CanViewApprovedVendorEligibility";
    /// <summary>View the QA product list.</summary>
    public const string CanViewQaProducts = "CanViewQaProducts";
    /// <summary>View a product's general information in QA.</summary>
    public const string CanViewQaProductInfo = "CanViewQaProductInfo";
    /// <summary>Edit a product's general information in QA.</summary>
    public const string CanEditQaProductInfo = "CanEditQaProductInfo";
    /// <summary>View the bill of materials in QA.</summary>
    public const string CanViewQaProductBom = "CanViewQaProductBom";
    /// <summary>Add bill of materials lines in QA.</summary>
    public const string CanAddQaProductBom = "CanAddQaProductBom";
    /// <summary>Edit bill of materials lines in QA.</summary>
    public const string CanEditQaProductBom = "CanEditQaProductBom";
    /// <summary>Remove bill of materials lines in QA.</summary>
    public const string CanDeleteQaProductBom = "CanDeleteQaProductBom";
    /// <summary>Archive the active bill of materials in QA.</summary>
    public const string CanArchiveQaProductBom = "CanArchiveQaProductBom";
    /// <summary>View packing materials in QA.</summary>
    public const string CanViewQaProductPackingMaterials = "CanViewQaProductPackingMaterials";
    /// <summary>Add packing materials in QA.</summary>
    public const string CanAddQaProductPackingMaterials = "CanAddQaProductPackingMaterials";
    /// <summary>Edit packing materials in QA.</summary>
    public const string CanEditQaProductPackingMaterials = "CanEditQaProductPackingMaterials";
    /// <summary>Remove packing materials in QA.</summary>
    public const string CanDeleteQaProductPackingMaterials = "CanDeleteQaProductPackingMaterials";
    /// <summary>View packing styles in QA.</summary>
    public const string CanViewQaProductPackingStyles = "CanViewQaProductPackingStyles";
    /// <summary>Add packing styles in QA.</summary>
    public const string CanAddQaProductPackingStyles = "CanAddQaProductPackingStyles";
    /// <summary>Edit packing styles in QA.</summary>
    public const string CanEditQaProductPackingStyles = "CanEditQaProductPackingStyles";
    /// <summary>Remove packing styles in QA.</summary>
    public const string CanDeleteQaProductPackingStyles = "CanDeleteQaProductPackingStyles";
    /// <summary>View the packing order in QA.</summary>
    public const string CanViewQaProductPackingOrder = "CanViewQaProductPackingOrder";
    /// <summary>Edit the packing order in QA.</summary>
    public const string CanEditQaProductPackingOrder = "CanEditQaProductPackingOrder";
    /// <summary>View the procedure in QA.</summary>
    public const string CanViewQaProductProcedure = "CanViewQaProductProcedure";
    /// <summary>Add procedure steps in QA.</summary>
    public const string CanAddQaProductProcedure = "CanAddQaProductProcedure";
    /// <summary>Edit procedure steps in QA.</summary>
    public const string CanEditQaProductProcedure = "CanEditQaProductProcedure";
    /// <summary>Remove procedure steps in QA.</summary>
    public const string CanDeleteQaProductProcedure = "CanDeleteQaProductProcedure";
    /// <summary>View the product specification in QA.</summary>
    public const string CanViewQaProductSpecifications = "CanViewQaProductSpecifications";
    /// <summary>View the product ARDs in QA.</summary>
    public const string CanViewQaProductArds = "CanViewQaProductArds";
    /// <summary>View the product STPs in QA.</summary>
    public const string CanViewQaProductStps = "CanViewQaProductStps";
    /// <summary>View the finished goods product list.</summary>
    public const string CanViewFgProducts = "CanViewFgProducts";
    /// <summary>Release a material batch whose QC analysis is complete (PUT material/batch/{id}/approve).</summary>
    public const string CanApproveMaterialBatch = "CanApproveMaterialBatch";
    /// <summary>Ask for a product analytical test request to be retested.</summary>
    public const string CanRequestRetestProductTest = "CanRequestRetestProductTest";
    /// <summary>Complete a product analytical test request retest.</summary>
    public const string CanCompleteRetestProductTest = "CanCompleteRetestProductTest";
    public const string CanViewProductionScheduleMaterialForecast = "CanViewProductionScheduleMaterialForecast";
    public const string CanViewOpeningBalances = "CanViewOpeningBalances";
    public const string CanUploadOpeningBalances = "CanUploadOpeningBalances";
    public const string CanCorrectOpeningBalances = "CanCorrectOpeningBalances";
    public const string CanApproveOpeningBalances = "CanApproveOpeningBalances";
    /// <summary>Consolidate an unused approved raw-material migration top-up into its matching lot.</summary>
    public const string CanConsolidateRawMaterialTopUps = "CanConsolidateRawMaterialTopUps";
    /// <summary>Consolidate an unused approved packaging-material migration top-up into its matching lot.</summary>
    public const string CanConsolidatePackagingMaterialTopUps = "CanConsolidatePackagingMaterialTopUps";
    // Procurement
    public const string CanViewPurchaseRequisitions = "CanViewPurchaseRequisitions";
    public const string CanSourcePurchaseRequisition = "CanSourcePurchaseRequisition";
    public const string CanViewForeignQuotation = "CanViewForeignQuotation";
    public const string CanSendForeignQuotationRequest = "CanSendForeignQuotationRequest";
    public const string CanViewLocalQuotation = "CanViewLocalQuotation";
    public const string CanSendLocalQuotationRequest = "CanSendLocalQuotationRequest";
    public const string CanViewForeignQuotationResponse = "CanViewForeignQuotationResponse";
    public const string CanSendForeignQuotationResponse = "CanSendForeignQuotationResponse";
    public const string CanViewLocalQuotationResponse = "CanViewLocalQuotationResponse";
    public const string CanSendLocalQuotationResponse = "CanSendLocalQuotationResponse";
    public const string CanViewForeignVendorPricing = "CanViewForeignVendorPricing";
    public const string CanApplyChangesForForeignVendorPricingSelection =
        "CanApplyChangesForForeignVendorPricingSelection";
    public const string CanViewLocalVendorPricing = "CanViewLocalVendorPricing";
    public const string CanApplyChangesForLocalVendorPricingSelection =
        "CanApplyChangesForLocalVendorPricingSelection";
    public const string CanViewForeignProformaRequest = "CanViewForeignProformaRequest";
    public const string CanSendForeignProformaRequest = "CanSendForeignProformaRequest";
    public const string CanViewLocalProformaRequest = "CanViewLocalProformaRequest";
    public const string CanSendLocalProformaRequest = "CanSendLocalProformaRequest";
    public const string CanViewForeignProformaInvoiceSubmissions =
        "CanViewForeignProformaInvoiceSubmissions";
    public const string CanSendForeignProformaInvoice = "CanSendForeignProformaInvoice";
    public const string CanViewLocalProformaInvoiceSubmissions =
        "CanViewLocalProformaInvoiceSubmissions";
    public const string CanSendLocalProformaInvoice = "CanSendLocalProformaInvoice";
    public const string CanViewForeignPurchaseOrder = "CanViewForeignPurchaseOrder";
    public const string CanViewApprovedForeignPurchaseOrder = "CanViewApprovedForeignPurchaseOrder";
    public const string CanCreateForeignPurchaseOrder = "CanCreateForeignPurchaseOrder";
    public const string CanViewLocalPurchaseOrder = "CanViewLocalPurchaseOrder";
    public const string CanViewApprovedLocalPurchaseOrder = "CanViewApprovedLocalPurchaseOrder";
    public const string CanCreateLocalPurchaseOrder = "CanCreateLocalPurchaseOrder";
    public const string CanCheckForeignPurchaseOrder = "CanCheckForeignPurchaseOrder";
    public const string CanCheckLocalPurchaseOrder = "CanCheckLocalPurchaseOrder";
    public const string CanSendForeignPurchaseOrder = "CanSendForeignPurchaseOrder";
    public const string CanSendApprovedForeignPurchaseOrder = "CanSendApprovedForeignPurchaseOrder";
    public const string CanSendLocalPurchaseOrder = "CanSendLocalPurchaseOrder";
    public const string CanSendApprovedLocalPurchaseOrder = "CanSendApprovedLocalPurchaseOrder";
    public const string CanReviseForeignPurchaseOrder = "CanReviseForeignPurchaseOrder";
    public const string CanDeleteForeignPurchaseOrderItem = "CanDeleteForeignPurchaseOrderItem";
    public const string CanReAssignForeignPurchaseOrderItem = "CanReAssignForeignPurchaseOrderItem";
    public const string CanChangeSupplySourceForeignPurchaseOrderItem =
        "CanChangeSupplySourceForeignPurchaseOrderItem";
    public const string CanReviseLocalPurchaseOrder = "CanReviseLocalPurchaseOrder";
    /// <summary>Cancel a foreign or local purchase order (audit phase 3; granted to roles that could revise orders).</summary>
    public const string CanCancelForeignPurchaseOrder = "CanCancelForeignPurchaseOrder";
    public const string CanCancelLocalPurchaseOrder = "CanCancelLocalPurchaseOrder";
    /// <summary>Delete a foreign or local purchase order (audit phase 3; granted to nobody until assigned).</summary>
    public const string CanDeleteForeignPurchaseOrder = "CanDeleteForeignPurchaseOrder";
    public const string CanDeleteLocalPurchaseOrder = "CanDeleteLocalPurchaseOrder";
    public const string CanDeleteLocalPurchaseOrderItem = "CanDeleteLocalPurchaseOrderItem";
    public const string CanReAssignLocalPurchaseOrderItem = "CanReAssignLocalPurchaseOrderItem";
    public const string CanChangeSupplySourceLocalPurchaseOrderItem =
        "CanChangeSupplySourceLocalPurchaseOrderItem";
    public const string CanViewMaterialDistribution = "CanViewMaterialDistribution";
    public const string CanDistributeMaterial = "CanDistributeMaterial";

    // Logistics
    public const string CanCreateShipmentInvoice = "CanCreateShipmentInvoice";
    public const string CanViewShipmentInvoice = "CanViewShipmentInvoice";
    public const string CanEditShipmentInvoice = "CanEditShipmentInvoice";
    public const string CanDeleteShipmentInvoice = "CanDeleteShipmentInvoice";
    public const string CanCreateShipmentDocument = "CanCreateShipmentDocument";
    public const string CanViewShipmentDocument = "CanViewShipmentDocument";
    public const string CanChangeShipmentDocumentStatus = "CanChangeShipmentDocumentStatus";
    public const string CanEditShipmentDocument = "CanEditShipmentDocument";
    public const string CanDeleteShipmentDocument = "CanDeleteShipmentDocument";
    public const string CanViewShipmentDiscrepancy = "CanViewShipmentDiscrepancy";
    public const string CanRecordShipmentDiscrepancy = "CanRecordShipmentDiscrepancy";
    public const string CanCreateShortageMakeUpInvoice = "CanCreateShortageMakeUpInvoice";
    public const string CanApproveShipmentDiscrepancy = "CanApproveShipmentDiscrepancy";
    public const string CanRejectShipmentDiscrepancy = "CanRejectShipmentDiscrepancy";
    public const string CanCreateBillingSheet = "CanCreateBillingSheet";
    public const string CanViewBillingSheet = "CanViewBillingSheet";
    public const string CanEditBillingSheet = "CanEditBillingSheet";
    public const string CanDeleteBillingSheet = "CanDeleteBillingSheet";
    public const string CanCreateWaybill = "CanCreateWaybill";
    public const string CanViewWaybill = "CanViewWaybill";
    public const string CanChangeWaybillStatus = "CanChangeWaybillStatus";
    public const string CanEditWaybill = "CanEditWaybill";
    public const string CanDeleteWaybill = "CanDeleteWaybill";
    public const string CanViewRawMaterialStock = "CanViewRawMaterialStock";
    public const string CanViewPackingMaterialStock = "CanViewPackingMaterialStock";
    public const string CanViewPayments = "CanViewPayments";
    public const string CanRecordPayment = "CanRecordPayment";
    public const string CanApprovePayment = "CanApprovePayment";
    public const string CanViewCashflowReports = "CanViewCashflowReports";

    // Warehouse
    public const string CanViewRawMaterialsItems = "CanViewRawMaterialsItems";
    public const string CanCreatePreSamplingChecklist = "CanCreatePreSamplingChecklist";
    public const string CanCreateChecklistForRawMaterials = "CanCreateChecklistForRawMaterials";
    public const string CanCreateGrnForRawMaterials = "CanCreateGrnForRawMaterials";
    public const string CanViewPackagingMaterialsItems = "CanViewPackagingMaterialsItems";
    public const string CanCreateChecklistForPackagingMaterials =
        "CanCreateChecklistForPackagingMaterials";
    public const string CanCreateGrnForPackagingMaterials = "CanCreateGrnForPackagingMaterials";
    public const string CanViewRawMaterials = "CanViewRawMaterials";
    public const string CanViewQuarantineRawMaterials = "CanViewQuarantineRawMaterials";
    public const string CanDistributeRawMaterials = "CanDistributeRawMaterials";
    public const string CanViewDistributedRawMaterials = "CanViewDistributedRawMaterials";
    public const string CanViewRawMaterialsReceivedList = "CanViewRawMaterialsReceivedList";
    public const string CanViewPackagingMaterials = "CanViewPackagingMaterials";
    public const string CanViewDistributedPackagingMaterials =
        "CanViewDistributedPackagingMaterials";
    public const string CanViewQuarantinePackagingMaterials = "CanViewQuarantinePackagingMaterials";
    public const string CanDistributePackagingMaterials = "CanDistributePackagingMaterials";
    public const string CanViewPackagingMaterialsReceivedList =
        "CanViewPackagingMaterialsReceivedList";
    public const string CanAssignRawMaterialsStockToShelves = "CanAssignRawMaterialsStockToShelves";
    public const string CanAssignPackagingMaterialsStockToShelves =
        "CanAssignPackagingMaterialsStockToShelves";
    /// <summary>Release unused raw material reservations retained after a recorded production Unstart.</summary>
    public const string CanRepairUnstartRawMaterialReservations = "CanRepairUnstartRawMaterialReservations";
    /// <summary>Release unused packaging reservations retained after a recorded production Unstart.</summary>
    public const string CanRepairUnstartPackagingMaterialReservations = "CanRepairUnstartPackagingMaterialReservations";
    public const string CanViewApprovedRawMaterials = "CanViewApprovedRawMaterials";
    public const string CanViewApprovedPackagingMaterials = "CanViewApprovedPackagingMaterials";
    /// <summary>Read a material's department bin card.</summary>
    public const string CanViewMaterialBinCard = "CanViewMaterialBinCard";
    /// <summary>View and print the reconciled raw-material store, production-floor and consumed stock trace.</summary>
    public const string CanViewRawMaterialStockTrace = "CanViewRawMaterialStockTrace";
    /// <summary>View and print the reconciled packaging-material store, production-floor and consumed stock trace.</summary>
    public const string CanViewPackagingMaterialStockTrace = "CanViewPackagingMaterialStockTrace";
    /// <summary>Generate and print raw-material stock position, bin-card and consumption reports.</summary>
    public const string CanPrintRawMaterialStockTrace = "CanPrintRawMaterialStockTrace";
    /// <summary>Generate and print packaging-material stock position, bin-card and consumption reports.</summary>
    public const string CanPrintPackagingMaterialStockTrace = "CanPrintPackagingMaterialStockTrace";
    public const string CanViewLinkedRawMaterials = "CanViewLinkedRawMaterials";
    public const string CanEditLinkedRawMaterials = "CanEditLinkedRawMaterials";
    public const string CanUnlinkRawMaterials = "CanUnlinkRawMaterials";
    public const string CanViewLinkedPackagingMaterials = "CanViewLinkedPackagingMaterials";
    public const string CanUnlinkPackagingMaterials = "CanUnlinkPackagingMaterials";
    public const string CanViewUnlinkedRawMaterials = "CanViewUnlinkedRawMaterials";
    /// <summary>Sets the shared verified density of a raw material.</summary>
    public const string CanEditRawMaterialDensity = "CanEditRawMaterialDensity";
    /// <summary>Sets the shared verified density of a packaging material.</summary>
    public const string CanEditPackagingMaterialDensity = "CanEditPackagingMaterialDensity";
    /// <summary>Changes the unit the department counts a raw material in, converting every value it stores in that unit.</summary>
    public const string CanChangeRawMaterialCountingUnit = "CanChangeRawMaterialCountingUnit";
    /// <summary>Changes the unit the department counts a packaging material in, converting every value it stores in that unit.</summary>
    public const string CanChangePackagingMaterialCountingUnit = "CanChangePackagingMaterialCountingUnit";
    public const string CanLinkRawMaterials = "CanLinkRawMaterials";
    public const string CanViewUnlinkedPackagingMaterials = "CanViewUnlinkedPackagingMaterials";
    public const string CanLinkPackagingMaterials = "CanLinkPackagingMaterials";
    public const string CanCreateNewRawMaterials = "CanCreateNewRawMaterials";
    public const string CanEditRawMaterials = "CanEditRawMaterials";
    public const string CanDeleteRawMaterials = "CanDeleteRawMaterials";
    public const string CanCreateNewPackagingMaterials = "CanCreateNewPackagingMaterials";
    public const string CanEditPackagingMaterials = "CanEditPackagingMaterials";
    public const string CanDeletePackagingMaterials = "CanDeletePackagingMaterials";
    public const string CanViewRejectedRawMaterials = "CanViewRejectedRawMaterials";
    public const string CanViewRejectedPackagingMaterials = "CanViewRejectedPackagingMaterials";
    public const string CanViewRawMaterialRequisitions = "CanViewRawMaterialRequisitions";
    /// <summary>Cancel unissued raw material requests left behind by a recorded production unstart.</summary>
    public const string CanRepairUnstartRawMaterialRequisitions = "CanRepairUnstartRawMaterialRequisitions";
    /// <summary>Cancel unissued packaging requests left behind by a recorded production unstart.</summary>
    public const string CanRepairUnstartPackagingMaterialRequisitions = "CanRepairUnstartPackagingMaterialRequisitions";
    public const string CanIssueRawMaterialRequisitions = "CanIssueRawMaterialRequisitions";
    public const string CanSwapRawMaterialRequisitions = "CanSwapRawMaterialRequisitions";
    /// <summary>Choose which BOM option (main material or a substitute) a raw-material stock requisition line is issued as, before issue.</summary>
    public const string CanChooseRawMaterialRequisitionSubstitute = "CanChooseRawMaterialRequisitionSubstitute";
    public const string CanChoosePackagingMaterialRequisitionSubstitute = "CanChoosePackagingMaterialRequisitionSubstitute";
    public const string CanViewPackagingMaterialRequisitions =
        "CanViewPackagingMaterialRequisitions";
    public const string CanIssuePackagingMaterialRequisitions =
        "CanIssuePackagingMaterialRequisitions";
    /// <summary>Set how much of a packaging stock requisition line is issued when whole packs make it differ from the requirement, with a reason.</summary>
    public const string CanAdjustPackagingMaterialIssueQuantity = "CanAdjustPackagingMaterialIssueQuantity";
    public const string CanSwapPackagingMaterialRequisitions =
        "CanSwapPackagingMaterialRequisitions";
    public const string CanViewMaterialsReturnNote = "CanViewMaterialsReturnNote";
    public const string CanViewExpiredMaterials = "CanViewExpiredMaterials";
    public const string CanViewAboutToExpireMaterials = "CanViewAboutToExpireMaterials";
    public const string CanViewExpiryAcrossDepartments = "CanViewExpiryAcrossDepartments";
    public const string CanViewUpcomingBatchIssues = "CanViewUpcomingBatchIssues";
    public const string CanViewRawMaterialLocationChartList = "CanViewRawMaterialLocationChartList";
    public const string CanReassignRawMaterialStock = "CanReassignRawMaterialStock";
    public const string CanViewRawMaterialLocationList = "CanViewRawMaterialLocationList";
    public const string CanViewPackagingMaterialLocationChartList =
        "CanViewPackagingMaterialLocationChartList";
    public const string CanReassignPackagingMaterialStock = "CanReassignPackagingMaterialStock";
    public const string CanViewPackagingMaterialLocationList =
        "CanViewPackagingMaterialLocationList";
    public const string CanViewRawMaterialTransferList = "CanViewRawMaterialTransferList";
    public const string CanIssueRawMaterialStockTransfers = "CanIssueRawMaterialStockTransfers";
    public const string CanViewPackagingMaterialTransferList =
        "CanViewPackagingMaterialTransferList";
    public const string CanIssuePackagingMaterialStockTransfers =
        "CanIssuePackagingMaterialStockTransfers";
    public const string CanAssignRawMaterialStockTransfers = "CanAssignRawMaterialStockTransfers";
    public const string CanAssignPackagingMaterialStockTransfers =
        "CanAssignPackagingMaterialStockTransfers";
    public const string CanViewMaterialStockAdjustment = "CanViewMaterialStockAdjustment";
    public const string CanCreateMaterialStockAdjustment = "CanCreateMaterialStockAdjustment";
    public const string CanViewStockDiscrepancy = "CanViewStockDiscrepancy";
    public const string CanRecordStockDiscrepancy = "CanRecordStockDiscrepancy";
    public const string CanApproveStockDiscrepancy = "CanApproveStockDiscrepancy";
    public const string CanRejectStockDiscrepancy = "CanRejectStockDiscrepancy";
    public const string CanReconcileStockDiscrepancy = "CanReconcileStockDiscrepancy";

    // Production
    // Production's requisition list has its own keys; they used to be aliases of the Warehouse issue-list keys.
    public const string CanViewRawMaterialRequisitionsForProduction = "CanViewProductionRawMaterialRequisitions";
    public const string CanViewPackagingMaterialRequisitionsForProduction = "CanViewProductionPackagingMaterialRequisitions";
    public const string CanViewRawMaterialRequisitionsForCreation =
        "CanViewRawMaterialRequisitionsForCreation";
    public const string CanCreateRawMaterialRequisitions = "CanCreateRawMaterialRequisitions";
    public const string CanViewPackageMaterialRequisitionsForCreation =
        "CanViewPackageMaterialRequisitionsForCreation";
    public const string CanCreatePackageMaterialRequisitions =
        "CanCreatePackageMaterialRequisitions";
    public const string CanViewPlannedProducts = "CanViewPlannedProducts";
    public const string CanCreateNewProductionPlan = "CanCreateNewProductionPlan";
    public const string CanEditProductionPlan = "CanEditProductionPlan";
    /// <summary>Re-apply a product's current procedure to a running batch's active and upcoming steps.</summary>
    public const string CanRepairProcedure = "CanRepairProcedure";
    public const string CanRepairIssuedBom = "CanRepairIssuedBom";
    public const string CanRepairIssuedPackaging = "CanRepairIssuedPackaging";
    public const string CanUnstartProduction = "CanUnstartProduction";
    public const string CanRepairStockReservations = "CanRepairStockReservations";
    // White Placebo preparation: one key per task (view, recipe, request, issue, record output).
    public const string CanViewWhitePlacebo = "CanViewWhitePlacebo";
    public const string CanEditWhitePlaceboRecipe = "CanEditWhitePlaceboRecipe";
    public const string CanRequestWhitePlaceboIngredients = "CanRequestWhitePlaceboIngredients";
    public const string CanIssueWhitePlaceboIngredients = "CanIssueWhitePlaceboIngredients";
    public const string CanRecordWhitePlaceboOutput = "CanRecordWhitePlaceboOutput";
    public const string CanRepairBom = "CanRepairBom";
    public const string CanRepairPackaging = "CanRepairPackaging";
    public const string CanViewProductSchedules = "CanViewProductSchedules";
    public const string CanCreateProductSchedule = "CanCreateProductSchedule";
    public const string CanEditProductSchedule = "CanEditProductSchedule";
    public const string CanViewIncomingStockTransferRequests =
        "CanViewIncomingStockTransferRequests";
    public const string CanApproveIncomingStockTransferRequest =
        "CanApproveIncomingStockTransferRequest";
    public const string CanRejectIncomingStockTransferRequest =
        "CanRejectIncomingStockTransferRequest";
    public const string CanViewOutgoingStockTransferRequests =
        "CanViewOutgoingStockTransferRequests";
    public const string CanCreateInternalStockTransfer = "CanCreateInternalStockTransfer";
    public const string CanAddProductsToProductSchedule = "CanAddProductsToProductSchedule";
    public const string CanRemoveProductsFromProductSchedule =
        "CanRemoveProductsFromProductSchedule";

    // Quality Control
    public const string CanViewRawMaterialGoodsReceiptNotes = "CanViewRawMaterialGoodsReceiptNotes";
    public const string CanTakeRawMaterialSample = "CanTakeRawMaterialSample";
    public const string CanAssignRawMaterialTest = "CanAssignRawMaterialTest";
    public const string CanStartRawMaterialTest = "CanStartRawMaterialTest";
    public const string CanCheckRawMaterialTestResult = "CanCheckRawMaterialTestResult";
    public const string CanViewPackagingMaterialGoodsReceiptNotes =
        "CanViewPackagingMaterialGoodsReceiptNotes";
    public const string CanTakePackagingMaterialSample = "CanTakePackagingMaterialSample";
    public const string CanAssignPackagingMaterialTest = "CanAssignPackagingMaterialTest";
    public const string CanStartPackagingMaterialTest = "CanStartPackagingMaterialTest";
    public const string CanCheckPackagingMaterialTestResult = "CanCheckPackagingMaterialTestResult";
    public const string CanReassignRawMaterialTest = "CanReassignRawMaterialTest";
    public const string CanReassignPackagingMaterialTest = "CanReassignPackagingMaterialTest";
    public const string CanViewProductAnalyticalTestRequests =
        "CanViewProductAnalyticalTestRequests";
    public const string CanViewProductAnalyticalTestRequestsForAnalysing =
        "CanViewProductAnalyticalTestRequestsForAnalysing";
    public const string CanAcknowledgeSampleTaken = "CanAcknowledgeSampleTaken";
    public const string CanAssignProductTest = "CanAssignProductTest";
    /// <summary>Return a submitted Product test correction record before COA generation.</summary>
    public const string CanReturnProductTestForCorrection = "CanReturnProductTestForCorrection";
    /// <summary>Read the Product test correction record before COA generation.</summary>
    public const string CanViewProductTestCorrectionHistory = "CanViewProductTestCorrectionHistory";
    /// <summary>Return a submitted RawMaterial test correction record before COA generation.</summary>
    public const string CanReturnRawMaterialTestForCorrection = "CanReturnRawMaterialTestForCorrection";
    /// <summary>Read the RawMaterial test correction record before COA generation.</summary>
    public const string CanViewRawMaterialTestCorrectionHistory = "CanViewRawMaterialTestCorrectionHistory";
    /// <summary>Return a submitted PackagingMaterial test correction record before COA generation.</summary>
    public const string CanReturnPackagingMaterialTestForCorrection = "CanReturnPackagingMaterialTestForCorrection";
    /// <summary>Read the PackagingMaterial test correction record before COA generation.</summary>
    public const string CanViewPackagingMaterialTestCorrectionHistory = "CanViewPackagingMaterialTestCorrectionHistory";
    public const string CanReassignProductTest = "CanReassignProductTest";
    public const string CanStartProductTest = "CanStartProductTest";
    public const string CanCheckProductTest = "CanCheckProductTest";
    /// <summary>Allows printing a product's Analytical Test Request form (QAD/SOP/046/F02-04).</summary>
    public const string CanPrintProductAnalyticalTestRequest = "CanPrintProductAnalyticalTestRequest";
    /// <summary>Allows printing a product batch's generated certificate of analysis.</summary>
    public const string CanPrintProductCertificateOfAnalysis = "CanPrintProductCertificateOfAnalysis";
    public const string CanVerifyProduct = "CanVerifyProduct";
    public const string CanViewRawMaterialStps = "CanViewRawMaterialStps";
    public const string CanCreateRawMaterialStp = "CanCreateRawMaterialStp";
    public const string CanEditRawMaterialStp = "CanEditRawMaterialStp";
    public const string CanDeleteRawMaterialStp = "CanDeleteRawMaterialStp";
    public const string CanViewPackagingMaterialStps = "CanViewPackagingMaterialStps";
    public const string CanCreatePackagingMaterialStp = "CanCreatePackagingMaterialStp";
    public const string CanEditPackagingMaterialStp = "CanEditPackagingMaterialStp";
    public const string CanDeletePackagingMaterialStp = "CanDeletePackagingMaterialStp";
    public const string CanViewRawMaterialSpecifications = "CanViewRawMaterialSpecifications";
    /// <summary>Print raw material specifications with their recorded revisions.</summary>
    public const string CanExportRawMaterialSpecification = "CanExportRawMaterialSpecification";
    public const string CanCreateRawMaterialSpecification = "CanCreateRawMaterialSpecification";
    public const string CanEditRawMaterialSpecification = "CanEditRawMaterialSpecification";
    public const string CanDeleteRawMaterialSpecification = "CanDeleteRawMaterialSpecification";
    public const string CanVerifyRawMaterialSpecification = "CanVerifyRawMaterialSpecification";
    public const string CanViewPackagingMaterialSpecifications =
        "CanViewPackagingMaterialSpecifications";
    /// <summary>Print packaging material specifications with their recorded revisions.</summary>
    public const string CanExportPackagingMaterialSpecification = "CanExportPackagingMaterialSpecification";
    public const string CanCreatePackagingMaterialSpecification =
        "CanCreatePackagingMaterialSpecification";
    public const string CanEditPackagingMaterialSpecification =
        "CanEditPackagingMaterialSpecification";
    public const string CanDeletePackagingMaterialSpecification =
        "CanDeletePackagingMaterialSpecification";
    public const string CanVerifyPackagingMaterialSpecification =
        "CanVerifyPackagingMaterialSpecification";
    public const string CanViewRawMaterialArds = "CanViewRawMaterialArds";
    public const string CanCreateRawMaterialArd = "CanCreateRawMaterialArd";
    public const string CanEditRawMaterialArd = "CanEditRawMaterialArd";
    public const string CanDeleteRawMaterialArd = "CanDeleteRawMaterialArd";
    public const string CanVerifyRawMaterialArd = "CanVerifyRawMaterialArd";
    public const string CanViewPackagingMaterialArds = "CanViewPackagingMaterialArds";
    public const string CanCreatePackagingMaterialArd = "CanCreatePackagingMaterialArd";
    public const string CanEditPackagingMaterialArd = "CanEditPackagingMaterialArd";
    public const string CanDeletePackagingMaterialArd = "CanDeletePackagingMaterialArd";
    public const string CanVerifyPackagingMaterialArd = "CanVerifyPackagingMaterialArd";
    public const string CanViewProductStps = "CanViewProductStps";
    public const string CanCreateProductStp = "CanCreateProductStp";
    public const string CanEditProductStp = "CanEditProductStp";
    public const string CanDeleteProductStp = "CanDeleteProductStp";
    public const string CanViewProductSpecifications = "CanViewProductSpecifications";
    /// <summary>View the per-product assay outputs and their COA reporting configuration.</summary>
    public const string CanViewProductAssayReporting = "CanViewProductAssayReporting";
    /// <summary>Change per-product assay COA reporting rules with a recorded reason.</summary>
    public const string CanEditProductAssayReporting = "CanEditProductAssayReporting";
    /// <summary>Print product specifications with their recorded revisions.</summary>
    public const string CanExportProductSpecification = "CanExportProductSpecification";
    public const string CanCreateProductSpecification = "CanCreateProductSpecification";
    public const string CanEditProductSpecification = "CanEditProductSpecification";
    public const string CanDeleteProductSpecification = "CanDeleteProductSpecification";
    public const string CanVerifyProductSpecification = "CanVerifyProductSpecification";
    public const string CanViewProductArds = "CanViewProductArds";
    public const string CanCreateProductArd = "CanCreateProductArd";
    public const string CanEditProductArd = "CanEditProductArd";
    public const string CanDeleteProductArd = "CanDeleteProductArd";
    public const string CanVerifyProductArd = "CanVerifyProductArd";
    public const string CanViewRoutineTests = "CanViewRoutineTests";
    public const string CanCreateRoutineTest = "CanCreateRoutineTest";
    public const string CanAssignRoutineTest = "CanAssignRoutineTest";
    public const string CanPerformRoutineTest = "CanPerformRoutineTest";
    public const string CanCheckRoutineTest = "CanCheckRoutineTest";
    public const string CanApproveRoutineTest = "CanApproveRoutineTest";
    public const string CanViewRoutineSetup = "CanViewRoutineSetup";
    public const string CanManageRoutineSetup = "CanManageRoutineSetup";
    public const string CanManageMicrobialRequirements = "CanManageMicrobialRequirements";
    public const string CanViewWaterQuality = "CanViewWaterQuality";
    public const string CanManageWaterQuality = "CanManageWaterQuality";
    public const string CanViewQualityCertificates = "CanViewQualityCertificates";
    public const string CanGenerateQualityCertificate = "CanGenerateQualityCertificate";

    // Quality Assurance
    public const string CanViewIssuedBmrBprs = "CanViewIssuedBmrBprs";
    public const string CanIssueBmr = "CanIssueBmr";
    /// <summary>Delete a production schedule.</summary>
    public const string CanDeleteProductSchedule = "CanDeleteProductSchedule";
    /// <summary>Start production of a scheduled batch.</summary>
    public const string CanStartProduction = "CanStartProduction";
    /// <summary>Start or complete a production step on the batch board.</summary>
    public const string CanUpdateProductionStep = "CanUpdateProductionStep";
    /// <summary>Return materials before or after production.</summary>
    public const string CanReturnProductionMaterials = "CanReturnProductionMaterials";
    /// <summary>Record, change or remove a batch's final packing.</summary>
    public const string CanRecordFinalPacking = "CanRecordFinalPacking";
    /// <summary>Raise or change a finished goods transfer note.</summary>
    public const string CanCreateFgtn = "CanCreateFgtn";
    /// <summary>Request extra packing material for a batch.</summary>
    public const string CanRequestExtraPacking = "CanRequestExtraPacking";
    /// <summary>Create, change or issue a batch packaging record.</summary>
    public const string CanIssueBpr = "CanIssueBpr";
    /// <summary>Complete a material return note.</summary>
    public const string CanCompleteMaterialReturnNote = "CanCompleteMaterialReturnNote";
    public const string CanViewIssuedBmrRecords = "CanViewIssuedBmrRecords";
    public const string CanCorrectIssuedBmrBatchNumber = "CanCorrectIssuedBmrBatchNumber";
    public const string CanRequestIssuedBmrRevision = "CanRequestIssuedBmrRevision";
    public const string CanApproveIssuedBmrRevision = "CanApproveIssuedBmrRevision";
    /// <summary>Print the issued BMR/BPR register and the issued-by-product report.</summary>
    public const string CanPrintIssuedBmrRecords = "CanPrintIssuedBmrRecords";
    public const string CanViewAnalyticalTestRequests = "CanViewAnalyticalTestRequests";
    public const string CanViewAnalyticalTestRequestsForSampling =
        "CanViewAnalyticalTestRequestsForSampling";
    public const string CanTakeSamples = "CanTakeSamples";
    public const string CanViewPendingApprovals = "CanViewPendingApprovals";
    public const string CanApprovePendingApproval = "CanApprovePendingApproval";
    /// <summary>Reject an unissued raw material stock requisition and allow production to request it again.</summary>
    public const string CanRejectRawMaterialStockRequisition = "CanRejectRawMaterialStockRequisition";
    /// <summary>Reject an unissued packaging stock requisition and allow production to request it again.</summary>
    public const string CanRejectPackagingMaterialStockRequisition = "CanRejectPackagingMaterialStockRequisition";
    public const string CanRejectPendingApproval = "CanRejectPendingApproval";
    /// <summary>Edit a product certificate before approval and resubmit it.</summary>
    public const string CanEditProductCertificateOfAnalysis = "CanEditProductCertificateOfAnalysis";
    /// <summary>Request review of a product certificate assigned for approval.</summary>
    public const string CanRequestProductCertificateReview = "CanRequestProductCertificateReview";
    /// <summary>Request a correction of a product certificate assigned for approval.</summary>
    public const string CanRequestProductCertificateCorrection = "CanRequestProductCertificateCorrection";
    /// <summary>Edit a raw material certificate before approval and resubmit it.</summary>
    public const string CanEditRawMaterialCertificateOfAnalysis = "CanEditRawMaterialCertificateOfAnalysis";
    /// <summary>Request review of a raw material certificate assigned for approval.</summary>
    public const string CanRequestRawMaterialCertificateReview = "CanRequestRawMaterialCertificateReview";
    /// <summary>Request a correction of a raw material certificate assigned for approval.</summary>
    public const string CanRequestRawMaterialCertificateCorrection = "CanRequestRawMaterialCertificateCorrection";
    /// <summary>Edit a packaging material certificate before approval and resubmit it.</summary>
    public const string CanEditPackagingMaterialCertificateOfAnalysis = "CanEditPackagingMaterialCertificateOfAnalysis";
    /// <summary>Request review of a packaging material certificate assigned for approval.</summary>
    public const string CanRequestPackagingMaterialCertificateReview = "CanRequestPackagingMaterialCertificateReview";
    /// <summary>Request a correction of a packaging material certificate assigned for approval.</summary>
    public const string CanRequestPackagingMaterialCertificateCorrection = "CanRequestPackagingMaterialCertificateCorrection";
    public const string CanApproveResponse = "CanApproveResponse";
    public const string CanRejectResponse = "CanRejectResponse";
    public const string CanTransferApprovalRights = "CanTransferApprovalRights";
    public const string CanTransferAnyApprovalRights = "CanTransferAnyApprovalRights";
    public const string CanInitiateOosInvestigation = "CanInitiateOosInvestigation";
    public const string CanViewOosInvestigations = "CanViewOosInvestigations";
    public const string CanSubmitOosInvestigation = "CanSubmitOosInvestigation";
    public const string CanReviewOosInvestigation = "CanReviewOosInvestigation";

    // Audit
    public const string CanViewQualityAudits = "CanViewQualityAudits";
    public const string CanCreateQualityAudit = "CanCreateQualityAudit";
    public const string CanConductQualityAudit = "CanConductQualityAudit";
    public const string CanRaiseCorrectiveAction = "CanRaiseCorrectiveAction";
    public const string CanReviewCorrectiveAction = "CanReviewCorrectiveAction";
    public const string CanCloseQualityAudit = "CanCloseQualityAudit";

    // Research & Development
    // Note: approving/rejecting an RndProject goes through the generic
    // ApprovalController (POST /approval/approve|reject/RndProject/{id}), which -
    // like every other approval-gated module in this codebase - authorizes purely
    // by approval-stage UserId/RoleId assignment rather than a permission key, so
    // no CanApproveRndProject key exists here (it would have nowhere to be applied).
    public const string CanViewRndProjects = "CanViewRndProjects";
    public const string CanCreateRndProject = "CanCreateRndProject";
    public const string CanEditRndProject = "CanEditRndProject";
    public const string CanUpdateRndProjectStatus = "CanUpdateRndProjectStatus";
    public const string CanDeleteRndProject = "CanDeleteRndProject";
    public const string CanViewRndFormulations = "CanViewRndFormulations";
    // A calculation scratchpad in the desktop app; it reads and writes nothing on the server.
    public const string CanViewFormulaBuilder = "CanViewFormulaBuilder";
    public const string CanCreateRndFormulation = "CanCreateRndFormulation";
    public const string CanEditRndFormulation = "CanEditRndFormulation";
    public const string CanViewRndTrialBatches = "CanViewRndTrialBatches";
    public const string CanCreateRndTrialBatch = "CanCreateRndTrialBatch";
    public const string CanEditRndTrialBatch = "CanEditRndTrialBatch";
    public const string CanIssueTrialRequisition = "CanIssueTrialRequisition";
    public const string CanViewRndAnalyticalMethods = "CanViewRndAnalyticalMethods";
    public const string CanCreateRndAnalyticalMethod = "CanCreateRndAnalyticalMethod";
    public const string CanEditRndAnalyticalMethod = "CanEditRndAnalyticalMethod";
    public const string CanTransferRndAnalyticalMethod = "CanTransferRndAnalyticalMethod";
    public const string CanViewRndStabilityStudies = "CanViewRndStabilityStudies";
    public const string CanCreateRndStabilityStudy = "CanCreateRndStabilityStudy";
    public const string CanEditRndStabilityStudy = "CanEditRndStabilityStudy";
    public const string CanManageRndStabilityChambers = "CanManageRndStabilityChambers";
    public const string CanViewRndTechnologyTransfers = "CanViewRndTechnologyTransfers";
    public const string CanCreateRndTechnologyTransfer = "CanCreateRndTechnologyTransfer";
    public const string CanEditRndTechnologyTransfer = "CanEditRndTechnologyTransfer";
    public const string CanPromoteRndTechnologyTransfer = "CanPromoteRndTechnologyTransfer";

    // Finished Goods Warehouse
    public const string CanViewCustomers = "CanViewCustomers";
    public const string CanCreateCustomer = "CanCreateCustomer";
    public const string CanEditCustomer = "CanEditCustomer";
    public const string CanDeleteCustomer = "CanDeleteCustomer";
    public const string CanViewCustomerCreditStatus = "ViewCustomerCreditStatus";
    public const string CanManageCustomerContracts = "ManageCustomerContracts";
    /// <summary>Finance costing: product list prices and customer pricing agreements.</summary>
    public const string CanManageCosting = "ManageCosting";
    /// <summary>View price-list revisions and their prices.</summary>
    public const string CanViewCostingPriceList = "CanViewCostingPriceList";
    /// <summary>Prepare the first price-list revision for a division.</summary>
    public const string CanCreateCostingPriceList = "CanCreateCostingPriceList";
    /// <summary>Correct an editable price-list revision.</summary>
    public const string CanEditCostingPriceList = "CanEditCostingPriceList";
    /// <summary>Prepare a replacement price-list revision.</summary>
    public const string CanReviseCostingPriceList = "CanReviseCostingPriceList";
    /// <summary>Submit a price-list revision for review.</summary>
    public const string CanSubmitCostingPriceList = "CanSubmitCostingPriceList";
    /// <summary>Confirm the review of a price-list revision.</summary>
    public const string CanReviewCostingPriceList = "CanReviewCostingPriceList";
    /// <summary>Return a price-list revision for correction.</summary>
    public const string CanReturnCostingPriceList = "CanReturnCostingPriceList";
    /// <summary>Approve an assigned price-list revision.</summary>
    public const string CanApproveCostingPriceList = "CanApproveCostingPriceList";
    /// <summary>Reject an assigned price-list revision.</summary>
    public const string CanRejectCostingPriceList = "CanRejectCostingPriceList";
    /// <summary>Print a price-list revision and its signatories.</summary>
    public const string CanPrintCostingPriceList = "CanPrintCostingPriceList";
    public const string CanViewCustomerQuotations = "ViewCustomerQuotations";
    public const string CanCreateCustomerQuotation = "CreateCustomerQuotation";
    public const string CanApproveCustomerQuotation = "ApproveCustomerQuotation";
    public const string CanConvertCustomerQuotation = "ConvertCustomerQuotation";
    public const string CanViewOrder = "CanViewOrder";
    public const string CanCreateOrders = "CanCreateOrders";
    /// <summary>Change a production order.</summary>
    public const string CanEditOrder = "CanEditOrder";
    /// <summary>Delete a production order.</summary>
    public const string CanDeleteOrder = "CanDeleteOrder";
    /// <summary>Delete a proforma invoice.</summary>
    public const string CanDeleteProformaInvoice = "CanDeleteProformaInvoice";
    /// <summary>Create or change a customer invoice.</summary>
    public const string CanCreateInvoice = "CanCreateInvoice";
    /// <summary>Delete a customer invoice.</summary>
    public const string CanDeleteInvoice = "CanDeleteInvoice";
    /// <summary>Cancel a packing list and release reserved stock after documented unloading.</summary>
    public const string CanCancelFinishedGoodsAllocation = "CanCancelFinishedGoodsAllocation";
    /// <summary>Void an unpaid, undeparted finished goods invoice with a recorded reason.</summary>
    public const string CanVoidFinishedGoodsInvoice = "CanVoidFinishedGoodsInvoice";
    /// <summary>Authorise destruction of recalled stock held on site.</summary>
    public const string CanDisposeRecalledFinishedGoodsStock = "CanDisposeRecalledFinishedGoodsStock";
    /// <summary>Reconcile historical finished goods counts, conversion and evidenced departures.</summary>
    public const string CanReconcileFinishedGoodsStock = "CanReconcileFinishedGoodsStock";
    /// <summary>Post an approved finished goods stock adjustment to the received-stock ledger.</summary>
    public const string CanApplyFinishedGoodsStockAdjustment = "CanApplyFinishedGoodsStockAdjustment";
    /// <summary>Confirm the approved manufacturing cost basis per finished product pack and currency.</summary>
    public const string CanConfirmFinishedGoodsCosting = "CanConfirmFinishedGoodsCosting";
    /// <summary>Record Finance's external credit, refund or replacement resolution for a product recall.</summary>
    public const string CanRecordProductRecallSettlement = "CanRecordProductRecallSettlement";
    /// <summary>Print a finished goods waybill and its verified shipment details.</summary>
    public const string CanPrintFinishedGoodsWaybill = "CanPrintFinishedGoodsWaybill";
    /// <summary>Record the authority classification and notification evidence for an open product recall.</summary>
    public const string CanUpdateProductRecallDecision = "CanUpdateProductRecallDecision";
    /// <summary>Reopen a closed product recall with QA evidence while retaining its closure history.</summary>
    public const string CanReopenProductRecall = "CanReopenProductRecall";
    /// <summary>Verify the customer PO against proforma pack quantities, prices and currency.</summary>
    public const string CanVerifyFinishedGoodsCustomerPo = "CanVerifyFinishedGoodsCustomerPo";
    /// <summary>Confirm invoice payment or credit clearance before finished goods leave.</summary>
    public const string CanClearFinishedGoodsDispatch = "CanClearFinishedGoodsDispatch";
    /// <summary>Authorise finished goods departure and post the warehouse issue.</summary>
    public const string CanDispatchFinishedGoods = "CanDispatchFinishedGoods";
    /// <summary>Place finished goods batches on a quality hold.</summary>
    public const string CanHoldFinishedGoods = "CanHoldFinishedGoods";
    /// <summary>Release a finished goods hold with QA evidence.</summary>
    public const string CanReleaseFinishedGoodsHold = "CanReleaseFinishedGoodsHold";
    /// <summary>View product recall records and batch distribution traceability.</summary>
    public const string CanViewProductRecalls = "CanViewProductRecalls";
    /// <summary>Initiate a product batch recall and block further distribution.</summary>
    public const string CanCreateProductRecall = "CanCreateProductRecall";
    /// <summary>Record recall notifications and recipient acknowledgement.</summary>
    public const string CanNotifyProductRecall = "CanNotifyProductRecall";
    /// <summary>Record recalled products recovered into quarantine.</summary>
    public const string CanRecordProductRecallRecovery = "CanRecordProductRecallRecovery";
    /// <summary>Approve documented destruction of recovered recalled products.</summary>
    public const string CanDisposeProductRecallRecovery = "CanDisposeProductRecallRecovery";
    /// <summary>Close a reconciled product recall with an effectiveness report.</summary>
    public const string CanCloseProductRecall = "CanCloseProductRecall";
    /// <summary>Print product recall and distribution reconciliation records.</summary>
    public const string CanPrintProductRecall = "CanPrintProductRecall";
    /// <summary>Mark an allocation as loaded.</summary>
    public const string CanLoadFinishedGoods = "CanLoadFinishedGoods";
    /// <summary>Mark an allocation as delivered.</summary>
    public const string CanDeliverFinishedGoods = "CanDeliverFinishedGoods";
    /// <summary>Send a waybill to the customer.</summary>
    public const string CanSendWaybillToCustomer = "CanSendWaybillToCustomer";
    public const string CanGeneratePackingList = "CanGeneratePackingList";
    public const string CanViewPackingList = "CanViewPackingList";
    public const string CanGenerateProformaInvoice = "CanGenerateProformaInvoice";
    public const string CanViewProformaInvoice = "CanViewProformaInvoice";
    public const string CanSendProformaInvoice = "CanSendProformaInvoice";
    public const string CanViewInvoice = "CanViewInvoice";
    public const string CanViewWaybillForFgw = "CanViewWaybillForFgw";
    public const string CanCreateWaybillForFgw = "CanCreateWaybillForFgw";
    public const string CanEditWaybillForFgw = "CanEditWaybillForFgw";
    public const string CanDeleteWaybillForFgw = "CanDeleteWaybillForFgw";
    public const string CanViewFgtn = "CanViewFgtn";
    public const string CanApproveFgtn = "CanApproveFgtn";
    public const string CanViewApprovedProducts = "CanViewApprovedProducts";
    public const string CanEditApprovedProductPackingStyle = "CanEditApprovedProductPackingStyle";

    // STORES
    public const string CanViewItemsInReagentStore = "CanViewItemsInReagentStore";
    public const string CanCreateNewItemInReagentStore = "CanCreateNewItemInReagentStore";
    public const string CanEditItemInReagentStore = "CanEditItemInReagentStore";
    public const string CanDeleteItemFromReagentStore = "CanDeleteItemFromReagentStore";
    public const string CanViewItemsInEquipmentStore = "CanViewItemsInEquipmentStore";
    public const string CanCreateNewItemInEquipmentStore = "CanCreateNewItemInEquipmentStore";
    public const string CanEditItemInEquipmentStore = "CanEditItemInEquipmentStore";
    public const string CanDeleteItemFromEquipmentStore = "CanDeleteItemFromEquipmentStore";
    public const string CanViewItemsInGeneralStore = "CanViewItemsInGeneralStore";
    public const string CanCreateNewItemInGeneralStore = "CanCreateNewItemInGeneralStore";
    public const string CanEditItemInGeneralStore = "CanEditItemInGeneralStore";
    public const string CanDeleteItemFromGeneralStore = "CanDeleteItemFromGeneralStore";
    public const string CanViewItemsInItStore = "CanViewItemsInItStore";
    public const string CanCreateNewItemInItStore = "CanCreateNewItemInItStore";
    public const string CanEditItemInItStore = "CanEditItemInItStore";
    public const string CanDeleteItemFromItStore = "CanDeleteItemFromItStore";
    public const string CanViewReagentStoreReceivingArea = "CanViewReagentStoreReceivingArea";
    public const string CanApproveReagentStoreReceivingAreaEntry =
        "CanApproveReagentStoreReceivingAreaEntry";
    public const string CanRejectReagentStoreReceivingAreaEntry =
        "CanRejectReagentStoreReceivingAreaEntry";
    public const string CanViewEquipmentStoreReceivingArea = "CanViewEquipmentStoreReceivingArea";
    public const string CanApproveEquipmentStoreReceivingAreaEntry =
        "CanApproveEquipmentStoreReceivingAreaEntry";
    public const string CanRejectEquipmentStoreReceivingAreaEntry =
        "CanRejectEquipmentStoreReceivingAreaEntry";
    public const string CanViewGeneralStoreReceivingArea = "CanViewGeneralStoreReceivingArea";
    public const string CanApproveGeneralStoreReceivingAreaEntry =
        "CanApproveGeneralStoreReceivingAreaEntry";
    public const string CanRejectGeneralStoreReceivingAreaEntry =
        "CanRejectGeneralStoreReceivingAreaEntry";
    public const string CanViewItStoreReceivingArea = "CanViewItStoreReceivingArea";
    public const string CanApproveItStoreReceivingAreaEntry = "CanApproveItStoreReceivingAreaEntry";
    public const string CanRejectItStoreReceivingAreaEntry = "CanRejectItStoreReceivingAreaEntry";
    public const string CanViewReagentStoreAvailableStocks = "CanViewReagentStoreAvailableStocks";
    public const string CanViewEquipmentStoreAvailableStocks =
        "CanViewEquipmentStoreAvailableStocks";
    public const string CanViewGeneralStoreAvailableStocks = "CanViewGeneralStoreAvailableStocks";
    public const string CanViewItStoreAvailableStocks = "CanViewItStoreAvailableStocks";
    public const string CanViewReagentStorePurchaseRequisitions =
        "CanViewReagentStorePurchaseRequisitions";
    public const string CanCreateReagentStorePurchaseRequisition =
        "CanCreateReagentStorePurchaseRequisition";
    public const string CanEditReagentStorePurchaseRequisition =
        "CanEditReagentStorePurchaseRequisition";
    public const string CanDeleteReagentStorePurchaseRequisition =
        "CanDeleteReagentStorePurchaseRequisition";
    public const string CanViewEquipmentStorePurchaseRequisitions =
        "CanViewEquipmentStorePurchaseRequisitions";
    public const string CanCreateEquipmentStorePurchaseRequisition =
        "CanCreateEquipmentStorePurchaseRequisition";
    public const string CanEditEquipmentStorePurchaseRequisition =
        "CanEditEquipmentStorePurchaseRequisition";
    public const string CanDeleteEquipmentStorePurchaseRequisition =
        "CanDeleteEquipmentStorePurchaseRequisition";
    public const string CanViewGeneralStorePurchaseRequisitions =
        "CanViewGeneralStorePurchaseRequisitions";
    public const string CanCreateGeneralStorePurchaseRequisition =
        "CanCreateGeneralStorePurchaseRequisition";
    public const string CanEditGeneralStorePurchaseRequisition =
        "CanEditGeneralStorePurchaseRequisition";
    public const string CanDeleteGeneralStorePurchaseRequisition =
        "CanDeleteGeneralStorePurchaseRequisition";
    public const string CanViewItStorePurchaseRequisitions = "CanViewItStorePurchaseRequisitions";
    public const string CanCreateItStorePurchaseRequisition = "CanCreateItStorePurchaseRequisition";
    public const string CanEditItStorePurchaseRequisition = "CanEditItStorePurchaseRequisition";
    public const string CanDeleteItStorePurchaseRequisition = "CanDeleteItStorePurchaseRequisition";
    public const string CanViewReagentStoreStockRequisitions =
        "CanViewReagentStoreStockRequisitions";
    public const string CanCreateReagentStoreStockRequisition =
        "CanCreateReagentStoreStockRequisition";
    public const string CanViewEquipmentStoreStockRequisitions =
        "CanViewEquipmentStoreStockRequisitions";
    public const string CanCreateEquipmentStoreStockRequisition =
        "CanCreateEquipmentStoreStockRequisition";
    public const string CanViewGeneralStoreStockRequisitions =
        "CanViewGeneralStoreStockRequisitions";
    public const string CanCreateGeneralStoreStockRequisition =
        "CanCreateGeneralStoreStockRequisition";
    public const string CanViewItStoreStockRequisitions = "CanViewItStoreStockRequisitions";
    public const string CanCreateItStoreStockRequisition = "CanCreateItStoreStockRequisition";
    public const string CanViewReagentStoreIssueStocks = "CanViewReagentStoreIssueStocks";
    public const string CanCreateReagentStoreIssueStock = "CanCreateReagentStoreIssueStock";
    public const string CanViewEquipmentStoreIssueStocks = "CanViewEquipmentStoreIssueStocks";
    public const string CanCreateEquipmentStoreIssueStock = "CanCreateEquipmentStoreIssueStock";
    public const string CanViewGeneralStoreIssueStocks = "CanViewGeneralStoreIssueStocks";
    public const string CanCreateGeneralStoreIssueStock = "CanCreateGeneralStoreIssueStock";
    public const string CanViewItStoreIssueStocks = "CanViewItStoreIssueStocks";
    public const string CanCreateItStoreIssueStock = "CanCreateItStoreIssueStock";
    public const string CanViewReagentStoreReturnItems = "CanViewReagentStoreReturnItems";
    public const string CanCreateReagentStoreReturnItem = "CanCreateReagentStoreReturnItem";
    public const string CanEditReagentStoreReturnItem = "CanEditReagentStoreReturnItem";
    public const string CanDeleteReagentStoreReturnItem = "CanDeleteReagentStoreReturnItem";
    public const string CanViewEquipmentStoreReturnItems = "CanViewEquipmentStoreReturnItems";
    public const string CanCreateEquipmentStoreReturnItem = "CanCreateEquipmentStoreReturnItem";
    public const string CanEditEquipmentStoreReturnItem = "CanEditEquipmentStoreReturnItem";
    public const string CanDeleteEquipmentStoreReturnItem = "CanDeleteEquipmentStoreReturnItem";
    public const string CanViewGeneralStoreReturnItems = "CanViewGeneralStoreReturnItems";
    public const string CanCreateGeneralStoreReturnItem = "CanCreateGeneralStoreReturnItem";
    public const string CanEditGeneralStoreReturnItem = "CanEditGeneralStoreReturnItem";
    public const string CanDeleteGeneralStoreReturnItem = "CanDeleteGeneralStoreReturnItem";
    public const string CanViewItStoreReturnItems = "CanViewItStoreReturnItems";
    public const string CanCreateItStoreReturnItem = "CanCreateItStoreReturnItem";
    public const string CanEditItStoreReturnItem = "CanEditItStoreReturnItem";
    public const string CanDeleteItStoreReturnItem = "CanDeleteItStoreReturnItem";
    public const string CanViewReagentStoreDamageMissingItems =
        "CanViewReagentStoreDamageMissingItems";
    public const string CanCreateReagentStoreDamageMissingItemRecord =
        "CanCreateReagentStoreDamageMissingItemRecord";
    public const string CanEditReagentStoreDamageMissingItemRecord =
        "CanEditReagentStoreDamageMissingItemRecord";
    public const string CanDeleteReagentStoreDamageMissingItemRecord =
        "CanDeleteReagentStoreDamageMissingItemRecord";
    public const string CanViewEquipmentStoreDamageMissingItems =
        "CanViewEquipmentStoreDamageMissingItems";
    public const string CanCreateEquipmentStoreDamageMissingItemRecord =
        "CanCreateEquipmentStoreDamageMissingItemRecord";
    public const string CanEditEquipmentStoreDamageMissingItemRecord =
        "CanEditEquipmentStoreDamageMissingItemRecord";
    public const string CanDeleteEquipmentStoreDamageMissingItemRecord =
        "CanDeleteEquipmentStoreDamageMissingItemRecord";
    public const string CanViewGeneralStoreDamageMissingItems =
        "CanViewGeneralStoreDamageMissingItems";
    public const string CanCreateGeneralStoreDamageMissingItemRecord =
        "CanCreateGeneralStoreDamageMissingItemRecord";
    public const string CanEditGeneralStoreDamageMissingItemRecord =
        "CanEditGeneralStoreDamageMissingItemRecord";
    public const string CanDeleteGeneralStoreDamageMissingItemRecord =
        "CanDeleteGeneralStoreDamageMissingItemRecord";
    public const string CanViewItStoreDamageMissingItems = "CanViewItStoreDamageMissingItems";
    public const string CanCreateItStoreDamageMissingItemRecord =
        "CanCreateItStoreDamageMissingItemRecord";
    public const string CanEditItStoreDamageMissingItemRecord =
        "CanEditItStoreDamageMissingItemRecord";
    public const string CanDeleteItStoreDamageMissingItemRecord =
        "CanDeleteItStoreDamageMissingItemRecord";

    // Service and Store Procurement
    public const string CanViewPurchaseRequisitionsSourcing = "CanViewPurchaseRequisitionsSourcing";
    public const string CanSourcePurchaseRequisitionSourcing =
        "CanSourcePurchaseRequisitionSourcing";
    public const string CanViewQuotationRequestsForTrustedVendors =
        "CanViewQuotationRequestsForTrustedVendors";
    public const string CanCreateQuotationRequestForTrustedVendor =
        "CanCreateQuotationRequestForTrustedVendor";
    public const string CanViewQuotationResponsesForTrustedVendors =
        "CanViewQuotationResponsesForTrustedVendors";
    public const string CanSendQuotationResponseForTrustedVendor =
        "CanSendQuotationResponseForTrustedVendor";
    public const string CanViewServicesQuotationResponses = "CanViewServicesQuotationResponses";
    public const string CanSendServicesQuotationResponse = "CanSendServicesQuotationResponse";
    public const string CanViewOpenMarketQuotationSheets = "CanViewOpenMarketQuotationSheets";
    public const string CanCreateOpenMarketQuotationSheet = "CanCreateOpenMarketQuotationSheet";
    public const string CanViewTrustedVendorPricingSelection =
        "CanViewTrustedVendorPricingSelection";
    public const string CanApplyChangesForTrustedVendorPricingSelection =
        "CanApplyChangesForTrustedVendorPricingSelection";
    public const string CanViewOpenMarketPricingSelection = "CanViewOpenMarketPricingSelection";
    public const string CanApplyChangesForOpenMarketPricingSelection =
        "CanApplyChangesForOpenMarketPricingSelection";
    public const string CanViewServicesPricingSelection = "CanViewServicesPricingSelection";
    public const string CanApplyChangesForServicesPricingSelection =
        "CanApplyChangesForServicesPricingSelection";
    public const string CanViewServicesProformaRequests = "CanViewServicesProformaRequests";
    public const string CanSendServicesProformaRequest = "CanSendServicesProformaRequest";
    public const string CanViewServicesProformaResponses = "CanViewServicesProformaResponses";
    public const string CanSendServicesProformaResponse = "CanSendServicesProformaResponse";
    public const string CanViewMemoForTrustedVendors = "CanViewMemoForTrustedVendors";
    public const string CanCreateMemoForTrustedVendor = "CanCreateMemoForTrustedVendor";
    public const string CanViewOpenMarketMemo = "CanViewOpenMarketMemo";
    public const string CanCreateOpenMarketMemo = "CanCreateOpenMarketMemo";
    public const string CanViewServicesMemo = "CanViewServicesMemo";
    public const string CanCreateServicesMemo = "CanCreateServicesMemo";
    public const string CanViewPurchaseOrdersForTrustedVendors =
        "CanViewPurchaseOrdersForTrustedVendors";
    public const string CanCreatePurchaseOrderForTrustedVendor =
        "CanCreatePurchaseOrderForTrustedVendor";
    public const string CanViewOpenMarketPurchaseOrders = "CanViewOpenMarketPurchaseOrders";
    public const string CanCreateOpenMarketPurchaseOrder = "CanCreateOpenMarketPurchaseOrder";
    public const string CanViewServicesPurchaseOrders = "CanViewServicesPurchaseOrders";
    public const string CanCreateServicesPurchaseOrder = "CanCreateServicesPurchaseOrder";
    public const string CanViewServices = "CanViewServices";
    public const string CanCreateService = "CanCreateService";
    public const string CanEditService = "CanEditService";
    public const string CanDeleteService = "CanDeleteService";
    public const string CanViewContractors = "CanViewContractors";
    public const string CanCreateContractors = "CanCreateContractors";
    public const string CanEditContractors = "CanEditContractors";
    public const string CanDeleteContractors = "CanDeleteContractors";
    public const string CanViewJobRequests = "CanViewJobRequests";
    public const string CanCreateJobRequest = "CanCreateJobRequest";
    public const string CanEditJobRequest = "CanEditJobRequest";
    public const string CanDeleteJobRequest = "CanDeleteJobRequest";
    public const string CanAcknowledgeJobRequest = "CanAcknowledgeJobRequest";
    public const string CanAssignJobRequest = "CanAssignJobRequest";
    public const string CanStartJobRequest = "CanStartJobRequest";
    public const string CanCompleteJobRequest = "CanCompleteJobRequest";
    public const string CanViewJobOrders = "CanViewJobOrders";
    public const string CanCreateJobOrder = "CanCreateJobOrder";
    public const string CanEditJobOrder = "CanEditJobOrder";
    public const string CanDeleteJobOrder = "CanDeleteJobOrder";

    // Human Resources
    public const string CanViewActiveEmployee = "CanViewActiveEmployee";
    public const string CanViewInactiveEmployee = "CanViewInactiveEmployee";
    public const string CanViewNewEmployee = "CanViewNewEmployee";
    public const string CanRegisterEmployee = "CanRegisterEmployee";
    public const string CanUpdateEmployeeInfo = "CanUpdateEmployeeInfo";
    public const string CanUpdateEmployeeEmail = "CanUpdateEmployeeEmail";
    public const string CanViewEmployeeDetails = "CanViewEmployeeDetails";
    public const string CanViewDepartmentEmployee = "CanViewDepartmentEmployee";
    public const string CanExportDepartmentEmployee = "CanExportDepartmentEmployee";
    public const string CanViewDesignation = "CanViewDesignation";
    public const string CanCreateDesignation = "CanCreateDesignation";
    public const string CanEditDesignation = "CanEditDesignation";
    public const string CanDeleteDesignation = "CanDeleteDesignation";
    public const string CanViewStaffRequisition = "CanViewStaffRequisition";
    public const string CanCreateStaffRequisition = "CanCreateStaffRequisition";
    public const string CanEditStaffRequisition = "CanEditStaffRequisition";
    public const string CanDeleteStaffRequisition = "CanDeleteStaffRequisition";
    public const string CanViewLeaveRequests = "CanViewLeaveRequests";
    public const string CanCreateLeaveRequest = "CanCreateLeaveRequest";
    public const string CanEditLeaveRequest = "CanEditLeaveRequest";
    public const string CanRecallLeave = "CanRecallLeave";
    public const string CanCancelLeave = "CanCancelLeave";
    public const string CanViewLeaveType = "CanViewLeaveType";
    public const string CanCreateLeaveType = "CanCreateLeaveType";
    public const string CanEditLeaveType = "CanEditLeaveType";
    public const string CanDeleteLeaveType = "CanDeleteLeaveType";
    public const string CanCreateLeaveEntitlement = "CanCreateLeaveEntitlement";
    public const string CanEditLeaveEntitlement = "CanEditLeaveEntitlement";
    public const string CanDeleteLeaveEntitlement = "CanDeleteLeaveEntitlement";
    /// <summary>Delete a leave request record (not cancel or recall).</summary>
    public const string CanDeleteLeaveRequest = "CanDeleteLeaveRequest";
    /// <summary>Delete an employee record.</summary>
    public const string CanDeleteEmployee = "CanDeleteEmployee";
    public const string CanViewAttendanceReportUpload = "CanViewAttendanceReportUpload";
    public const string CanSubmitAttendanceReportUpload = "CanSubmitAttendanceReportUpload";
    public const string CanCancelAttendanceReportUpload = "CanCancelAttendanceReportUpload";
    public const string CanViewShiftScheduleReportUpload = "CanViewShiftScheduleReportUpload";
    public const string CanSubmitShiftScheduleReportUpload = "CanSubmitShiftScheduleReportUpload";
    public const string CanViewOvertimeManagement = "CanViewOvertimeManagement";
    public const string CanCreateOvertimeManagement = "CanCreateOvertimeManagement";
    public const string CanEditOvertimeManagement = "CanEditOvertimeManagement";
    public const string CanDeleteOvertimeManagement = "CanDeleteOvertimeManagement";
    public const string CanViewCompensation = "CanViewCompensation";
    public const string CanManageCompensation = "CanManageCompensation";
    public const string CanViewPayrollConfiguration = "CanViewPayrollConfiguration";
    public const string CanManagePayrollConfiguration = "CanManagePayrollConfiguration";
    public const string CanViewPayrollRuns = "CanViewPayrollRuns";
    public const string CanCreatePayrollRun = "CanCreatePayrollRun";
    public const string CanApprovePayrollRun = "CanApprovePayrollRun";
    public const string CanCancelPayrollRun = "CanCancelPayrollRun";
    public const string CanViewPayslip = "CanViewPayslip";
    public const string CanViewOwnPayslip = "CanViewOwnPayslip";
    public const string CanExportBankAdvice = "CanExportBankAdvice";
    public const string CanExportPayrollRegister = "CanExportPayrollRegister";
    public const string CanViewPerformanceCycles = "CanViewPerformanceCycles";
    public const string CanManagePerformanceCycles = "CanManagePerformanceCycles";
    public const string CanViewOwnGoals = "CanViewOwnGoals";
    public const string CanViewTeamGoals = "CanViewTeamGoals";
    public const string CanCreateGoal = "CanCreateGoal";
    public const string CanEditGoal = "CanEditGoal";
    public const string CanDeleteGoal = "CanDeleteGoal";
    public const string CanViewPerformanceReviews = "CanViewPerformanceReviews";
    public const string CanSubmitSelfAssessment = "CanSubmitSelfAssessment";
    public const string CanSubmitManagerReview = "CanSubmitManagerReview";
    public const string CanApprovePerformanceReview = "CanApprovePerformanceReview";

    // IT SUPPORT
    public const string CanViewActiveUser = "CanViewActiveUser";
    public const string CanCreateUser = "CanCreateUser";
    public const string CanEditUser = "CanEditUser";
    public const string CanBlockUser = "CanBlockUser";
    public const string CanViewInactiveUser = "CanViewInactiveUser";
    public const string CanUnblockUser = "CanUnblockUser";
    /// <summary>Change which role a user has. Only roles whose permissions the actor holds can be given.</summary>
    public const string CanAssignUserRole = "CanAssignUserRole";
    public const string CanDeleteUser = "CanDeleteUser";
    /// <summary>Lets a user move their own account to another production department.</summary>
    public const string CanSwitchDepartment = "CanSwitchDepartment";
    public const string CanViewAuditTrail = "CanViewAuditTrail";
    public const string CanViewRoles = "CanViewRoles";
    public const string CanCreateRole = "CanCreateRole";
    public const string CanEditRole = "CanEditRole";
    public const string CanDeleteRole = "CanDeleteRole";
    public const string CanViewPermissions = "CanViewPermissions";
    public const string CanUpdateExistingPermission = "CanUpdateExistingPermission";
    public const string CanResetPermission = "CanResetPermission";

    // Settings
    public const string CanViewProductCategories = "CanViewProductCategories";
    public const string CanCreateProductCategory = "CanCreateProductCategory";
    public const string CanEditProductCategory = "CanEditProductCategory";
    public const string CanDeleteProductCategory = "CanDeleteProductCategory";
    public const string CanViewRawCategories = "CanViewRawCategories";
    public const string CanCreateRawCategory = "CanCreateRawCategory";
    public const string CanEditRawCategory = "CanEditRawCategory";
    public const string CanDeleteRawCategory = "CanDeleteRawCategory";
    public const string CanViewPackageCategories = "CanViewPackageCategories";
    public const string CanCreatePackageCategory = "CanCreatePackageCategory";
    public const string CanEditPackageCategory = "CanEditPackageCategory";
    public const string CanDeletePackageCategory = "CanDeletePackageCategory";
    public const string CanViewItemCategory = "CanViewItemCategory";
    public const string CanCreateItemCategory = "CanCreateItemCategory";
    public const string CanEditItemCategory = "CanEditItemCategory";
    public const string CanDeleteItemCategory = "CanDeleteItemCategory";
    public const string CanViewEquipmentCategory = "CanViewEquipmentCategory";
    public const string CanCreateEquipmentCategory = "CanCreateEquipmentCategory";
    public const string CanEditEquipmentCategory = "CanEditEquipmentCategory";
    public const string CanDeleteEquipmentCategory = "CanDeleteEquipmentCategory";
    public const string CanViewMaterialTypes = "CanViewMaterialTypes";
    public const string CanCreateMaterialType = "CanCreateMaterialType";
    public const string CanEditMaterialType = "CanEditMaterialType";
    public const string CanDeleteMaterialType = "CanDeleteMaterialType";
    public const string CanViewUomStyle = "CanViewUomStyle";
    public const string CanCreateUomStyle = "CanCreateUomStyle";
    public const string CanEditUomStyle = "CanEditUomStyle";
    public const string CanDeleteUomStyle = "CanDeleteUomStyle";
    public const string CanViewPackageStyle = "CanViewPackageStyle";
    public const string CanCreatePackageStyle = "CanCreatePackageStyle";
    public const string CanEditPackageStyle = "CanEditPackageStyle";
    public const string CanDeletePackageStyle = "CanDeletePackageStyle";
    public const string CanViewProductState = "CanViewProductState";
    public const string CanCreateProductState = "CanCreateProductState";
    public const string CanEditProductState = "CanEditProductState";
    public const string CanDeleteProductState = "CanDeleteProductState";
    public const string CanViewResources = "CanViewResources";
    public const string CanCreateResource = "CanCreateResource";
    public const string CanEditResource = "CanEditResource";
    public const string CanDeleteResource = "CanDeleteResource";
    public const string CanViewOperations = "CanViewOperations";
    /// <summary>View the linked operations and saved production-board order for a department.</summary>
    public const string CanViewDepartmentProductionBoardOrder = "CanViewDepartmentProductionBoardOrder";
    /// <summary>Reorder a department's production-board columns without changing recorded batch procedures.</summary>
    public const string CanReorderDepartmentProductionBoard = "CanReorderDepartmentProductionBoard";
    public const string CanCreateOperation = "CanCreateOperation";
    public const string CanEditOperation = "CanEditOperation";
    public const string CanDeleteOperation = "CanDeleteOperation";
    /// <summary>Link an operation to the production department that uses it.</summary>
    public const string CanLinkOperationDepartment = "CanLinkOperationDepartment";
    /// <summary>Unlink an operation from its production department.</summary>
    public const string CanUnlinkOperationDepartment = "CanUnlinkOperationDepartment";
    public const string CanViewWorkCenters = "CanViewWorkCenters";
    public const string CanCreateWorkCenter = "CanCreateWorkCenter";
    public const string CanEditWorkCenter = "CanEditWorkCenter";
    public const string CanDeleteWorkCenter = "CanDeleteWorkCenter";
    public const string CanViewSite = "CanViewSite";
    public const string CanCreateSite = "CanCreateSite";
    public const string CanEditWorkSite = "CanEditWorkSite";
    public const string CanDeleteSite = "CanDeleteSite";
    public const string CanViewCountries = "CanViewCountries";
    public const string CanCreateCountries = "CanCreateCountries";
    public const string CanEditCountries = "CanEditCountries";
    public const string CanDeleteCountries = "CanDeleteCountries";
    public const string CanViewPaymentTerms = "CanViewPaymentTerms";
    public const string CanCreatePaymentTerm = "CanCreatePaymentTerm";
    public const string CanEditPaymentTerm = "CanEditPaymentTerm";
    public const string CanDeletePaymentTerm = "CanDeletePaymentTerm";
    public const string CanViewDeliveryModes = "CanViewDeliveryModes";
    public const string CanCreateDeliveryMode = "CanCreateDeliveryMode";
    public const string CanEditDeliveryMode = "CanEditDeliveryMode";
    public const string CanDeleteDeliveryMode = "CanDeleteDeliveryMode";
    public const string CanViewCharges = "CanViewCharges";
    public const string CanCreateCharges = "CanCreateCharges";
    public const string CanEditCharges = "CanEditCharges";
    public const string CanDeleteCharges = "CanDeleteCharges";
    public const string CanViewThreshold = "CanViewThreshold";
    public const string CanCreateThreshold = "CanCreateThreshold";
    public const string CanEditThreshold = "CanEditThreshold";
    public const string CanDeleteThreshold = "CanDeleteThreshold";
    public const string CanViewCodeSettings = "CanViewCodeSettings";
    public const string CanCreateNewCodes = "CanCreateNewCodes";
    public const string CanEditCodeSettings = "CanEditCodeSettings";
    public const string CanDeleteCodeSettings = "CanDeleteCodeSettings";
    public const string CanViewQuestions = "CanViewQuestions";
    public const string CanCreateQuestions = "CanCreateQuestions";
    public const string CanEditQuestions = "CanEditQuestions";
    public const string CanRepairQuestionFormula = "CanRepairQuestionFormula";
    public const string CanRepairTestFormula = "CanRepairTestFormula";
    /// <summary>See which open tests of a worksheet template miss fields added after assignment, and preview the template repair.</summary>
    public const string CanViewTemplateRepair = "CanViewTemplateRepair";
    /// <summary>Apply a reviewed template repair: give fields added to a template to analysts already on its open tests.</summary>
    public const string CanApplyTemplateRepair = "CanApplyTemplateRepair";
    public const string CanDeleteQuestions = "CanDeleteQuestions";
    public const string CanApplyFormulaMigration = "CanApplyFormulaMigration";
    public const string CanViewReviewFormulaRevision = "CanViewReviewFormulaRevision";
    public const string CanReviewFormulaRevision = "CanReviewFormulaRevision";
    public const string CanViewApproveFormulaRevision = "CanViewApproveFormulaRevision";
    public const string CanApproveFormulaRevision = "CanApproveFormulaRevision";
    /// <summary>Return a formula revision in review to draft, with a comment, for its author to correct.</summary>
    public const string CanReturnFormulaRevision = "CanReturnFormulaRevision";
    public const string CanExecuteFormulaResponse = "CanExecuteFormulaResponse";
    public const string CanReviewFormRevision = "CanReviewFormRevision";
    public const string CanApproveFormRevision = "CanApproveFormRevision";
    public const string CanViewTemplate = "CanViewTemplate";
    public const string CanCreateTemplate = "CanCreateTemplate";
    public const string CanEditTemplate = "CanEditTemplate";
    public const string CanDeleteTemplate = "CanDeleteTemplate";
    public const string CanViewAlerts = "CanViewAlerts";
    public const string CanCreateNewAlerts = "CanCreateNewAlerts";
    public const string CanEditAlerts = "CanEditAlerts";
    public const string CanEnableDisableAlerts = "CanEnableDisableAlerts";
    public const string CanDeleteAlerts = "CanDeleteAlerts";
    public const string CanViewApprovals = "CanViewApprovals";
    public const string CanCreateNewApproval = "CanCreateNewApproval";
    public const string CanEditApprovalWorkflow = "CanEditApprovalWorkflow";
    public const string CanDeleteApprovals = "CanDeleteApprovals";
    /// <summary>See which roles and users review each document type.</summary>
    public const string CanViewDocumentReviewSettings = "CanViewDocumentReviewSettings";
    /// <summary>Create or change who reviews a document type.</summary>
    public const string CanEditDocumentReviewSettings = "CanEditDocumentReviewSettings";
    /// <summary>Remove a document type's review step.</summary>
    public const string CanDeleteDocumentReviewSettings = "CanDeleteDocumentReviewSettings";
    /// <summary>See the records waiting for your review or returned to you.</summary>
    public const string CanViewPendingReviews = "CanViewPendingReviews";
    /// <summary>Return a foreign purchase order awaiting check to its preparer for correction.</summary>
    public const string CanReturnForeignPurchaseOrderCheck = "CanReturnForeignPurchaseOrderCheck";
    /// <summary>Return a local purchase order awaiting check to its preparer for correction.</summary>
    public const string CanReturnLocalPurchaseOrderCheck = "CanReturnLocalPurchaseOrderCheck";
    /// <summary>Correct a raw material purchase requisition while it is in review, and send it back for review.</summary>
    public const string CanEditRawMaterialPurchaseRequisition = "CanEditRawMaterialPurchaseRequisition";
    /// <summary>Correct a packaging material purchase requisition while it is in review, and send it back for review.</summary>
    public const string CanEditPackageMaterialPurchaseRequisition = "CanEditPackageMaterialPurchaseRequisition";
    /// <summary>Confirm the review of a raw material purchase requisition, which sends it for approval.</summary>
    public const string CanReviewRawMaterialPurchaseRequisition = "CanReviewRawMaterialPurchaseRequisition";
    /// <summary>Confirm the review of a packaging material purchase requisition, which sends it for approval.</summary>
    public const string CanReviewPackageMaterialPurchaseRequisition = "CanReviewPackageMaterialPurchaseRequisition";
    /// <summary>Return a raw material purchase requisition in review to its preparer for correction.</summary>
    public const string CanReturnRawMaterialPurchaseRequisition = "CanReturnRawMaterialPurchaseRequisition";
    /// <summary>Return a packaging material purchase requisition in review to its preparer for correction.</summary>
    public const string CanReturnPackageMaterialPurchaseRequisition = "CanReturnPackageMaterialPurchaseRequisition";
    /// <summary>Confirm the review of a raw material stock requisition, which sends it for approval.</summary>
    public const string CanReviewRawMaterialStockRequisition = "CanReviewRawMaterialStockRequisition";
    /// <summary>Confirm the review of a packaging material stock requisition, which sends it for approval.</summary>
    public const string CanReviewPackageMaterialStockRequisition = "CanReviewPackageMaterialStockRequisition";
    /// <summary>Return a raw material stock requisition in review to its preparer for correction.</summary>
    public const string CanReturnRawMaterialStockRequisition = "CanReturnRawMaterialStockRequisition";
    /// <summary>Return a packaging material stock requisition in review to its preparer for correction.</summary>
    public const string CanReturnPackageMaterialStockRequisition = "CanReturnPackageMaterialStockRequisition";
    /// <summary>Confirm the review of a billing sheet, which sends it for approval.</summary>
    public const string CanReviewBillingSheet = "CanReviewBillingSheet";
    /// <summary>Return a billing sheet in review to its preparer for correction.</summary>
    public const string CanReturnBillingSheet = "CanReturnBillingSheet";
    /// <summary>Confirm the review of a staff requisition, which sends it for approval.</summary>
    public const string CanReviewStaffRequisition = "CanReviewStaffRequisition";
    /// <summary>Return a staff requisition in review to its preparer for correction.</summary>
    public const string CanReturnStaffRequisition = "CanReturnStaffRequisition";
    /// <summary>Confirm the review of an overtime request, which sends it for approval.</summary>
    public const string CanReviewOvertimeRequest = "CanReviewOvertimeRequest";
    /// <summary>Return an overtime request in review to its preparer for correction.</summary>
    public const string CanReturnOvertimeRequest = "CanReturnOvertimeRequest";
    /// <summary>Confirm the review of a leave request, which sends it for approval.</summary>
    public const string CanReviewLeaveRequest = "CanReviewLeaveRequest";
    /// <summary>Return a leave request in review to its preparer for correction.</summary>
    public const string CanReturnLeaveRequest = "CanReturnLeaveRequest";
    /// <summary>Confirm the review of a job request, which sends it for approval.</summary>
    public const string CanReviewJobRequest = "CanReviewJobRequest";
    /// <summary>Return a job request in review to its preparer for correction.</summary>
    public const string CanReturnJobRequest = "CanReturnJobRequest";
    /// <summary>Confirm the review of a packing list, which sends it for approval.</summary>
    public const string CanReviewPackingList = "CanReviewPackingList";
    /// <summary>Return a packing list in review to its preparer for correction.</summary>
    public const string CanReturnPackingList = "CanReturnPackingList";
    /// <summary>Check issued raw material: confirm the issue's review, signed on the voucher as "Checked by".</summary>
    public const string CanCheckRawMaterialIssue = "CanCheckRawMaterialIssue";
    /// <summary>Check issued packaging material: confirm the issue's review, signed on the voucher as "Checked by".</summary>
    public const string CanCheckPackagingMaterialIssue = "CanCheckPackagingMaterialIssue";
    /// <summary>Return a raw material issue to the store for correction through the issued-material repair.</summary>
    public const string CanReturnRawMaterialIssue = "CanReturnRawMaterialIssue";
    /// <summary>Return a packaging material issue to the store for correction through the issued-material repair.</summary>
    public const string CanReturnPackagingMaterialIssue = "CanReturnPackagingMaterialIssue";
    public const string CanViewSignatureSettings = "CanViewSignatureSettings";
    public const string CanCreateSignatureSettings = "CanCreateSignatureSettings";
    public const string CanViewChangePassword = "CanViewChangePassword";
    public const string CanChangePassword = "CanChangePassword";

    // Inventory Management
    public const string CanViewManufacturers = "CanViewManufacturers";
    public const string CanCreateManufacturer = "CanCreateManufacturer";
    public const string CanUpdateManufacturerDetails = "CanUpdateManufacturerDetails";
    public const string CanDeleteManufacturer = "CanDeleteManufacturer";
    public const string CanViewVendors = "CanViewVendors";
    public const string CanCreateVendor = "CanCreateVendor";
    public const string CanUpdateVendorDetails = "CanUpdateVendorDetails";
    public const string CanDeleteVendor = "CanDeleteVendor";
    public const string CanViewSupplierCertifications = "CanViewSupplierCertifications";
    public const string CanManageSupplierCertifications = "CanManageSupplierCertifications";
    public const string CanViewSupplierPerformance = "CanViewSupplierPerformance";
    public const string CanManageSupplierContracts = "CanManageSupplierContracts";
    public const string CanViewSupplierSpend = "CanViewSupplierSpend";
    public const string CanViewWarehouses = "CanViewWarehouses";
    /// <summary>Create a warehouse. New in the permissions audit (phase 3); granted to nobody until assigned.</summary>
    public const string CanCreateWarehouse = "CanCreateWarehouse";
    public const string CanEditWarehouse = "CanEditWarehouse";
    public const string CanDeleteWarehouse = "CanDeleteWarehouse";
    public const string CanViewLocations = "CanViewLocations";
    public const string CanAddNewLocation = "CanAddNewLocation";
    public const string CanEditLocation = "CanEditLocation";
    public const string CanDeleteLocation = "CanDeleteLocation";
    public const string CanViewRacks = "CanViewRacks";
    public const string CanAddNewRack = "CanAddNewRack";
    public const string CanEditRack = "CanEditRack";
    public const string CanDeleteRack = "CanDeleteRack";
    public const string CanViewShelves = "CanViewShelves";
    public const string CanAddNewShelf = "CanAddNewShelf";
    public const string CanEditShelf = "CanEditShelf";
    public const string CanDeleteShelf = "CanDeleteShelf";
    public const string CanViewEquipment = "CanViewEquipment";
    public const string CanCreateEquipment = "CanCreateEquipment";
    public const string CanEditEquipmentDetails = "CanEditEquipmentDetails";
    public const string CanDeleteEquipment = "CanDeleteEquipment";
    public const string CanViewQcEquipment = "CanViewQcEquipment";
    public const string CanCreateQcEquipment = "CanCreateQcEquipment";
    public const string CanEditQcEquipmentDetails = "CanEditQcEquipmentDetails";
    public const string CanDeleteQcEquipment = "CanDeleteQcEquipment";
    public const string CanViewQcEquipmentCalibration = "CanViewQcEquipmentCalibration";
    public const string CanUpdateQcEquipmentCalibration = "CanUpdateQcEquipmentCalibration";
    public const string CanViewInstrumentCalibration = "CanViewInstrumentCalibration";
    public const string CanUpdateInstrumentCalibration = "CanUpdateInstrumentCalibration";
    public const string CanViewUnitOfMeasure = "CanViewUnitOfMeasure";
    public const string CanCreateUnitOfMeasure = "CanCreateUnitOfMeasure";
    public const string CanEditUnitOfMeasure = "CanEditUnitOfMeasure";
    public const string CanDeleteUnitOfMeasure = "CanDeleteUnitOfMeasure";

    // Organizational Structure
    public const string CanViewDepartments = "CanViewDepartments";
    public const string CanCreateNewDepartment = "CanCreateNewDepartment";
    public const string CanEditDepartment = "CanEditDepartment";
    public const string CanDeleteDepartment = "CanDeleteDepartment";
    public const string CanViewWorkingDays = "CanViewWorkingDays";
    public const string CanCreateWorkingDays = "CanCreateWorkingDays";
    public const string CanResetWorkingDays = "CanResetWorkingDays";
    public const string CanViewHolidays = "CanViewHolidays";
    public const string CanCreateHoliday = "CanCreateHoliday";
    public const string CanEditHoliday = "CanEditHoliday";
    public const string CanDeleteHoliday = "CanDeleteHoliday";
    public const string CanViewShiftTypes = "CanViewShiftTypes";
    public const string CanCreateShiftType = "CanCreateShiftType";
    public const string CanEditShiftType = "CanEditShiftType";
    public const string CanDeleteShiftType = "CanDeleteShiftType";
    public const string CanViewShiftSchedule = "CanViewShiftSchedule";
    public const string CanCreateShiftSchedule = "CanCreateShiftSchedule";
    public const string CanEditShiftSchedule = "CanEditShiftSchedule";
    public const string CanDeleteShiftSchedule = "CanDeleteShiftSchedule";
    public const string CanViewShiftCategories = "CanViewShiftCategories";
    public const string CanCreateShiftCategory = "CanCreateShiftCategory";
    public const string CanEditShiftCategory = "CanEditShiftCategory";
    public const string CanDeleteShiftCategory = "CanDeleteShiftCategory";
    public const string CanViewWorkingHoursPolicy = "CanViewWorkingHoursPolicy";
    public const string CanManageWorkingHoursPolicy = "CanManageWorkingHoursPolicy";

    // Reports
    public const string CanViewHumanResourcesReport = "CanViewHumanResourcesReport";
    public const string CanViewInventoryReport = "CanViewInventoryReport";
    public const string CanViewQcAndQaReport = "CanViewQcAndQaReport";
    public const string CanViewWarehouseReport = "CanViewWarehouseReport";
    public const string CanViewStoreAndInventoryReport = "CanViewStoreAndInventoryReport";
    public const string CanViewProcurementReport = "CanViewProcurementReport";
    public const string CanViewFgtnReport = "CanViewFgtnReport";

    // One key per report (reports hub). The group keys above still guard the older register reports.
    /// <summary>Run and print the approved manufacturer list for raw and packaging materials.</summary>
    public const string CanViewApprovedManufacturerListReport = "CanViewApprovedManufacturerListReport";
    /// <summary>Run and print the supplier and manufacturer mapping, summary.</summary>
    public const string CanViewSupplierMappingSummaryReport = "CanViewSupplierMappingSummaryReport";
    /// <summary>Run and print the supplier and manufacturer mapping, detail.</summary>
    public const string CanViewSupplierMappingDetailReport = "CanViewSupplierMappingDetailReport";
    /// <summary>Run and print the purchase orders report.</summary>
    public const string CanViewPurchaseOrdersReport = "CanViewPurchaseOrdersReport";
    /// <summary>Run and print the invoiced purchase orders report.</summary>
    public const string CanViewInvoicedPurchaseOrdersReport = "CanViewInvoicedPurchaseOrdersReport";
    /// <summary>Run and print the shipments report.</summary>
    public const string CanViewShipmentsReport = "CanViewShipmentsReport";
    /// <summary>Run and print the material list report.</summary>
    public const string CanViewMaterialListReport = "CanViewMaterialListReport";
    /// <summary>Run and print the materials per department report.</summary>
    public const string CanViewMaterialsPerDepartmentReport = "CanViewMaterialsPerDepartmentReport";
    /// <summary>Run and print the material stock report, summary.</summary>
    public const string CanViewMaterialStockSummaryReport = "CanViewMaterialStockSummaryReport";
    /// <summary>Run and print the material stock report, detail by batch.</summary>
    public const string CanViewMaterialStockDetailReport = "CanViewMaterialStockDetailReport";
    /// <summary>Run and print the stock count sheet.</summary>
    public const string CanViewStockCountSheetReport = "CanViewStockCountSheetReport";
    /// <summary>Run and print the expired stock report.</summary>
    public const string CanViewExpiredStockReport = "CanViewExpiredStockReport";
    /// <summary>Run and print the inventory valuation report.</summary>
    public const string CanViewInventoryValuationReport = "CanViewInventoryValuationReport";
    /// <summary>Run and print the monthly production schedule report.</summary>
    public const string CanViewMonthlyProductionScheduleReport = "CanViewMonthlyProductionScheduleReport";
    /// <summary>Run and print the daily production schedule report.</summary>
    public const string CanViewDailyProductionScheduleReport = "CanViewDailyProductionScheduleReport";
    /// <summary>Run and print the product readiness report.</summary>
    public const string CanViewProductReadinessReport = "CanViewProductReadinessReport";
    /// <summary>Run and print the BMR issued report.</summary>
    public const string CanViewBmrIssuedReport = "CanViewBmrIssuedReport";
    /// <summary>Run and print the list of BMRs issued for one product.</summary>
    public const string CanViewBmrIssuedByProductReport = "CanViewBmrIssuedByProductReport";
    /// <summary>Run and print the stock issued to production batches.</summary>
    public const string CanViewStockIssuedReport = "CanViewStockIssuedReport";
    /// <summary>Run and print the production monthly report, one row per batch.</summary>
    public const string CanViewProductionMonthlyReport = "CanViewProductionMonthlyReport";
    /// <summary>Run and print the monthly production schedule summary by product and market.</summary>
    public const string CanViewProductionScheduleSummaryReport = "CanViewProductionScheduleSummaryReport";
    /// <summary>Run and print the batch value report: packing style, unit price, expected and actual quantity.</summary>
    public const string CanViewProductionBatchValueReport = "CanViewProductionBatchValueReport";
    /// <summary>Run and print the product list report.</summary>
    public const string CanViewProductListReport = "CanViewProductListReport";
    /// <summary>Run and print the department product list report.</summary>
    public const string CanViewDepartmentProductListReport = "CanViewDepartmentProductListReport";
    /// <summary>Run and print the product stock report, summary.</summary>
    public const string CanViewProductStockSummaryReport = "CanViewProductStockSummaryReport";
    /// <summary>Run and print the product stock report, detail by batch.</summary>
    public const string CanViewProductStockDetailReport = "CanViewProductStockDetailReport";
    /// <summary>Run and print the finished goods transfer report, summary.</summary>
    public const string CanViewFinishedGoodsTransferSummaryReport = "CanViewFinishedGoodsTransferSummaryReport";
    /// <summary>Run and print the finished goods transfer report, detail.</summary>
    public const string CanViewFinishedGoodsTransferDetailReport = "CanViewFinishedGoodsTransferDetailReport";
    /// <summary>Run and print the invoiced products report.</summary>
    public const string CanViewInvoicedProductsReport = "CanViewInvoicedProductsReport";
    /// <summary>Run and print the store items report.</summary>
    public const string CanViewStoreItemsReport = "CanViewStoreItemsReport";
    /// <summary>Run and print the store stock report.</summary>
    public const string CanViewStoreStockReport = "CanViewStoreStockReport";
    /// <summary>Run and print the vendor item mapping, summary.</summary>
    public const string CanViewVendorItemMappingSummaryReport = "CanViewVendorItemMappingSummaryReport";
    /// <summary>Run and print the vendor item mapping, detail.</summary>
    public const string CanViewVendorItemMappingDetailReport = "CanViewVendorItemMappingDetailReport";
    /// <summary>Run and print the employee master list.</summary>
    public const string CanViewEmployeeMasterListReport = "CanViewEmployeeMasterListReport";
    /// <summary>Run and print the staff count by grade report.</summary>
    public const string CanViewStaffGradeReport = "CanViewStaffGradeReport";
    /// <summary>Run and print the staff total report.</summary>
    public const string CanViewStaffTotalReport = "CanViewStaffTotalReport";
    /// <summary>Run and print the staff gender ratio report.</summary>
    public const string CanViewStaffGenderRatioReport = "CanViewStaffGenderRatioReport";
    /// <summary>Run and print the new hires and exits report.</summary>
    public const string CanViewNewHiresAndExitsReport = "CanViewNewHiresAndExitsReport";
    /// <summary>Run and print the staff turnover report.</summary>
    public const string CanViewStaffTurnoverReport = "CanViewStaffTurnoverReport";
    /// <summary>Run and print the leave overview report.</summary>
    public const string CanViewLeaveOverviewReport = "CanViewLeaveOverviewReport";
    /// <summary>Run and print the daily attendance report.</summary>
    public const string CanViewDailyAttendanceReport = "CanViewDailyAttendanceReport";
    /// <summary>Run and print the department attendance report.</summary>
    public const string CanViewDepartmentAttendanceReport = "CanViewDepartmentAttendanceReport";

    // IT Support Tickets
    public const string CanCreateTicket = "CanCreateTicket";
    public const string CanViewAllTickets = "CanViewAllTickets";
    public const string CanAssignTicket = "CanAssignTicket";
    public const string CanCloseTicket = "CanCloseTicket";
    public const string CanCommentOnTicket = "CanCommentOnTicket";
}

public static class PermissionUtils
{
    public static IEnumerable<PermissionDto> GeneratePermissions()
    {
        string GenerateDisplayName(string key) => Regex.Replace(key, "(\\B[A-Z])", " $1");
        string CreateDescription(string name) => $"Allows the user to {name[3..].ToLower()}.";
        var permissions = new List<PermissionDto>();

        Action<string, string, string> addPermission = (module, submodule, key) =>
        {
            var name = GenerateDisplayName(key);
            permissions.Add(
                new PermissionDto(module, submodule, key, name, CreateDescription(name))
            );
        };

        addPermission(PermissionModules.Warehouse, "Expired Materials", PermissionKeys.CanUpdateMigrationRetestDate);
        addPermission(PermissionModules.Warehouse, "Expired Materials", PermissionKeys.CanUpdateMigrationBatchDetails);
        addPermission(PermissionModules.Procurement, "Store Item GRNs", PermissionKeys.CanCreateItemGrn);
        addPermission(PermissionModules.Procurement, "Store Item GRNs", PermissionKeys.CanEditItemGrn);
        addPermission(PermissionModules.Procurement, "Store Item GRNs", PermissionKeys.CanDeleteItemGrn);
        addPermission(PermissionModules.Settings, "Products", PermissionKeys.CanManageOtherCollectionTypes);
        addPermission(PermissionModules.Production, "Equipment", PermissionKeys.CanUploadEquipment);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanUploadProductStock);
        addPermission(PermissionModules.Procurement, "Purchase Order Invoices", PermissionKeys.CanCreatePurchaseOrderInvoice);
        addPermission(PermissionModules.Procurement, "Purchase Order Invoices", PermissionKeys.CanDeletePurchaseOrderInvoice);
        addPermission(PermissionModules.Production, "Work Orders", PermissionKeys.CanCreateWorkOrder);
        addPermission(PermissionModules.Production, "Work Orders", PermissionKeys.CanEditWorkOrder);
        addPermission(PermissionModules.Production, "Work Orders", PermissionKeys.CanDeleteWorkOrder);
        addPermission(PermissionModules.Warehouse, "Arrival Locations", PermissionKeys.CanCreateArrivalLocation);
        addPermission(PermissionModules.Warehouse, "Arrival Locations", PermissionKeys.CanEditArrivalLocation);
        addPermission(PermissionModules.Warehouse, "Arrival Locations", PermissionKeys.CanConfirmMaterialArrival);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanCreateMaterialBatch);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanImportMaterialBatches);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanMoveMaterialBatch);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanConsumeMaterialAtLocation);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanChangeMaterialBatchStatus);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanRequestMaterialBatchRetest);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanCompleteMaterialBatchRetest);
        addPermission(PermissionModules.Warehouse, "Material Batches", PermissionKeys.CanFixStockInconsistencies);
        addPermission(PermissionModules.Procurement, "Purchase Requisition Sourcing", PermissionKeys.CanEditSourceRequisition);
        addPermission(PermissionModules.Procurement, "Purchase Requisition Sourcing", PermissionKeys.CanDeleteSourceRequisition);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewHrDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewHrExecutiveSummary);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewProductionHealthDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewFgtnDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewWarehouseDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewInventoryCommandCenter);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewLogisticsDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewProcurementDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewQcDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewQaDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewServicesDashboard);
        addPermission(PermissionModules.Reports, "Dashboards", PermissionKeys.CanViewStoresDashboard);
        // Products (audit phase 2): Production authors, QA maintains, the FG warehouse lists.
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProducts);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanImportProductionProducts);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanExportProductionProducts);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductInfo);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanCreateProductionProduct);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductInfo);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanDeleteProductionProduct);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanAddProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanDeleteProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanArchiveProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanImportProductionProductBom);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductPackingMaterials);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanAddProductionProductPackingMaterials);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductPackingMaterials);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanDeleteProductionProductPackingMaterials);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanImportProductionProductPackingMaterials);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductPackingStyles);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanAddProductionProductPackingStyles);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductPackingStyles);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanDeleteProductionProductPackingStyles);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductPackingOrder);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductPackingOrder);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductProcedure);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanAddProductionProductProcedure);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanEditProductionProductProcedure);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanDeleteProductionProductProcedure);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductSpecifications);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductStps);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanViewProductionProductArds);
        addPermission(PermissionModules.Production, "Products", PermissionKeys.CanPrintProductionProduct);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanViewApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanCreateApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanEditApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanEditApprovedVendorListDraft);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanSubmitApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanDeleteApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanReviseApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanReviewApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanReturnApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanApproveApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanRejectApprovedVendorList);
        addPermission(PermissionModules.QualityAssurance, "Approved Vendor List", PermissionKeys.CanPrintApprovedVendorList);
        addPermission(PermissionModules.Procurement, "Approved Vendor List", PermissionKeys.CanViewApprovedVendorEligibility);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProducts);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductInfo);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductInfo);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductBom);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanAddQaProductBom);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductBom);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanDeleteQaProductBom);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanArchiveQaProductBom);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductPackingMaterials);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanAddQaProductPackingMaterials);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductPackingMaterials);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanDeleteQaProductPackingMaterials);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductPackingStyles);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanAddQaProductPackingStyles);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductPackingStyles);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanDeleteQaProductPackingStyles);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductPackingOrder);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductPackingOrder);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductProcedure);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanAddQaProductProcedure);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanEditQaProductProcedure);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanDeleteQaProductProcedure);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductSpecifications);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductArds);
        addPermission(PermissionModules.QualityAssurance, "Products", PermissionKeys.CanViewQaProductStps);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanVerifyFinishedGoodsCustomerPo);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanClearFinishedGoodsDispatch);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanDispatchFinishedGoods);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanHoldFinishedGoods);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanReleaseFinishedGoodsHold);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanViewProductRecalls);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanCreateProductRecall);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanNotifyProductRecall);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanRecordProductRecallRecovery);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanDisposeProductRecallRecovery);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanCloseProductRecall);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanPrintProductRecall);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanCancelFinishedGoodsAllocation);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Allocations", PermissionKeys.CanVoidFinishedGoodsInvoice);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanDisposeRecalledFinishedGoodsStock);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Products", PermissionKeys.CanReconcileFinishedGoodsStock);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Products", PermissionKeys.CanApplyFinishedGoodsStockAdjustment);
        addPermission(PermissionModules.Finance, "Costing", PermissionKeys.CanConfirmFinishedGoodsCosting);
        addPermission(PermissionModules.Finance, "Product Recall Settlements", PermissionKeys.CanRecordProductRecallSettlement);
        addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.Waybill, PermissionKeys.CanPrintFinishedGoodsWaybill);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanReopenProductRecall);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Product Recalls", PermissionKeys.CanUpdateProductRecallDecision);
        addPermission(PermissionModules.FinishedGoodsWarehouse, "Products", PermissionKeys.CanViewFgProducts);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanApproveMaterialBatch);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanRequestRetestProductTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanCompleteRetestProductTest);
        addPermission(PermissionModules.Warehouse, "Production Material Forecast", PermissionKeys.CanViewProductionScheduleMaterialForecast);

        // Opening-balance drafts and posting are separately permissioned.
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanViewOpeningBalances);
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanUploadOpeningBalances);
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanCorrectOpeningBalances);
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanApproveOpeningBalances);
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanConsolidateRawMaterialTopUps);
        addPermission(PermissionModules.Settings, "Opening Balances", PermissionKeys.CanConsolidatePackagingMaterialTopUps);

        // Procurement
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseRequisition,
            PermissionKeys.CanViewPurchaseRequisitions
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseRequisition,
            PermissionKeys.CanSourcePurchaseRequisition
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsRequest,
            PermissionKeys.CanViewForeignQuotation
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsRequest,
            PermissionKeys.CanSendForeignQuotationRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsRequest,
            PermissionKeys.CanViewLocalQuotation
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsRequest,
            PermissionKeys.CanSendLocalQuotationRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsResponses,
            PermissionKeys.CanViewForeignQuotationResponse
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsResponses,
            PermissionKeys.CanSendForeignQuotationResponse
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsResponses,
            PermissionKeys.CanViewLocalQuotationResponse
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.QuotationsResponses,
            PermissionKeys.CanSendLocalQuotationResponse
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PriceComparison,
            PermissionKeys.CanViewForeignVendorPricing
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PriceComparison,
            PermissionKeys.CanApplyChangesForForeignVendorPricingSelection
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PriceComparison,
            PermissionKeys.CanViewLocalVendorPricing
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PriceComparison,
            PermissionKeys.CanApplyChangesForLocalVendorPricingSelection
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaRequest,
            PermissionKeys.CanViewForeignProformaRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaRequest,
            PermissionKeys.CanSendForeignProformaRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaRequest,
            PermissionKeys.CanViewLocalProformaRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaRequest,
            PermissionKeys.CanSendLocalProformaRequest
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaResponses,
            PermissionKeys.CanViewForeignProformaInvoiceSubmissions
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaResponses,
            PermissionKeys.CanSendForeignProformaInvoice
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaResponses,
            PermissionKeys.CanViewLocalProformaInvoiceSubmissions
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ProformaResponses,
            PermissionKeys.CanSendLocalProformaInvoice
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanViewForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanViewApprovedForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanCreateForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanViewLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanViewApprovedLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CreatePurchaseOrdersPage,
            PermissionKeys.CanCreateLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanViewForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanViewApprovedForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanCheckForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanViewLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanViewApprovedLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanCheckLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanViewForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanViewApprovedForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanSendForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanSendApprovedForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanViewLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanViewApprovedLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanSendLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.ApprovePurchaseOrderPage,
            PermissionKeys.CanSendApprovedLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanViewForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanCreateForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanReviseForeignPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanDeleteForeignPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanReAssignForeignPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanChangeSupplySourceForeignPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanViewLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanViewApprovedLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanCreateLocalPurchaseOrder
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanReviseLocalPurchaseOrder
        );
        addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanCancelForeignPurchaseOrder);
        addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanCancelLocalPurchaseOrder);
        addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanDeleteForeignPurchaseOrder);
        addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanDeleteLocalPurchaseOrder);
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanDeleteLocalPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanReAssignLocalPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.PurchaseOrderListPage,
            PermissionKeys.CanChangeSupplySourceLocalPurchaseOrderItem
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.MaterialDistribution,
            PermissionKeys.CanViewMaterialDistribution
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.MaterialDistribution,
            PermissionKeys.CanDistributeMaterial
        );

        // Logistics
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentInvoice,
            PermissionKeys.CanCreateShipmentInvoice
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentInvoice,
            PermissionKeys.CanViewShipmentInvoice
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentInvoice,
            PermissionKeys.CanEditShipmentInvoice
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentInvoice,
            PermissionKeys.CanDeleteShipmentInvoice
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.Waybill,
            PermissionKeys.CanCreateWaybill
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.Waybill,
            PermissionKeys.CanViewWaybill
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.Waybill,
            PermissionKeys.CanChangeWaybillStatus
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.Waybill,
            PermissionKeys.CanEditWaybill
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.Waybill,
            PermissionKeys.CanDeleteWaybill
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanCreateShipmentDocument
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanViewShipmentDocument
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanChangeShipmentDocumentStatus
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanEditShipmentDocument
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanDeleteShipmentDocument
        );
        addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanViewShipmentDiscrepancy);
        addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanRecordShipmentDiscrepancy);
        addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice,
            PermissionKeys.CanCreateShortageMakeUpInvoice);
        addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanApproveShipmentDiscrepancy);
        addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument,
            PermissionKeys.CanRejectShipmentDiscrepancy);
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanCreateBillingSheet
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanViewBillingSheet
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanEditBillingSheet
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanDeleteBillingSheet
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.AvailableStock,
            PermissionKeys.CanViewRawMaterialStock
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.AvailableStock,
            PermissionKeys.CanViewPackingMaterialStock
        );
        // Finance. Grants are stored by key, so moving these keys here from Logistics and
        // Reports keeps every role's existing access.
        addPermission(
            PermissionModules.Finance,
            PermissionSubmodules.Payments,
            PermissionKeys.CanViewPayments
        );
        addPermission(
            PermissionModules.Finance,
            PermissionSubmodules.Payments,
            PermissionKeys.CanRecordPayment
        );
        addPermission(
            PermissionModules.Finance,
            PermissionSubmodules.Payments,
            PermissionKeys.CanApprovePayment
        );
        addPermission(
            PermissionModules.Finance,
            PermissionSubmodules.CashflowReports,
            PermissionKeys.CanViewCashflowReports
        );
        addPermission(
            PermissionModules.Finance,
            PermissionSubmodules.Costing,
            PermissionKeys.CanManageCosting
        );
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanViewCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanCreateCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanEditCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanReviseCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanSubmitCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanReviewCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanReturnCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanApproveCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanRejectCostingPriceList);
        addPermission(PermissionModules.Finance, PermissionSubmodules.Costing, PermissionKeys.CanPrintCostingPriceList);

        // Warehouse
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanViewRawMaterialsItems
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanViewRawMaterialsReceivedList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanCreatePreSamplingChecklist
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanCreateChecklistForRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanCreateGrnForRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanViewPackagingMaterialsItems
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanCreateChecklistForPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ReceivingArea,
            PermissionKeys.CanCreateGrnForPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewQuarantineRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanDistributeRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewDistributedRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewDistributedPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanViewQuarantinePackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.QuarantineAreaGrn,
            PermissionKeys.CanDistributePackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.DistributedArea,
            PermissionKeys.CanViewRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.DistributedArea,
            PermissionKeys.CanAssignRawMaterialsStockToShelves
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.DistributedArea,
            PermissionKeys.CanViewPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.DistributedArea,
            PermissionKeys.CanAssignPackagingMaterialsStockToShelves
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ApprovedMaterials,
            PermissionKeys.CanViewApprovedRawMaterials
        );
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanRepairUnstartRawMaterialReservations);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanRepairUnstartPackagingMaterialReservations);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewMaterialBinCard);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewRawMaterialStockTrace);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewPackagingMaterialStockTrace);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanPrintRawMaterialStockTrace);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanPrintPackagingMaterialStockTrace);
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ApprovedMaterials,
            PermissionKeys.CanViewApprovedPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.LinkedMaterials,
            PermissionKeys.CanViewLinkedRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.LinkedMaterials,
            PermissionKeys.CanEditLinkedRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.LinkedMaterials,
            PermissionKeys.CanUnlinkRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.LinkedMaterials,
            PermissionKeys.CanViewLinkedPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.LinkedMaterials,
            PermissionKeys.CanUnlinkPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.UnlinkedMaterials,
            PermissionKeys.CanViewUnlinkedRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.UnlinkedMaterials,
            PermissionKeys.CanLinkRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.UnlinkedMaterials,
            PermissionKeys.CanViewUnlinkedPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.UnlinkedMaterials,
            PermissionKeys.CanLinkPackagingMaterials
        );
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanEditRawMaterialDensity);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanEditPackagingMaterialDensity);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanChangeRawMaterialCountingUnit);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanChangePackagingMaterialCountingUnit);
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanViewRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanCreateNewRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanEditRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanDeleteRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanViewPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanViewPackagingMaterialsReceivedList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanCreateNewPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanEditPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.Materials,
            PermissionKeys.CanDeletePackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.RejectedMaterials,
            PermissionKeys.CanViewRejectedRawMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.RejectedMaterials,
            PermissionKeys.CanViewRejectedPackagingMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanViewRawMaterialRequisitions
        );
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanRepairUnstartRawMaterialRequisitions);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanRepairUnstartPackagingMaterialRequisitions);
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanIssueRawMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanSwapRawMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanChooseRawMaterialRequisitionSubstitute
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanChoosePackagingMaterialRequisitionSubstitute
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanViewPackagingMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanIssuePackagingMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanAdjustPackagingMaterialIssueQuantity
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanSwapPackagingMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ExtraPackagingRequisitions,
            PermissionKeys.CanIssuePackagingMaterialRequisitions
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialReturnNote,
            PermissionKeys.CanViewMaterialsReturnNote
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialReturnNote,
            PermissionKeys.CanCompleteMaterialReturnNote
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialReturnNote,
            PermissionKeys.CanAssignPackagingMaterialsStockToShelves
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ExpiredMaterials,
            PermissionKeys.CanViewExpiredMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ExpiredMaterials,
            PermissionKeys.CanViewAboutToExpireMaterials
        );
        addPermission(
            PermissionModules.Warehouse,
            "Upcoming Batch Issues",
            PermissionKeys.CanViewUpcomingBatchIssues
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.ExpiredMaterials,
            PermissionKeys.CanViewExpiryAcrossDepartments
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanViewRawMaterialLocationChartList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanReassignRawMaterialStock
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanViewRawMaterialLocationList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanViewPackagingMaterialLocationChartList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanReassignPackagingMaterialStock
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.MaterialLocation,
            PermissionKeys.CanViewPackagingMaterialLocationList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanViewMaterialStockAdjustment
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanCreateMaterialStockAdjustment
        );
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanViewStockDiscrepancy);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanRecordStockDiscrepancy);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanApproveStockDiscrepancy);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanRejectStockDiscrepancy);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockAdjustment,
            PermissionKeys.CanReconcileStockDiscrepancy);
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferIssues,
            PermissionKeys.CanViewRawMaterialTransferList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferIssues,
            PermissionKeys.CanIssueRawMaterialStockTransfers
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferIssues,
            PermissionKeys.CanViewPackagingMaterialTransferList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferIssues,
            PermissionKeys.CanIssuePackagingMaterialStockTransfers
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferredAllocated,
            PermissionKeys.CanViewRawMaterialTransferList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferredAllocated,
            PermissionKeys.CanAssignRawMaterialStockTransfers
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferredAllocated,
            PermissionKeys.CanViewPackagingMaterialTransferList
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.StockTransferredAllocated,
            PermissionKeys.CanAssignPackagingMaterialStockTransfers
        );

        // Production
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Requisitions,
            PermissionKeys.CanViewRawMaterialRequisitionsForProduction
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Requisitions,
            PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Requisitions,
            PermissionKeys.CanCreateInternalStockTransfer
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanViewRawMaterialRequisitionsForCreation
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanCreateRawMaterialRequisitions
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanViewPackageMaterialRequisitionsForCreation
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanCreatePackageMaterialRequisitions
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewPlannedProducts
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanRequestExtraPacking
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateFgtn
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanRecordFinalPacking
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanReturnProductionMaterials
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanUpdateProductionStep
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateNewProductionPlan
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditProductionPlan
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.Products,
            PermissionKeys.CanRepairProcedure
        );
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanRepairIssuedBom);
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanRepairIssuedPackaging);
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanUnstartProduction);
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanRepairStockReservations);
        addPermission(PermissionModules.Production, PermissionSubmodules.WhitePlacebo, PermissionKeys.CanViewWhitePlacebo);
        addPermission(PermissionModules.Production, PermissionSubmodules.WhitePlacebo, PermissionKeys.CanEditWhitePlaceboRecipe);
        addPermission(PermissionModules.Production, PermissionSubmodules.WhitePlacebo, PermissionKeys.CanRequestWhitePlaceboIngredients);
        addPermission(PermissionModules.Production, PermissionSubmodules.WhitePlacebo, PermissionKeys.CanRecordWhitePlaceboOutput);
        addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanIssueWhitePlaceboIngredients);
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanRepairBom);
        addPermission(PermissionModules.Production, PermissionSubmodules.Products, PermissionKeys.CanRepairPackaging);
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanViewProductSchedules
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanCreateProductSchedule
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanEditProductSchedule
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanAddProductsToProductSchedule
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanRemoveProductsFromProductSchedule
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanStartProduction
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.ProductSchedule,
            PermissionKeys.CanDeleteProductSchedule
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.StockTransferRequests,
            PermissionKeys.CanViewIncomingStockTransferRequests
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.StockTransferRequests,
            PermissionKeys.CanApproveIncomingStockTransferRequest
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.StockTransferRequests,
            PermissionKeys.CanRejectIncomingStockTransferRequest
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.StockTransferRequests,
            PermissionKeys.CanViewOutgoingStockTransferRequests
        );

        // Quality Control
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanViewRawMaterialGoodsReceiptNotes
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanTakeRawMaterialSample
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanAssignRawMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.Testing,
            PermissionKeys.CanStartRawMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanCheckRawMaterialTestResult
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanCreateUniformityOfWeight
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanEditUniformityOfWeight
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanDeleteUniformityOfWeight
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanSubmitUniformityOfWeight
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanReassignRawMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanViewPackagingMaterialGoodsReceiptNotes
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanTakePackagingMaterialSample
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanAssignPackagingMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.Testing,
            PermissionKeys.CanStartPackagingMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanCheckPackagingMaterialTestResult
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.GoodsReceiptNote,
            PermissionKeys.CanReassignPackagingMaterialTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanViewProductAnalyticalTestRequests
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanViewProductAnalyticalTestRequestsForAnalysing
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanAcknowledgeSampleTaken
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanAssignProductTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanEditAnalyticalTestRequest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanChangeAnalyticalTestRequestStatus
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanDeleteAnalyticalTestRequest
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanReturnProductTestForCorrection);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanViewProductTestCorrectionHistory);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanReturnRawMaterialTestForCorrection);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanViewRawMaterialTestCorrectionHistory);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanReturnPackagingMaterialTestForCorrection);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanViewPackagingMaterialTestCorrectionHistory);
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanReassignProductTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.Testing,
            PermissionKeys.CanStartProductTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanCheckProductTest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanPrintProductAnalyticalTestRequest
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.AnalyticalTestRequestProducts,
            PermissionKeys.CanPrintProductCertificateOfAnalysis
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanViewRawMaterialStps
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanCreateRawMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanEditRawMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanDeleteRawMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanViewPackagingMaterialStps
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanCreatePackagingMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanEditPackagingMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialStp,
            PermissionKeys.CanDeletePackagingMaterialStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanViewRawMaterialSpecifications
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanExportRawMaterialSpecification);
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanCreateRawMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanEditRawMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanDeleteRawMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanViewPackagingMaterialSpecifications
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanExportPackagingMaterialSpecification);
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanCreatePackagingMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanEditPackagingMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialSpecification,
            PermissionKeys.CanDeletePackagingMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanViewRawMaterialArds
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanCreateRawMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanEditRawMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanDeleteRawMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanViewPackagingMaterialArds
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanCreatePackagingMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanEditPackagingMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.MaterialArd,
            PermissionKeys.CanDeletePackagingMaterialArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductStp,
            PermissionKeys.CanViewProductStps
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductStp,
            PermissionKeys.CanCreateProductStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductStp,
            PermissionKeys.CanEditProductStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductStp,
            PermissionKeys.CanDeleteProductStp
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductSpecification,
            PermissionKeys.CanViewProductSpecifications
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification,
            PermissionKeys.CanExportProductSpecification);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanViewProductAssayReporting);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanEditProductAssayReporting);
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductSpecification,
            PermissionKeys.CanCreateProductSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductSpecification,
            PermissionKeys.CanEditProductSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductSpecification,
            PermissionKeys.CanDeleteProductSpecification
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductArd,
            PermissionKeys.CanViewProductArds
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductArd,
            PermissionKeys.CanCreateProductArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductArd,
            PermissionKeys.CanEditProductArd
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.ProductArd,
            PermissionKeys.CanDeleteProductArd
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanViewRoutineTests);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanCreateRoutineTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanAssignRoutineTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanPerformRoutineTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanCheckRoutineTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineTests, PermissionKeys.CanApproveRoutineTest);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineSetup, PermissionKeys.CanViewRoutineSetup);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.RoutineSetup, PermissionKeys.CanManageRoutineSetup);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.MicrobialRequirements, PermissionKeys.CanManageMicrobialRequirements);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.WaterQuality, PermissionKeys.CanViewWaterQuality);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.WaterQuality, PermissionKeys.CanManageWaterQuality);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.QualityCertificates, PermissionKeys.CanViewQualityCertificates);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.QualityCertificates, PermissionKeys.CanGenerateQualityCertificate);

        // Quality Assurance
        foreach (var key in new[] { PermissionKeys.CanViewIssuedBmrRecords, PermissionKeys.CanCorrectIssuedBmrBatchNumber,
                     PermissionKeys.CanRequestIssuedBmrRevision, PermissionKeys.CanApproveIssuedBmrRevision, PermissionKeys.CanPrintIssuedBmrRecords })
            addPermission(PermissionModules.QualityAssurance, "Issued BMR Records", key);
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.IssueBmr,
            PermissionKeys.CanViewIssuedBmrBprs
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.IssueBmr,
            PermissionKeys.CanIssueBmr
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.IssueBmr,
            PermissionKeys.CanIssueBpr
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.AnalyticalTestRequests,
            PermissionKeys.CanViewAnalyticalTestRequests
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.AnalyticalTestRequests,
            PermissionKeys.CanViewAnalyticalTestRequestsForSampling
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.AnalyticalTestRequests,
            PermissionKeys.CanTakeSamples
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyProduct
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyRawMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyPackagingMaterialSpecification
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyProductSpecification
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyRawMaterialArd
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyPackagingMaterialArd
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.Verification,
            PermissionKeys.CanVerifyProductArd
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingApprovals,
            PermissionKeys.CanViewPendingApprovals
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingApprovals,
            PermissionKeys.CanApprovePendingApproval
        );
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanEditProductCertificateOfAnalysis);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestProductCertificateReview);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestProductCertificateCorrection);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanEditRawMaterialCertificateOfAnalysis);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestRawMaterialCertificateReview);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestRawMaterialCertificateCorrection);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanEditPackagingMaterialCertificateOfAnalysis);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestPackagingMaterialCertificateReview);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRequestPackagingMaterialCertificateCorrection);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRejectRawMaterialStockRequisition);
        addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRejectPackagingMaterialStockRequisition);
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingApprovals,
            PermissionKeys.CanRejectPendingApproval
        );
        addPermission(PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingApprovals, PermissionKeys.CanApproveResponse);
        addPermission(PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingApprovals, PermissionKeys.CanRejectResponse);
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.TransferApprovalRights,
            PermissionKeys.CanTransferApprovalRights
        );
        addPermission(PermissionModules.ItSupport, "Approval Rights Transfer", PermissionKeys.CanTransferAnyApprovalRights);

        foreach (var key in new[] { PermissionKeys.CanInitiateOosInvestigation, PermissionKeys.CanViewOosInvestigations,
                     PermissionKeys.CanSubmitOosInvestigation, PermissionKeys.CanReviewOosInvestigation })
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.OosInvestigations, key);

        // Finished Goods Warehouse
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanViewCustomers
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanCreateCustomer
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanEditCustomer
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanDeleteCustomer
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanViewCustomerCreditStatus
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanManageCustomerContracts
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanViewCustomerQuotations
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanCreateCustomerQuotation
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanApproveCustomerQuotation
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.CustomerManagement,
            PermissionKeys.CanConvertCustomerQuotation
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanViewOrder
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanCreateOrders
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanEditOrder
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanDeleteOrder
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanGeneratePackingList
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.PackingList,
            PermissionKeys.CanViewPackingList
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanGenerateProformaInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanViewProformaInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanSendProformaInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanDeleteProformaInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanCreateInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProformaInvoice,
            PermissionKeys.CanDeleteInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Invoice,
            PermissionKeys.CanViewInvoice
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Allocations,
            PermissionKeys.CanViewWaybillForFgw
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Allocations,
            PermissionKeys.CanCreateWaybillForFgw
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Allocations,
            PermissionKeys.CanLoadFinishedGoods
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Allocations,
            PermissionKeys.CanDeliverFinishedGoods
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.Allocations,
            PermissionKeys.CanSendWaybillToCustomer
        );
        addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.Allocations, PermissionKeys.CanEditWaybillForFgw);
        addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.Allocations, PermissionKeys.CanDeleteWaybillForFgw);
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.FinishedGoodsTransferNote,
            PermissionKeys.CanViewFgtn
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.FinishedGoodsTransferNote,
            PermissionKeys.CanApproveFgtn
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ApprovedProducts,
            PermissionKeys.CanViewApprovedProducts
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ApprovedProducts,
            PermissionKeys.CanEditApprovedProductPackingStyle
        );

        // STORES
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanViewItemsInReagentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanCreateNewItemInReagentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanEditItemInReagentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanDeleteItemFromReagentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanViewItemsInEquipmentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanCreateNewItemInEquipmentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanEditItemInEquipmentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanDeleteItemFromEquipmentStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanViewItemsInGeneralStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanCreateNewItemInGeneralStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanEditItemInGeneralStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanDeleteItemFromGeneralStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanViewItemsInItStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanCreateNewItemInItStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanEditItemInItStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.GeneralInventory,
            PermissionKeys.CanDeleteItemFromItStore
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanViewReagentStoreReceivingArea
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanApproveReagentStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanRejectReagentStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanViewEquipmentStoreReceivingArea
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanApproveEquipmentStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanRejectEquipmentStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanViewGeneralStoreReceivingArea
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanApproveGeneralStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanRejectGeneralStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanViewItStoreReceivingArea
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanApproveItStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReceivingArea,
            PermissionKeys.CanRejectItStoreReceivingAreaEntry
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresAvailableStocks,
            PermissionKeys.CanViewReagentStoreAvailableStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresAvailableStocks,
            PermissionKeys.CanViewEquipmentStoreAvailableStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresAvailableStocks,
            PermissionKeys.CanViewGeneralStoreAvailableStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresAvailableStocks,
            PermissionKeys.CanViewItStoreAvailableStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanViewReagentStorePurchaseRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanCreateReagentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanEditReagentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanDeleteReagentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanViewEquipmentStorePurchaseRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanCreateEquipmentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanEditEquipmentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanDeleteEquipmentStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanViewGeneralStorePurchaseRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanCreateGeneralStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanEditGeneralStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanDeleteGeneralStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanViewItStorePurchaseRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanCreateItStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanEditItStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresPurchaseRequisition,
            PermissionKeys.CanDeleteItStorePurchaseRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanViewReagentStoreStockRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanCreateReagentStoreStockRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanViewEquipmentStoreStockRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanCreateEquipmentStoreStockRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanViewGeneralStoreStockRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanCreateGeneralStoreStockRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanViewItStoreStockRequisitions
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresStockRequisition,
            PermissionKeys.CanCreateItStoreStockRequisition
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanViewReagentStoreIssueStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanCreateReagentStoreIssueStock
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanViewEquipmentStoreIssueStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanCreateEquipmentStoreIssueStock
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanViewGeneralStoreIssueStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanCreateGeneralStoreIssueStock
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanViewItStoreIssueStocks
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresIssueStockRequisition,
            PermissionKeys.CanCreateItStoreIssueStock
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanViewReagentStoreReturnItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanCreateReagentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanEditReagentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanDeleteReagentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanViewEquipmentStoreReturnItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanCreateEquipmentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanEditEquipmentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanDeleteEquipmentStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanViewGeneralStoreReturnItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanCreateGeneralStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanEditGeneralStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanDeleteGeneralStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanViewItStoreReturnItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanCreateItStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanEditItStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.StoresReturnMaterials,
            PermissionKeys.CanDeleteItStoreReturnItem
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanViewReagentStoreDamageMissingItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanCreateReagentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanEditReagentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanDeleteReagentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanViewEquipmentStoreDamageMissingItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanCreateEquipmentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanEditEquipmentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanDeleteEquipmentStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanViewGeneralStoreDamageMissingItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanCreateGeneralStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanEditGeneralStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanDeleteGeneralStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanViewItStoreDamageMissingItems
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanCreateItStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanEditItStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.DamageMissingItems,
            PermissionKeys.CanDeleteItStoreDamageMissingItemRecord
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.VendorManagement,
            PermissionKeys.CanViewVendors
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.VendorManagement,
            PermissionKeys.CanCreateVendor
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.VendorManagement,
            PermissionKeys.CanUpdateVendorDetails
        );
        addPermission(
            PermissionModules.Stores,
            PermissionSubmodules.VendorManagement,
            PermissionKeys.CanDeleteVendor
        );

        // Service and Store Procurement
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.PurchaseRequisitionSourcing,
            PermissionKeys.CanViewPurchaseRequisitionsSourcing
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.PurchaseRequisitionSourcing,
            PermissionKeys.CanSourcePurchaseRequisitionSourcing
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationRequests,
            PermissionKeys.CanViewQuotationRequestsForTrustedVendors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationRequests,
            PermissionKeys.CanCreateQuotationRequestForTrustedVendor
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationResponses,
            PermissionKeys.CanViewQuotationResponsesForTrustedVendors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationResponses,
            PermissionKeys.CanSendQuotationResponseForTrustedVendor
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationResponses,
            PermissionKeys.CanViewServicesQuotationResponses
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceQuotationResponses,
            PermissionKeys.CanSendServicesQuotationResponse
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.OpenMarketQuotationSheet,
            PermissionKeys.CanViewOpenMarketQuotationSheets
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.OpenMarketQuotationSheet,
            PermissionKeys.CanCreateOpenMarketQuotationSheet
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanViewTrustedVendorPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanApplyChangesForTrustedVendorPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanViewOpenMarketPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanApplyChangesForOpenMarketPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanViewServicesPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePriceComparison,
            PermissionKeys.CanApplyChangesForServicesPricingSelection
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceProformaRequests,
            PermissionKeys.CanViewServicesProformaRequests
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceProformaRequests,
            PermissionKeys.CanSendServicesProformaRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceProformaResponses,
            PermissionKeys.CanViewServicesProformaResponses
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceProformaResponses,
            PermissionKeys.CanSendServicesProformaResponse
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanViewMemoForTrustedVendors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanCreateMemoForTrustedVendor
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanViewOpenMarketMemo
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanCreateOpenMarketMemo
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanViewServicesMemo
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.Memos,
            PermissionKeys.CanCreateServicesMemo
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanViewPurchaseOrdersForTrustedVendors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanCreatePurchaseOrderForTrustedVendor
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanViewOpenMarketPurchaseOrders
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanCreateOpenMarketPurchaseOrder
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanViewServicesPurchaseOrders
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServicePurchaseOrders,
            PermissionKeys.CanCreateServicesPurchaseOrder
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanViewServices
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanCreateService
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanEditService
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanDeleteService
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanViewContractors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanCreateContractors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanEditContractors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.ServiceManagement,
            PermissionKeys.CanDeleteContractors
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanViewJobRequests
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanCreateJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanEditJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanDeleteJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanAcknowledgeJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanAssignJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanStartJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanCompleteJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobOrders,
            PermissionKeys.CanViewJobOrders
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobOrders,
            PermissionKeys.CanCreateJobOrder
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobOrders,
            PermissionKeys.CanEditJobOrder
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobOrders,
            PermissionKeys.CanDeleteJobOrder
        );

        // Human Resources
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanViewActiveEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanViewInactiveEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanViewNewEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanRegisterEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanUpdateEmployeeInfo
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanUpdateEmployeeEmail
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanDeleteEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeManagement,
            PermissionKeys.CanViewEmployeeDetails
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DepartmentEmployeeExport,
            PermissionKeys.CanViewDepartmentEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DepartmentEmployeeExport,
            PermissionKeys.CanExportDepartmentEmployee
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DesignationManagement,
            PermissionKeys.CanViewDesignation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DesignationManagement,
            PermissionKeys.CanCreateDesignation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DesignationManagement,
            PermissionKeys.CanEditDesignation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.DesignationManagement,
            PermissionKeys.CanDeleteDesignation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanViewStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanCreateStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanEditStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanDeleteStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanViewLeaveRequests
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanCreateLeaveRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanEditLeaveRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanRecallLeave
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanCancelLeave
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanDeleteLeaveRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanViewLeaveType
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanCreateLeaveType
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanEditLeaveType
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanDeleteLeaveType
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanCreateLeaveEntitlement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanEditLeaveEntitlement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveTypeConfiguration,
            PermissionKeys.CanDeleteLeaveEntitlement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.AttendanceReportUpload,
            PermissionKeys.CanViewAttendanceReportUpload
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.AttendanceReportUpload,
            PermissionKeys.CanSubmitAttendanceReportUpload
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.AttendanceReportUpload,
            PermissionKeys.CanCancelAttendanceReportUpload
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.ShiftScheduleReportUpload,
            PermissionKeys.CanViewShiftScheduleReportUpload
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.ShiftScheduleReportUpload,
            PermissionKeys.CanSubmitShiftScheduleReportUpload
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanViewOvertimeManagement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanCreateOvertimeManagement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanEditOvertimeManagement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanDeleteOvertimeManagement
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeCompensation,
            PermissionKeys.CanViewCompensation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.EmployeeCompensation,
            PermissionKeys.CanManageCompensation
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollConfiguration,
            PermissionKeys.CanViewPayrollConfiguration
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollConfiguration,
            PermissionKeys.CanManagePayrollConfiguration
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanViewPayrollRuns
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanCreatePayrollRun
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanApprovePayrollRun
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanCancelPayrollRun
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanViewPayslip
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanViewOwnPayslip
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanExportBankAdvice
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PayrollManagement,
            PermissionKeys.CanExportPayrollRegister
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceCycleManagement,
            PermissionKeys.CanViewPerformanceCycles
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceCycleManagement,
            PermissionKeys.CanManagePerformanceCycles
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.GoalManagement,
            PermissionKeys.CanViewOwnGoals
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.GoalManagement,
            PermissionKeys.CanViewTeamGoals
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.GoalManagement,
            PermissionKeys.CanCreateGoal
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.GoalManagement,
            PermissionKeys.CanEditGoal
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.GoalManagement,
            PermissionKeys.CanDeleteGoal
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceReviewManagement,
            PermissionKeys.CanViewPerformanceReviews
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceReviewManagement,
            PermissionKeys.CanSubmitSelfAssessment
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceReviewManagement,
            PermissionKeys.CanSubmitManagerReview
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.PerformanceReviewManagement,
            PermissionKeys.CanApprovePerformanceReview
        );

        // IT Support
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanViewActiveUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanCreateUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanEditUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanBlockUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanViewInactiveUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanUnblockUser
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.UserManagement,
            PermissionKeys.CanSwitchDepartment
        );
        addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanAssignUserRole);
        addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanDeleteUser);
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.AuditTrail,
            PermissionKeys.CanViewAuditTrail
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManageRoles,
            PermissionKeys.CanViewRoles
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManageRoles,
            PermissionKeys.CanCreateRole
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManageRoles,
            PermissionKeys.CanEditRole
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManageRoles,
            PermissionKeys.CanDeleteRole
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManagePermissions,
            PermissionKeys.CanViewPermissions
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManagePermissions,
            PermissionKeys.CanUpdateExistingPermission
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.ManagePermissions,
            PermissionKeys.CanResetPermission
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.SupportTickets,
            PermissionKeys.CanCreateTicket
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.SupportTickets,
            PermissionKeys.CanViewAllTickets
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.SupportTickets,
            PermissionKeys.CanAssignTicket
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.SupportTickets,
            PermissionKeys.CanCloseTicket
        );
        addPermission(
            PermissionModules.ItSupport,
            PermissionSubmodules.SupportTickets,
            PermissionKeys.CanCommentOnTicket
        );

        // Settings
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ProductsCategory,
            PermissionKeys.CanViewProductCategories
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ProductsCategory,
            PermissionKeys.CanCreateProductCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ProductsCategory,
            PermissionKeys.CanEditProductCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ProductsCategory,
            PermissionKeys.CanDeleteProductCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewRawCategories
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateRawCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditRawCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteRawCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewPackageCategories
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreatePackageCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditPackageCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeletePackageCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewItemCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateItemCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditItemCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteItemCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewEquipmentCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateEquipmentCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditEquipmentCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteEquipmentCategory
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewMaterialTypes
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateMaterialType
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditMaterialType
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteMaterialType
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewUomStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateUomStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditUomStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteUomStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewPackageStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreatePackageStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditPackageStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeletePackageStyle
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanViewProductState
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanCreateProductState
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanEditProductState
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Products,
            PermissionKeys.CanDeleteProductState
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanViewResources
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanCreateResource
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanEditResource
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanDeleteResource
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanViewOperations
        );
        addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanViewDepartmentProductionBoardOrder);
        addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanReorderDepartmentProductionBoard);
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanCreateOperation
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanEditOperation
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanDeleteOperation
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanLinkOperationDepartment
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanUnlinkOperationDepartment
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanViewWorkCenters
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanCreateWorkCenter
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanEditWorkCenter
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Procedures,
            PermissionKeys.CanDeleteWorkCenter
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Site,
            PermissionKeys.CanViewSite
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Site,
            PermissionKeys.CanCreateSite
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Site,
            PermissionKeys.CanEditWorkSite
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Site,
            PermissionKeys.CanDeleteSite
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CountryAddress,
            PermissionKeys.CanViewCountries
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CountryAddress,
            PermissionKeys.CanCreateCountries
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CountryAddress,
            PermissionKeys.CanEditCountries
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CountryAddress,
            PermissionKeys.CanDeleteCountries
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.TermsOfPayment,
            PermissionKeys.CanViewPaymentTerms
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.TermsOfPayment,
            PermissionKeys.CanCreatePaymentTerm
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.TermsOfPayment,
            PermissionKeys.CanEditPaymentTerm
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.TermsOfPayment,
            PermissionKeys.CanDeletePaymentTerm
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DeliveryMode,
            PermissionKeys.CanViewDeliveryModes
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DeliveryMode,
            PermissionKeys.CanCreateDeliveryMode
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DeliveryMode,
            PermissionKeys.CanEditDeliveryMode
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DeliveryMode,
            PermissionKeys.CanDeleteDeliveryMode
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Charges,
            PermissionKeys.CanViewCharges
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Charges,
            PermissionKeys.CanCreateCharges
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Charges,
            PermissionKeys.CanEditCharges
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Charges,
            PermissionKeys.CanDeleteCharges
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Threshold,
            PermissionKeys.CanViewThreshold
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Threshold,
            PermissionKeys.CanCreateThreshold
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Threshold,
            PermissionKeys.CanEditThreshold
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Threshold,
            PermissionKeys.CanDeleteThreshold
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CodeSettings,
            PermissionKeys.CanViewCodeSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CodeSettings,
            PermissionKeys.CanCreateNewCodes
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CodeSettings,
            PermissionKeys.CanEditCodeSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.CodeSettings,
            PermissionKeys.CanDeleteCodeSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanViewQuestions
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanCreateQuestions
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanEditQuestions
        );
        addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanRepairQuestionFormula);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.Testing, PermissionKeys.CanRepairTestFormula);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.Testing, PermissionKeys.CanViewTemplateRepair);
        addPermission(PermissionModules.QualityControl, PermissionSubmodules.Testing, PermissionKeys.CanApplyTemplateRepair);
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanDeleteQuestions
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanApplyFormulaMigration
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanViewReviewFormulaRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanReviewFormulaRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanViewApproveFormulaRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanApproveFormulaRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanReturnFormulaRevision
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.Testing,
            PermissionKeys.CanExecuteFormulaResponse
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanReviewFormRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanApproveFormRevision
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanViewTemplate
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanCreateTemplate
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanEditTemplate
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.WorkflowBuilder,
            PermissionKeys.CanDeleteTemplate
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.AlertsNotifications,
            PermissionKeys.CanViewAlerts
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.AlertsNotifications,
            PermissionKeys.CanCreateNewAlerts
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.AlertsNotifications,
            PermissionKeys.CanEditAlerts
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.AlertsNotifications,
            PermissionKeys.CanEnableDisableAlerts
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.AlertsNotifications,
            PermissionKeys.CanDeleteAlerts
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Approvals,
            PermissionKeys.CanViewApprovals
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Approvals,
            PermissionKeys.CanCreateNewApproval
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Approvals,
            PermissionKeys.CanEditApprovalWorkflow
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.Approvals,
            PermissionKeys.CanDeleteApprovals
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.SignatureSettings,
            PermissionKeys.CanViewSignatureSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.SignatureSettings,
            PermissionKeys.CanCreateSignatureSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ChangePassword,
            PermissionKeys.CanViewChangePassword
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.ChangePassword,
            PermissionKeys.CanChangePassword
        );

        // Inventory Management
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Manufacturers,
            PermissionKeys.CanViewManufacturers
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Manufacturers,
            PermissionKeys.CanCreateManufacturer
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Manufacturers,
            PermissionKeys.CanUpdateManufacturerDetails
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Manufacturers,
            PermissionKeys.CanDeleteManufacturer
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanViewVendors
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanCreateVendor
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanUpdateVendorDetails
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanDeleteVendor
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanViewSupplierCertifications
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanManageSupplierCertifications
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanViewSupplierPerformance
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanManageSupplierContracts
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Suppliers,
            PermissionKeys.CanViewSupplierSpend
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Warehouses,
            PermissionKeys.CanViewWarehouses
        );
        addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Warehouses, PermissionKeys.CanCreateWarehouse);
        addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Warehouses, PermissionKeys.CanEditWarehouse);
        addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Warehouses, PermissionKeys.CanDeleteWarehouse);
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Locations,
            PermissionKeys.CanViewLocations
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Locations,
            PermissionKeys.CanAddNewLocation
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Locations,
            PermissionKeys.CanEditLocation
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Locations,
            PermissionKeys.CanDeleteLocation
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Racks,
            PermissionKeys.CanViewRacks
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Racks,
            PermissionKeys.CanAddNewRack
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Racks,
            PermissionKeys.CanEditRack
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Racks,
            PermissionKeys.CanDeleteRack
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Shelves,
            PermissionKeys.CanViewShelves
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Shelves,
            PermissionKeys.CanAddNewShelf
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Shelves,
            PermissionKeys.CanEditShelf
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Shelves,
            PermissionKeys.CanDeleteShelf
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Equipment,
            PermissionKeys.CanViewEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Equipment,
            PermissionKeys.CanCreateEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Equipment,
            PermissionKeys.CanEditEquipmentDetails
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.Equipment,
            PermissionKeys.CanDeleteEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanViewQcEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanCreateQcEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanEditQcEquipmentDetails
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanDeleteQcEquipment
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanViewQcEquipmentCalibration
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.QcEquipment,
            PermissionKeys.CanUpdateQcEquipmentCalibration
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.InstrumentCalibration,
            PermissionKeys.CanViewInstrumentCalibration
        );
        addPermission(
            PermissionModules.QualityControl,
            PermissionSubmodules.InstrumentCalibration,
            PermissionKeys.CanUpdateInstrumentCalibration
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.UnitOfMeasure,
            PermissionKeys.CanViewUnitOfMeasure
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.UnitOfMeasure,
            PermissionKeys.CanCreateUnitOfMeasure
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.UnitOfMeasure,
            PermissionKeys.CanEditUnitOfMeasure
        );
        addPermission(
            PermissionModules.InventoryManagement,
            PermissionSubmodules.UnitOfMeasure,
            PermissionKeys.CanDeleteUnitOfMeasure
        );

        // Organizational Structure
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Departments,
            PermissionKeys.CanViewDepartments
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Departments,
            PermissionKeys.CanCreateNewDepartment
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Departments,
            PermissionKeys.CanEditDepartment
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Departments,
            PermissionKeys.CanDeleteDepartment
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.WorkingDays,
            PermissionKeys.CanViewWorkingDays
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.WorkingDays,
            PermissionKeys.CanCreateWorkingDays
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.WorkingDays,
            PermissionKeys.CanResetWorkingDays
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Holidays,
            PermissionKeys.CanViewHolidays
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Holidays,
            PermissionKeys.CanCreateHoliday
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Holidays,
            PermissionKeys.CanEditHoliday
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.Holidays,
            PermissionKeys.CanDeleteHoliday
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsType,
            PermissionKeys.CanViewShiftTypes
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsType,
            PermissionKeys.CanCreateShiftType
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsType,
            PermissionKeys.CanEditShiftType
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsType,
            PermissionKeys.CanDeleteShiftType
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsSchedule,
            PermissionKeys.CanViewShiftSchedule
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsSchedule,
            PermissionKeys.CanCreateShiftSchedule
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsSchedule,
            PermissionKeys.CanEditShiftSchedule
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftsSchedule,
            PermissionKeys.CanDeleteShiftSchedule
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftCategory,
            PermissionKeys.CanViewShiftCategories
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftCategory,
            PermissionKeys.CanCreateShiftCategory
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftCategory,
            PermissionKeys.CanEditShiftCategory
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.ShiftCategory,
            PermissionKeys.CanDeleteShiftCategory
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.WorkingHoursPolicy,
            PermissionKeys.CanViewWorkingHoursPolicy
        );
        addPermission(
            PermissionModules.OrganizationalStructure,
            PermissionSubmodules.WorkingHoursPolicy,
            PermissionKeys.CanManageWorkingHoursPolicy
        );

        // Reports
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewHumanResourcesReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewInventoryReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewQcAndQaReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewWarehouseReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewStoreAndInventoryReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewProcurementReport
        );
        addPermission(
            PermissionModules.Reports,
            PermissionSubmodules.Reports,
            PermissionKeys.CanViewFgtnReport
        );
        addPermission(PermissionModules.Reports, PermissionSubmodules.SupplierReports, PermissionKeys.CanViewApprovedManufacturerListReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.SupplierReports, PermissionKeys.CanViewSupplierMappingSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.SupplierReports, PermissionKeys.CanViewSupplierMappingDetailReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PurchasingReports, PermissionKeys.CanViewPurchaseOrdersReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PurchasingReports, PermissionKeys.CanViewInvoicedPurchaseOrdersReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PurchasingReports, PermissionKeys.CanViewShipmentsReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewMaterialListReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewMaterialsPerDepartmentReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewMaterialStockSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewMaterialStockDetailReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewStockCountSheetReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewExpiredStockReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.MaterialReports, PermissionKeys.CanViewInventoryValuationReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewMonthlyProductionScheduleReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewDailyProductionScheduleReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewProductReadinessReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewBmrIssuedReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewBmrIssuedByProductReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewStockIssuedReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewProductionMonthlyReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewProductionScheduleSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductionReports, PermissionKeys.CanViewProductionBatchValueReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewProductListReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewDepartmentProductListReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewProductStockSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewProductStockDetailReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewFinishedGoodsTransferSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewFinishedGoodsTransferDetailReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.ProductReports, PermissionKeys.CanViewInvoicedProductsReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.GeneralStoreReports, PermissionKeys.CanViewStoreItemsReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.GeneralStoreReports, PermissionKeys.CanViewStoreStockReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.GeneralStoreReports, PermissionKeys.CanViewVendorItemMappingSummaryReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.GeneralStoreReports, PermissionKeys.CanViewVendorItemMappingDetailReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewEmployeeMasterListReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewStaffGradeReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewStaffTotalReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewStaffGenderRatioReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewNewHiresAndExitsReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewStaffTurnoverReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewLeaveOverviewReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewDailyAttendanceReport);
        addPermission(PermissionModules.Reports, PermissionSubmodules.PeopleReports, PermissionKeys.CanViewDepartmentAttendanceReport);

        // Audit
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanViewQualityAudits
        );
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanCreateQualityAudit
        );
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanConductQualityAudit
        );
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanRaiseCorrectiveAction
        );
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanReviewCorrectiveAction
        );
        addPermission(
            PermissionModules.Audit,
            PermissionSubmodules.QualityAudits,
            PermissionKeys.CanCloseQualityAudit
        );

        // Research & Development
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndProjects,
            PermissionKeys.CanViewRndProjects
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndProjects,
            PermissionKeys.CanCreateRndProject
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndProjects,
            PermissionKeys.CanEditRndProject
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndProjects,
            PermissionKeys.CanUpdateRndProjectStatus
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndProjects,
            PermissionKeys.CanDeleteRndProject
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndFormulations,
            PermissionKeys.CanViewRndFormulations
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndFormulaBuilder,
            PermissionKeys.CanViewFormulaBuilder
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndFormulations,
            PermissionKeys.CanCreateRndFormulation
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndFormulations,
            PermissionKeys.CanEditRndFormulation
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTrialBatches,
            PermissionKeys.CanViewRndTrialBatches
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTrialBatches,
            PermissionKeys.CanCreateRndTrialBatch
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTrialBatches,
            PermissionKeys.CanEditRndTrialBatch
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTrialBatches,
            PermissionKeys.CanIssueTrialRequisition
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndAnalyticalMethods,
            PermissionKeys.CanViewRndAnalyticalMethods
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndAnalyticalMethods,
            PermissionKeys.CanCreateRndAnalyticalMethod
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndAnalyticalMethods,
            PermissionKeys.CanEditRndAnalyticalMethod
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndAnalyticalMethods,
            PermissionKeys.CanTransferRndAnalyticalMethod
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndStabilityStudies,
            PermissionKeys.CanViewRndStabilityStudies
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndStabilityStudies,
            PermissionKeys.CanCreateRndStabilityStudy
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndStabilityStudies,
            PermissionKeys.CanEditRndStabilityStudy
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndStabilityStudies,
            PermissionKeys.CanManageRndStabilityChambers
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTechnologyTransfers,
            PermissionKeys.CanViewRndTechnologyTransfers
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTechnologyTransfers,
            PermissionKeys.CanCreateRndTechnologyTransfer
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTechnologyTransfers,
            PermissionKeys.CanEditRndTechnologyTransfer
        );
        addPermission(
            PermissionModules.ResearchAndDevelopment,
            PermissionSubmodules.RndTechnologyTransfers,
            PermissionKeys.CanPromoteRndTechnologyTransfer
        );

        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DocumentReviews,
            PermissionKeys.CanViewDocumentReviewSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DocumentReviews,
            PermissionKeys.CanEditDocumentReviewSettings
        );
        addPermission(
            PermissionModules.Settings,
            PermissionSubmodules.DocumentReviews,
            PermissionKeys.CanDeleteDocumentReviewSettings
        );
        addPermission(
            PermissionModules.QualityAssurance,
            PermissionSubmodules.PendingReviews,
            PermissionKeys.CanViewPendingReviews
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanReturnForeignPurchaseOrderCheck
        );
        addPermission(
            PermissionModules.Procurement,
            PermissionSubmodules.CheckPurchaseOrderPage,
            PermissionKeys.CanReturnLocalPurchaseOrderCheck
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanEditRawMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.CreatePurchaseRequisitions,
            PermissionKeys.CanEditPackageMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReviewRawMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReviewPackageMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReturnRawMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReturnPackageMaterialPurchaseRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReviewRawMaterialStockRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReviewPackageMaterialStockRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReturnRawMaterialStockRequisition
        );
        addPermission(
            PermissionModules.Production,
            PermissionSubmodules.RequisitionReviews,
            PermissionKeys.CanReturnPackageMaterialStockRequisition
        );

        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanReviewBillingSheet
        );
        addPermission(
            PermissionModules.Logistics,
            PermissionSubmodules.BillingSheet,
            PermissionKeys.CanReturnBillingSheet
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanReviewStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.StaffRequisition,
            PermissionKeys.CanReturnStaffRequisition
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanReviewOvertimeRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.OvertimeManagement,
            PermissionKeys.CanReturnOvertimeRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanReviewLeaveRequest
        );
        addPermission(
            PermissionModules.HumanResources,
            PermissionSubmodules.LeaveManagement,
            PermissionKeys.CanReturnLeaveRequest
        );

        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanReviewJobRequest
        );
        addPermission(
            PermissionModules.ServiceAndStoreProcurement,
            PermissionSubmodules.JobRequests,
            PermissionKeys.CanReturnJobRequest
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanReviewPackingList
        );
        addPermission(
            PermissionModules.FinishedGoodsWarehouse,
            PermissionSubmodules.ProductionOrders,
            PermissionKeys.CanReturnPackingList
        );

        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanCheckRawMaterialIssue
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanCheckPackagingMaterialIssue
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanReturnRawMaterialIssue
        );
        addPermission(
            PermissionModules.Warehouse,
            PermissionSubmodules.IssueStockRequisitions,
            PermissionKeys.CanReturnPackagingMaterialIssue
        );

        permissions.AddRange(FullProcedurePermissionCatalog.Generate());
        permissions.AddRange(QcWorksheetPermissionCatalog.Generate());
        return permissions;
    }
}
