using System.Text.RegularExpressions;
using DOMAIN.Entities.Permissions;
namespace APP.Utils;

public static class PermissionModules
{
    public const string Procurement = "Procurement";
    public const string Logistics = "Logistics";
    public const string Warehouse = "Warehouse";
    public const string Production = "Production";
    public const string QualityControl = "QualityControl";
    public const string QualityAssurance = "QualityAssurance";
    public const string FinishedGoodsWarehouse = "FinishedGoodsWarehouse";
    public const string HumanResources = "HumanResources";
    public const string ItSupport = "ITSupport";
    public const string Settings = "Settings";
    public const string InventoryManagement = "InventoryManagement";
    public const string OrganizationalStructure = "OrganizationalStructure";
}

public static class PermissionSubmodules
{
    // Procurement
    public const string PurchaseRequisition = "PurchaseRequisition";
    public const string QuotationsRequest = "QuotationsRequest";
    public const string QuotationsResponses = "QuotationsResponses";
    public const string PriceComparison = "PriceComparison";
    public const string ProformaRequest = "ProformaRequest";
    public const string ProformaResponses = "ProformaResponses";
    public const string CreatePurchaseOrdersPage = "CreatePurchaseOrdersPage";
    public const string PurchaseOrderListPage = "PurchaseOrderListPage";
    public const string MaterialDistribution = "MaterialDistribution";

    // Logistics
    public const string ShipmentInvoice = "ShipmentInvoice";
    public const string ShipmentDocument = "ShipmentDocument";
    public const string BillingSheet = "BillingSheet";
    public const string Waybill = "Waybill";
    public const string AvailableStock = "AvailableStock";

    // Warehouse
    public const string ReceivingArea = "ReceivingArea";
    public const string QuarantineAreaGrn = "QuarantineAreaGrn";
    public const string LinkedMaterials = "LinkedMaterials";
    public const string UnlinkedMaterials = "UnlinkedMaterials";
    public const string Materials = "Materials";
    public const string ApprovedMaterials = "ApprovedMaterials";
    public const string RejectedMaterials = "RejectedMaterials";
    public const string IssueStockRequisitions = "IssueStockRequisitions";
    public const string StockTransferIssues = "StockTransferIssues";
    public const string LocationChartRecord = "LocationChartRecord";

    // Production
    public const string Requisitions = "Requisitions";
    public const string CreatePurchaseRequisitions = "CreatePurchaseRequisitions";
    public const string Planning = "Planning";
    public const string StockTransferRequests = "StockTransferRequests";
    public const string ProductSchedule = "ProductSchedule";

    // Quality Control
    public const string GoodsReceiptNote = "GoodsReceiptNote";
    public const string AnalyticalTestRequestProducts = "AnalyticalTestRequestProducts";
    public const string MaterialStp = "MaterialStp";
    public const string MaterialSpecification = "MaterialSpecification";
    public const string MaterialArd = "MaterialArd";
    public const string ProductStp = "ProductStp";
    public const string ProductArd = "ProductArd";
    public const string ProductSpecification = "ProductSpecification";

    // Quality Assurance
    public const string IssueBmr = "IssueBmr";
    public const string AnalyticalTestRequests = "AnalyticalTestRequests";
    public const string PendingApprovals = "PendingApprovals";

    // Finished Goods Warehouse
    public const string CustomerManagement = "CustomerManagement";
    public const string ProductionOrders = "ProductionOrders";
    public const string PackingList = "PackingList";
    public const string ProformaInvoice = "ProformaInvoice";
    public const string Invoice = "Invoice";
    public const string WaybillFgw = "WaybillFgw";

    // Human Resources
    public const string EmployeeManagement = "EmployeeManagement";
    public const string DepartmentEmployeeExport = "DepartmentEmployeeExport";
    public const string DesignationManagement = "DesignationManagement";
    public const string LeaveManagement = "LeaveManagement";
    public const string LeaveTypeConfiguration = "LeaveTypeConfiguration";
    public const string StaffRequisition = "StaffRequisition";
    public const string AttendanceReportUpload = "AttendanceReportUpload";
    public const string ShiftScheduleReportUpload = "ShiftScheduleReportUpload";
    public const string OvertimeManagement = "OvertimeManagement";

    // IT SUPPORT
    public const string UserManagement = "UserManagement";
    public const string AuditTrail = "AuditTrail";
    public const string AccessManagement = "AccessManagement";
    public const string ManageRoles = "ManageRoles";
    public const string ManagePermissions = "ManagePermissions";

    // Settings
    public const string SystemSettings = "SystemSettings";
    public const string ProductsCategory = "ProductsCategory";
    public const string Products = "Products";
    public const string Procedures = "Procedures";
    public const string CountryAddress = "CountryAddress";
    public const string Schedules = "Schedules";
    public const string Payments = "Payments";
    public const string TermsOfPayment = "TermsOfPayment";
    public const string DeliveryMode = "DeliveryMode";
    public const string Charges = "Charges";
    public const string CodeSettings = "CodeSettings";
    public const string WorkflowBuilder = "WorkflowBuilder";
    public const string AlertsNotifications = "AlertsNotifications";
    public const string UserAccountManagement = "UserAccountManagement";
    public const string Approvals = "Approvals";
    public const string SignatureSettings = "SignatureSettings";
    public const string ChangePassword = "ChangePassword";

    // Inventory Management
    public const string Manufacturers = "Manufacturers";
    public const string Suppliers = "Suppliers";
    public const string Warehouses = "Warehouses";
    public const string Locations = "Locations";
    public const string Racks = "Racks";
    public const string Shelves = "Shelves";
    public const string Equipment = "Equipment";
    public const string UnitOfMeasure = "UnitOfMeasure";

    // Organizational Structure
    public const string Departments = "Departments";
    public const string WorkingDays = "WorkingDays";
    public const string Holidays = "Holidays";
    public const string ShiftsType = "ShiftsType";
    public const string ShiftsSchedule = "ShiftsSchedule";
}

public static class PermissionKeys
{
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
    public const string CanApplyChangesForForeignVendorPricingSelection = "CanApplyChangesForForeignVendorPricingSelection";
    public const string CanViewLocalVendorPricing = "CanViewLocalVendorPricing";
    public const string CanApplyChangesForLocalVendorPricingSelection = "CanApplyChangesForLocalVendorPricingSelection";
    public const string CanViewForeignProformaRequest = "CanViewForeignProformaRequest";
    public const string CanSendForeignProformaRequest = "CanSendForeignProformaRequest";
    public const string CanViewLocalProformaRequest = "CanViewLocalProformaRequest";
    public const string CanSendLocalProformaRequest = "CanSendLocalProformaRequest";
    public const string CanViewForeignProformaInvoiceSubmissions = "CanViewForeignProformaInvoiceSubmissions";
    public const string CanUploadForeignProformaInvoice = "CanUploadForeignProformaInvoice";
    public const string CanViewLocalProformaInvoiceSubmissions = "CanViewLocalProformaInvoiceSubmissions";
    public const string CanUploadLocalProformaInvoice = "CanUploadLocalProformaInvoice";
    public const string CanViewForeignPurchaseOrder = "CanViewForeignPurchaseOrder";
    public const string CanCreateForeignPurchaseOrder = "CanCreateForeignPurchaseOrder";
    public const string CanViewLocalPurchaseOrder = "CanViewLocalPurchaseOrder";
    public const string CanCreateLocalPurchaseOrder = "CanCreateLocalPurchaseOrder";
    public const string CanReviseForeignPurchaseOrder = "CanReviseForeignPurchaseOrder";
    public const string CanReviseLocalPurchaseOrder = "CanReviseLocalPurchaseOrder";
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
    public const string CanCreateBillingSheet = "CanCreateBillingSheet";
    public const string CanViewBillingSheet = "CanViewBillingSheet";
    public const string CanEditBillingSheet = "CanEditBillingSheet";
    public const string CanCreateWaybill = "CanCreateWaybill";
    public const string CanViewWaybill = "CanViewWaybill";
    public const string CanChangeWaybillStatus = "CanChangeWaybillStatus";
    public const string CanViewRawMaterialStock = "CanViewRawMaterialStock";
    public const string CanFilterRawMaterialStockByDepartment = "CanFilterRawMaterialStockByDepartment";
    public const string CanViewPackingMaterialStock = "CanViewPackingMaterialStock";
    public const string CanFilterPackingMaterialStockByDepartment = "CanFilterPackingMaterialStockByDepartment";

    // Warehouse
    public const string CanViewRawMaterialsItems = "CanViewRawMaterialsItems";
    public const string CanCreateChecklistForRawMaterials = "CanCreateChecklistForRawMaterials";
    public const string CanCreateGrnForRawMaterialsChecklistedItems = "CanCreateGrnForRawMaterialsChecklistedItems";
    public const string CanViewPackagingMaterialsItems = "CanViewPackagingMaterialsItems";
    public const string CanCreateChecklistForPackagingMaterials = "CanCreateChecklistForPackagingMaterials";
    public const string CanCreateGrnForPackagingMaterialsChecklistedItems = "CanCreateGrnForPackagingMaterialsChecklistedItems";
    public const string CanViewQuarantineRawMaterials = "CanViewQuarantineRawMaterials";
    public const string CanAssignRawMaterialsStockToShelves = "CanAssignRawMaterialsStockToShelves";
    public const string CanViewQuarantinePackagingMaterials = "CanViewQuarantinePackagingMaterials";
    public const string CanAssignPackagingMaterialsStockToShelves = "CanAssignPackagingMaterialsStockToShelves";
    public const string CanViewLinkedRawMaterials = "CanViewLinkedRawMaterials";
    public const string CanUnlinkRawMaterials = "CanUnlinkRawMaterials";
    public const string CanViewLinkedPackagingMaterials = "CanViewLinkedPackagingMaterials";
    public const string CanUnlinkPackagingMaterials = "CanUnlinkPackagingMaterials";
    public const string CanViewUnlinkedRawMaterials = "CanViewUnlinkedRawMaterials";
    public const string CanLinkRawMaterials = "CanLinkRawMaterials";
    public const string CanViewUnlinkedPackagingMaterials = "CanViewUnlinkedPackagingMaterials";
    public const string CanLinkPackagingMaterials = "CanLinkPackagingMaterials";
    public const string CanViewRawMaterials = "CanViewRawMaterials";
    public const string CanCreateNewRawMaterials = "CanCreateNewRawMaterials";
    public const string CanEditRawMaterials = "CanEditRawMaterials";
    public const string CanDeleteRawMaterials = "CanDeleteRawMaterials";
    public const string CanViewPackagingMaterials = "CanViewPackagingMaterials";
    public const string CanCreateNewPackagingMaterials = "CanCreateNewPackagingMaterials";
    public const string CanEditPackagingMaterials = "CanEditPackagingMaterials";
    public const string CanDeletePackagingMaterials = "CanDeletePackagingMaterials";
    public const string CanViewApprovedRawMaterials = "CanViewApprovedRawMaterials";
    public const string CanViewApprovedPackagingMaterials = "CanViewApprovedPackagingMaterials";
    public const string CanViewRejectedRawMaterials = "CanViewRejectedRawMaterials";
    public const string CanViewRejectedPackingMaterials = "CanViewRejectedPackingMaterials";
    public const string CanViewRawMaterialRequisitions = "CanViewRawMaterialRequisitions";
    public const string CanIssueRawMaterialRequisitions = "CanIssueRawMaterialRequisitions";
    public const string CanViewPackagingMaterialRequisitions = "CanViewPackagingMaterialRequisitions";
    public const string CanIssuePackagingMaterialRequisitions = "CanIssuePackagingMaterialRequisitions";
    public const string CanViewRawMaterialTransferList = "CanViewRawMaterialTransferList";
    public const string CanIssueRawMaterialStockTransfers = "CanIssueRawMaterialStockTransfers";
    public const string CanViewPackagingMaterialTransferList = "CanViewPackagingMaterialTransferList";
    public const string CanIssuePackagingMaterialStockTransfers = "CanIssuePackagingMaterialStockTransfers";
    public const string CanViewRawMaterialLocationChartList = "CanViewRawMaterialLocationChartList";
    public const string CanReassignRawMaterialStock = "CanReassignRawMaterialStock";
    public const string CanViewPackagingMaterialLocationChartList = "CanViewPackagingMaterialLocationChartList";
    public const string CanReassignPackagingMaterialStock = "CanReassignPackagingMaterialStock";

    // Production
    public const string CanViewMaterialRequisitions = "CanViewMaterialRequisitions";
    public const string CanViewMaterialRequisitionDetailsPage = "CanViewMaterialRequisitionDetailsPage";
    public const string CanViewRawMaterialRequisitionsForCreation = "CanViewRawMaterialRequisitionsForCreation";
    public const string CanCreateRawMaterialRequisitions = "CanCreateRawMaterialRequisitions";
    public const string CanViewPackageMaterialRequisitionsForCreation = "CanViewPackageMaterialRequisitionsForCreation";
    public const string CanCreatePackageMaterialRequisitions = "CanCreatePackageMaterialRequisitions";
    public const string CanViewPlannedProducts = "CanViewPlannedProducts";
    public const string CanCreateNewProductionPlan = "CanCreateNewProductionPlan";
    public const string CanEditProductionPlan = "CanEditProductionPlan";
    public const string CanViewIncomingStockTransferRequests = "CanViewIncomingStockTransferRequests";
    public const string CanApproveOrRejectIncomingStockTransferRequest = "CanApproveOrRejectIncomingStockTransferRequest";
    public const string CanViewOutgoingStockTransferRequests = "CanViewOutgoingStockTransferRequests";
    public const string CanViewProductSchedules = "CanViewProductSchedules";
    public const string CanCreateProductSchedule = "CanCreateProductSchedule";

    // Quality Control
    public const string CanViewRawMaterialGoodsReceiptNotes = "CanViewRawMaterialGoodsReceiptNotes";
    public const string CanTakeRawMaterialSample = "CanTakeRawMaterialSample";
    public const string CanStartRawMaterialTest = "CanStartRawMaterialTest";
    public const string CanCheckRawMaterialTestResult = "CanCheckRawMaterialTestResult";
    public const string CanViewPackagingMaterialGoodsReceiptNotes = "CanViewPackagingMaterialGoodsReceiptNotes";
    public const string CanTakePackagingMaterialSample = "CanTakePackagingMaterialSample";
    public const string CanStartPackagingMaterialTest = "CanStartPackagingMaterialTest";
    public const string CanCheckPackagingMaterialTestResult = "CanCheckPackagingMaterialTestResult";
    public const string CanViewProductAnalyticalTestRequests = "CanViewProductAnalyticalTestRequests";
    public const string CanAcknowledgeSampleTaken = "CanAcknowledgeSampleTaken";
    public const string CanStartProductTest = "CanStartProductTest";
    public const string CanCheckProductTest = "CanCheckProductTest";
    public const string CanViewRawMaterialStps = "CanViewRawMaterialStps";
    public const string CanCreateRawMaterialStp = "CanCreateRawMaterialStp";
    public const string CanEditRawMaterialStp = "CanEditRawMaterialStp";
    public const string CanDeleteRawMaterialStp = "CanDeleteRawMaterialStp";
    public const string CanViewPackagingMaterialStps = "CanViewPackagingMaterialStps";
    public const string CanCreatePackagingMaterialStp = "CanCreatePackagingMaterialStp";
    public const string CanEditPackagingMaterialStp = "CanEditPackagingMaterialStp";
    public const string CanDeletePackagingMaterialStp = "CanDeletePackagingMaterialStp";
    public const string CanViewRawMaterialSpecifications = "CanViewRawMaterialSpecifications";
    public const string CanCreateRawMaterialSpecification = "CanCreateRawMaterialSpecification";
    public const string CanEditRawMaterialSpecification = "CanEditRawMaterialSpecification";
    public const string CanDeleteRawMaterialSpecification = "CanDeleteRawMaterialSpecification";
    public const string CanViewPackagingMaterialSpecifications = "CanViewPackagingMaterialSpecifications";
    public const string CanCreatePackagingMaterialSpecification = "CanCreatePackagingMaterialSpecification";
    public const string CanEditPackagingMaterialSpecification = "CanEditPackagingMaterialSpecification";
    public const string CanDeletePackagingMaterialSpecification = "CanDeletePackagingMaterialSpecification";
    public const string CanViewRawMaterialArds = "CanViewRawMaterialArds";
    public const string CanCreateRawMaterialArd = "CanCreateRawMaterialArd";
    public const string CanEditRawMaterialArd = "CanEditRawMaterialArd";
    public const string CanDeleteRawMaterialArd = "CanDeleteRawMaterialArd";
    public const string CanViewPackagingMaterialArds = "CanViewPackagingMaterialArds";
    public const string CanCreatePackagingMaterialArd = "CanCreatePackagingMaterialArd";
    public const string CanEditPackagingMaterialArd = "CanEditPackagingMaterialArd";
    public const string CanDeletePackagingMaterialArd = "CanDeletePackagingMaterialArd";
    public const string CanViewProductStps = "CanViewProductStps";
    public const string CanCreateProductStp = "CanCreateProductStp";
    public const string CanEditProductStp = "CanEditProductStp";
    public const string CanDeleteProductStp = "CanDeleteProductStp";
    public const string CanViewProductArds = "CanViewProductArds";
    public const string CanCreateProductArd = "CanCreateProductArd";
    public const string CanEditProductArd = "CanEditProductArd";
    public const string CanDeleteProductArd = "CanDeleteProductArd";
    public const string CanViewProductSpecifications = "CanViewProductSpecifications";
    public const string CanCreateProductSpecification = "CanCreateProductSpecification";
    public const string CanEditProductSpecification = "CanEditProductSpecification";
    public const string CanDeleteProductSpecification = "CanDeleteProductSpecification";

    // Quality Assurance
    public const string CanViewIssuedBmrBprs = "CanViewIssuedBmrBprs";
    public const string CanIssueBmr = "CanIssueBmr";
    public const string CanViewAnalyticalTestRequests = "CanViewAnalyticalTestRequests";
    public const string CanTakeSamples = "CanTakeSamples";
    public const string CanViewPendingApprovals = "CanViewPendingApprovals";
    public const string CanApprovePendingApproval = "CanApprovePendingApproval";
    public const string CanRejectPendingApproval = "CanRejectPendingApproval";

    // Finished Goods Warehouse
    public const string CanViewCustomers = "CanViewCustomers";
    public const string CanCreateCustomer = "CanCreateCustomer";
    public const string CanEditCustomer = "CanEditCustomer";
    public const string CanDeleteCustomer = "CanDeleteCustomer";
    public const string CanViewOrder = "CanViewOrder";
    public const string CanCreateOrders = "CanCreateOrders";
    public const string CanGeneratePackingList = "CanGeneratePackingList";
    public const string CanViewPackingList = "CanViewPackingList";
    public const string CanGenerateProformaInvoice = "CanGenerateProformaInvoice";
    public const string CanViewProformaInvoice = "CanViewProformaInvoice";
    public const string CanViewInvoice = "CanViewInvoice";
    public const string CanViewWaybillForFgw = "CanViewWaybillForFgw";
    public const string CanCreateWaybillForFgw = "CanCreateWaybillForFgw";
    public const string CanEditWaybillForFgw = "CanEditWaybillForFgw";
    public const string CanDeleteWaybillForFgw = "CanDeleteWaybillForFgw";

    // Human Resources
    public const string CanViewEmployee = "CanViewEmployee";
    public const string CanRegisterEmployee = "CanRegisterEmployee";
    public const string CanUpdateEmployeeInfo = "CanUpdateEmployeeInfo";
    public const string CanViewEmployeeDetails = "CanViewEmployeeDetails";
    public const string CanViewDepartmentEmployee = "CanViewDepartmentEmployee";
    public const string CanExportDepartmentEmployee = "CanExportDepartmentEmployee";
    public const string CanViewDesignation = "CanViewDesignation";
    public const string CanCreateDesignation = "CanCreateDesignation";
    public const string CanEditDesignation = "CanEditDesignation";
    public const string CanDeleteDesignation = "CanDeleteDesignation";
    public const string CanViewLeaveRequests = "CanViewLeaveRequests";
    public const string CanCreateLeaveRequest = "CanCreateLeaveRequest";
    public const string CanEditLeaveRequest = "CanEditLeaveRequest";
    public const string CanDeleteLeaveRequest = "CanDeleteLeaveRequest";
    public const string CanRecallLeave = "CanRecallLeave";
    public const string CanViewLeaveTypeConfig = "CanViewLeaveTypeConfig";
    public const string CanCreateLeaveTypeConfig = "CanCreateLeaveTypeConfig";
    public const string CanEditLeaveTypeConfig = "CanEditLeaveTypeConfig";
    public const string CanDeleteLeaveTypeConfig = "CanDeleteLeaveTypeConfig";
    public const string CanViewStaffRequisition = "CanViewStaffRequisition";
    public const string CanCreateStaffRequisition = "CanCreateStaffRequisition";
    public const string CanEditStaffRequisition = "CanEditStaffRequisition";
    public const string CanDeleteStaffRequisition = "CanDeleteStaffRequisition";
    public const string CanViewAttendanceReportUpload = "CanViewAttendanceReportUpload";
    public const string CanSubmitAttendanceReportUpload = "CanSubmitAttendanceReportUpload";
    public const string CanCancelAttendanceReportUpload = "CanCancelAttendanceReportUpload";
    public const string CanViewShiftScheduleReportUpload = "CanViewShiftScheduleReportUpload";
    public const string CanSubmitShiftScheduleReportUpload = "CanSubmitShiftScheduleReportUpload";
    public const string CanViewOvertimeManagement = "CanViewOvertimeManagement";
    public const string CanCreateOvertimeManagement = "CanCreateOvertimeManagement";
    public const string CanEditOvertimeManagement = "CanEditOvertimeManagement";
    public const string CanDeleteOvertimeManagement = "CanDeleteOvertimeManagement";

    // IT SUPPORT
    public const string CanViewUserDirectory = "CanViewUserDirectory";
    public const string CanCreateUser = "CanCreateUser";
    public const string CanEditUser = "CanEditUser";
    public const string CanBlockUser = "CanBlockUser";
    public const string CanViewAuditTrail = "CanViewAuditTrail";
    public const string CanFilterAuditTrailByUser = "CanFilterAuditTrailByUser";
    public const string CanFilterAuditTrailByDepartment = "CanFilterAuditTrailByDepartment";
    public const string CanExportAuditTrail = "CanExportAuditTrail";
    public const string CanViewRoles = "CanViewRoles";
    public const string CanCreateRole = "CanCreateRole";
    public const string CanEditRole = "CanEditRole";
    public const string CanDeleteARole = "CanDeleteARole";
    public const string CanViewPermissions = "CanViewPermissions";
    public const string CanUpdateExistingPermission = "CanUpdateExistingPermission";
    public const string CanResetPermission = "CanResetPermission";

    // Settings
    public const string CanViewGeneralSettingsConfigurations = "CanViewGeneralSettingsConfigurations";
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
    public const string CanViewMaterialTypes = "CanViewMaterialTypes";
    public const string CanCreateMaterialType = "CanCreateMaterialType";
    public const string CanEditMaterialType = "CanEditMaterialType";
    public const string CanDeleteMaterialType = "CanDeleteMaterialType";
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
    public const string CanCreateOperation = "CanCreateOperation";
    public const string CanEditOperation = "CanEditOperation";
    public const string CanDeleteOperation = "CanDeleteOperation";
    public const string CanViewWorkCenters = "CanViewWorkCenters";
    public const string CanCreateWorkCenter = "CanCreateWorkCenter";
    public const string CanEditWorkCenter = "CanEditWorkCenter";
    public const string CanDeleteWorkCenter = "CanDeleteWorkCenter";
    public const string CanViewCountries = "CanViewCountries";
    public const string CanCreateCountries = "CanCreateCountries";
    public const string CanEditCountries = "CanEditCountries";
    public const string CanDeleteCountries = "CanDeleteCountries";
    public const string CanViewShiftSchedules = "CanViewShiftSchedules";
    public const string CanCreateShiftSchedules = "CanCreateShiftSchedules";
    public const string CanEditShiftSchedules = "CanEditShiftSchedules";
    public const string CanDeleteShiftSchedules = "CanDeleteShiftSchedules";
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
    public const string CanViewCodeSettings = "CanViewCodeSettings";
    public const string CanCreateNewCodes = "CanCreateNewCodes";
    public const string CanEditCodeSettings = "CanEditCodeSettings";
    public const string CanDeleteCodeSettings = "CanDeleteCodeSettings";
    public const string CanViewQuestions = "CanViewQuestions";
    public const string CanCreateQuestions = "CanCreateQuestions";
    public const string CanEditQuestions = "CanEditQuestions";
    public const string CanDeleteQuestions = "CanDeleteQuestions";
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
    public const string CanViewSignatureSettings = "CanViewSignatureSettings";
    public const string CanCreateSignatureSettings = "CanCreateSignatureSettings";
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
    public const string CanViewWarehouses = "CanViewWarehouses";
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
    public const string CanAddNewEquipment = "CanAddNewEquipment";
    public const string CanEditEquipmentDetails = "CanEditEquipmentDetails";
    public const string CanDeleteEquipment = "CanDeleteEquipment";
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
}

  public static class PermissionUtils
  { 
      public static IEnumerable<PermissionDto> GeneratePermissions()
      {
            // Helper function to generate a readable name from a PascalCase key.
            // E.g., "CanViewPurchaseRequisitions" becomes "Can View Purchase Requisitions".
          string GenerateDisplayName(string key) => Regex.Replace(key, "(\\B[A-Z])", " $1");

            // Helper function to generate a basic description.
          string CreateDescription(string name) => $"Allows the user to {char.ToLower(name[0])}";

            var permissions = new List<PermissionDto>();

            // A helper action to reduce boilerplate code when adding new permissions.
            Action<string, string, string> addPermission = (module, submodule, key) =>
            {
                var name = GenerateDisplayName(key);
                permissions.Add(new PermissionDto(module, submodule, key, name, CreateDescription(name)));
            };

            // Procurement
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseRequisition, PermissionKeys.CanViewPurchaseRequisitions);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseRequisition, PermissionKeys.CanSourcePurchaseRequisition);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsRequest, PermissionKeys.CanViewForeignQuotation);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsRequest, PermissionKeys.CanSendForeignQuotationRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsRequest, PermissionKeys.CanViewLocalQuotation);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsRequest, PermissionKeys.CanSendLocalQuotationRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsResponses, PermissionKeys.CanViewForeignQuotationResponse);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsResponses, PermissionKeys.CanSendForeignQuotationResponse);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsResponses, PermissionKeys.CanViewLocalQuotationResponse);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.QuotationsResponses, PermissionKeys.CanSendLocalQuotationResponse);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PriceComparison, PermissionKeys.CanViewForeignVendorPricing);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PriceComparison, PermissionKeys.CanApplyChangesForForeignVendorPricingSelection);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PriceComparison, PermissionKeys.CanViewLocalVendorPricing);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PriceComparison, PermissionKeys.CanApplyChangesForLocalVendorPricingSelection);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaRequest, PermissionKeys.CanViewForeignProformaRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaRequest, PermissionKeys.CanSendForeignProformaRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaRequest, PermissionKeys.CanViewLocalProformaRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaRequest, PermissionKeys.CanSendLocalProformaRequest);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaResponses, PermissionKeys.CanViewForeignProformaInvoiceSubmissions);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaResponses, PermissionKeys.CanUploadForeignProformaInvoice);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaResponses, PermissionKeys.CanViewLocalProformaInvoiceSubmissions);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.ProformaResponses, PermissionKeys.CanUploadLocalProformaInvoice);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.CreatePurchaseOrdersPage, PermissionKeys.CanViewForeignPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.CreatePurchaseOrdersPage, PermissionKeys.CanCreateForeignPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.CreatePurchaseOrdersPage, PermissionKeys.CanViewLocalPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.CreatePurchaseOrdersPage, PermissionKeys.CanCreateLocalPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanReviseForeignPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrderListPage, PermissionKeys.CanReviseLocalPurchaseOrder);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.MaterialDistribution, PermissionKeys.CanViewMaterialDistribution);
            addPermission(PermissionModules.Procurement, PermissionSubmodules.MaterialDistribution, PermissionKeys.CanDistributeMaterial);

            // Logistics
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanCreateShipmentInvoice);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanViewShipmentInvoice);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanEditShipmentInvoice);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanDeleteShipmentInvoice);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanCreateShipmentDocument);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanViewShipmentDocument);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanChangeShipmentDocumentStatus);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanEditShipmentDocument);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanDeleteShipmentDocument);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanCreateBillingSheet);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanViewBillingSheet);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanEditBillingSheet);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanCreateWaybill);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanViewWaybill);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanChangeWaybillStatus);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.AvailableStock, PermissionKeys.CanViewRawMaterialStock);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.AvailableStock, PermissionKeys.CanFilterRawMaterialStockByDepartment);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.AvailableStock, PermissionKeys.CanViewPackingMaterialStock);
            addPermission(PermissionModules.Logistics, PermissionSubmodules.AvailableStock, PermissionKeys.CanFilterPackingMaterialStockByDepartment);

            // Warehouse
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanViewRawMaterialsItems);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateChecklistForRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateGrnForRawMaterialsChecklistedItems);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanViewPackagingMaterialsItems);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateChecklistForPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateGrnForPackagingMaterialsChecklistedItems);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanViewQuarantineRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanAssignRawMaterialsStockToShelves);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanViewQuarantinePackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanAssignPackagingMaterialsStockToShelves);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanViewLinkedRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanUnlinkRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanViewLinkedPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LinkedMaterials, PermissionKeys.CanUnlinkPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.UnlinkedMaterials, PermissionKeys.CanViewUnlinkedRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.UnlinkedMaterials, PermissionKeys.CanLinkRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.UnlinkedMaterials, PermissionKeys.CanViewUnlinkedPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.UnlinkedMaterials, PermissionKeys.CanLinkPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanViewRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanCreateNewRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanEditRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanDeleteRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanViewPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanCreateNewPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanEditPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanDeletePackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewApprovedRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewApprovedPackagingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.RejectedMaterials, PermissionKeys.CanViewRejectedRawMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.RejectedMaterials, PermissionKeys.CanViewRejectedPackingMaterials);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanViewRawMaterialRequisitions);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanIssueRawMaterialRequisitions);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanViewPackagingMaterialRequisitions);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.IssueStockRequisitions, PermissionKeys.CanIssuePackagingMaterialRequisitions);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanViewRawMaterialTransferList);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanIssueRawMaterialStockTransfers);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanViewPackagingMaterialTransferList);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanIssuePackagingMaterialStockTransfers);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanViewRawMaterialLocationChartList);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanReassignRawMaterialStock);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanViewPackagingMaterialLocationChartList);
            addPermission(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanReassignPackagingMaterialStock);

            // Production
            addPermission(PermissionModules.Production, PermissionSubmodules.Requisitions, PermissionKeys.CanViewMaterialRequisitions);
            addPermission(PermissionModules.Production, PermissionSubmodules.Requisitions, PermissionKeys.CanViewMaterialRequisitionDetailsPage);
            addPermission(PermissionModules.Production, PermissionSubmodules.CreatePurchaseRequisitions, PermissionKeys.CanViewRawMaterialRequisitionsForCreation);
            addPermission(PermissionModules.Production, PermissionSubmodules.CreatePurchaseRequisitions, PermissionKeys.CanCreateRawMaterialRequisitions);
            addPermission(PermissionModules.Production, PermissionSubmodules.CreatePurchaseRequisitions, PermissionKeys.CanViewPackageMaterialRequisitionsForCreation);
            addPermission(PermissionModules.Production, PermissionSubmodules.CreatePurchaseRequisitions, PermissionKeys.CanCreatePackageMaterialRequisitions);
            addPermission(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanViewPlannedProducts);
            addPermission(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanCreateNewProductionPlan);
            addPermission(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanEditProductionPlan);
            addPermission(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanViewIncomingStockTransferRequests);
            addPermission(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanApproveOrRejectIncomingStockTransferRequest);
            addPermission(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanViewOutgoingStockTransferRequests);
            addPermission(PermissionModules.Production, PermissionSubmodules.ProductSchedule, PermissionKeys.CanViewProductSchedules);
            addPermission(PermissionModules.Production, PermissionSubmodules.ProductSchedule, PermissionKeys.CanCreateProductSchedule);
            
            // Quality Control
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanViewRawMaterialGoodsReceiptNotes);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanTakeRawMaterialSample);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanStartRawMaterialTest);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanCheckRawMaterialTestResult);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanViewPackagingMaterialGoodsReceiptNotes);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanTakePackagingMaterialSample);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanStartPackagingMaterialTest);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanCheckPackagingMaterialTestResult);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanViewProductAnalyticalTestRequests);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanAcknowledgeSampleTaken);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanStartProductTest);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.AnalyticalTestRequestProducts, PermissionKeys.CanCheckProductTest);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanViewRawMaterialStps);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanCreateRawMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanEditRawMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanDeleteRawMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanViewPackagingMaterialStps);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanCreatePackagingMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanEditPackagingMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanDeletePackagingMaterialStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanViewRawMaterialSpecifications);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanCreateRawMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanEditRawMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanDeleteRawMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanViewPackagingMaterialSpecifications);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanCreatePackagingMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanEditPackagingMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanDeletePackagingMaterialSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanViewRawMaterialArds);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanCreateRawMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanEditRawMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanDeleteRawMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanViewPackagingMaterialArds);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanCreatePackagingMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanEditPackagingMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanDeletePackagingMaterialArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanViewProductStps);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanCreateProductStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanEditProductStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanDeleteProductStp);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanViewProductArds);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanCreateProductArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanEditProductArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanDeleteProductArd);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanViewProductSpecifications);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanCreateProductSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanEditProductSpecification);
            addPermission(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanDeleteProductSpecification);

            // Quality Assurance
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.IssueBmr, PermissionKeys.CanViewIssuedBmrBprs);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.IssueBmr, PermissionKeys.CanIssueBmr);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanViewAnalyticalTestRequests);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanTakeSamples);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanViewPendingApprovals);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanApprovePendingApproval);
            addPermission(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanRejectPendingApproval);

            // Finished Goods Warehouse
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.CustomerManagement, PermissionKeys.CanViewCustomers);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.CustomerManagement, PermissionKeys.CanCreateCustomer);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.CustomerManagement, PermissionKeys.CanEditCustomer);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.CustomerManagement, PermissionKeys.CanDeleteCustomer);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.ProductionOrders, PermissionKeys.CanViewOrder);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.ProductionOrders, PermissionKeys.CanCreateOrders);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.ProductionOrders, PermissionKeys.CanGeneratePackingList);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.PackingList, PermissionKeys.CanViewPackingList);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanGenerateProformaInvoice);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanViewProformaInvoice);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.Invoice, PermissionKeys.CanViewInvoice);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.WaybillFgw, PermissionKeys.CanViewWaybillForFgw);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.WaybillFgw, PermissionKeys.CanCreateWaybillForFgw);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.WaybillFgw, PermissionKeys.CanEditWaybillForFgw);
            addPermission(PermissionModules.FinishedGoodsWarehouse, PermissionSubmodules.WaybillFgw, PermissionKeys.CanDeleteWaybillForFgw);
            
            // Human Resources
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanViewEmployee);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanRegisterEmployee);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanUpdateEmployeeInfo);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanViewEmployeeDetails);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DepartmentEmployeeExport, PermissionKeys.CanViewDepartmentEmployee);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DepartmentEmployeeExport, PermissionKeys.CanExportDepartmentEmployee);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanViewDesignation);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanCreateDesignation);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanEditDesignation);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanDeleteDesignation);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanViewLeaveRequests);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanCreateLeaveRequest);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanEditLeaveRequest);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanDeleteLeaveRequest);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanRecallLeave);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveTypeConfiguration, PermissionKeys.CanViewLeaveTypeConfig);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveTypeConfiguration, PermissionKeys.CanCreateLeaveTypeConfig);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveTypeConfiguration, PermissionKeys.CanEditLeaveTypeConfig);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.LeaveTypeConfiguration, PermissionKeys.CanDeleteLeaveTypeConfig);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.StaffRequisition, PermissionKeys.CanViewStaffRequisition);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.StaffRequisition, PermissionKeys.CanCreateStaffRequisition);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.StaffRequisition, PermissionKeys.CanEditStaffRequisition);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.StaffRequisition, PermissionKeys.CanDeleteStaffRequisition);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.AttendanceReportUpload, PermissionKeys.CanViewAttendanceReportUpload);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.AttendanceReportUpload, PermissionKeys.CanSubmitAttendanceReportUpload);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.AttendanceReportUpload, PermissionKeys.CanCancelAttendanceReportUpload);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.ShiftScheduleReportUpload, PermissionKeys.CanViewShiftScheduleReportUpload);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.ShiftScheduleReportUpload, PermissionKeys.CanSubmitShiftScheduleReportUpload);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.OvertimeManagement, PermissionKeys.CanViewOvertimeManagement);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.OvertimeManagement, PermissionKeys.CanCreateOvertimeManagement);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.OvertimeManagement, PermissionKeys.CanEditOvertimeManagement);
            addPermission(PermissionModules.HumanResources, PermissionSubmodules.OvertimeManagement, PermissionKeys.CanDeleteOvertimeManagement);

            // IT Support
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanViewUserDirectory);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanCreateUser);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanEditUser);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.UserManagement, PermissionKeys.CanBlockUser);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.AuditTrail, PermissionKeys.CanViewAuditTrail);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.AuditTrail, PermissionKeys.CanFilterAuditTrailByUser);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.AuditTrail, PermissionKeys.CanFilterAuditTrailByDepartment);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.AuditTrail, PermissionKeys.CanExportAuditTrail);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManageRoles, PermissionKeys.CanViewRoles);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManageRoles, PermissionKeys.CanCreateRole);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManageRoles, PermissionKeys.CanEditRole);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManageRoles, PermissionKeys.CanDeleteARole);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManagePermissions, PermissionKeys.CanViewPermissions);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManagePermissions, PermissionKeys.CanUpdateExistingPermission);
            addPermission(PermissionModules.ItSupport, PermissionSubmodules.ManagePermissions, PermissionKeys.CanResetPermission);
            
            // Settings
            addPermission(PermissionModules.Settings, PermissionSubmodules.SystemSettings, PermissionKeys.CanViewGeneralSettingsConfigurations);
            addPermission(PermissionModules.Settings, PermissionSubmodules.ProductsCategory, PermissionKeys.CanViewProductCategories);
            addPermission(PermissionModules.Settings, PermissionSubmodules.ProductsCategory, PermissionKeys.CanCreateProductCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.ProductsCategory, PermissionKeys.CanEditProductCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.ProductsCategory, PermissionKeys.CanDeleteProductCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanViewRawCategories);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanCreateRawCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanEditRawCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanDeleteRawCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanViewPackageCategories);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanCreatePackageCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanEditPackageCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanDeletePackageCategory);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanViewMaterialTypes);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanCreateMaterialType);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanEditMaterialType);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanDeleteMaterialType);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanViewPackageStyle);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanCreatePackageStyle);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanEditPackageStyle);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanDeletePackageStyle);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanViewProductState);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanCreateProductState);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanEditProductState);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Products, PermissionKeys.CanDeleteProductState);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanViewResources);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanCreateResource);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanEditResource);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanDeleteResource);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanViewOperations);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanCreateOperation);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanEditOperation);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanDeleteOperation);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanViewWorkCenters);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanCreateWorkCenter);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanEditWorkCenter);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Procedures, PermissionKeys.CanDeleteWorkCenter);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CountryAddress, PermissionKeys.CanViewCountries);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CountryAddress, PermissionKeys.CanCreateCountries);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CountryAddress, PermissionKeys.CanEditCountries);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CountryAddress, PermissionKeys.CanDeleteCountries);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Schedules, PermissionKeys.CanViewShiftSchedules);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Schedules, PermissionKeys.CanCreateShiftSchedules);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Schedules, PermissionKeys.CanEditShiftSchedules);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Schedules, PermissionKeys.CanDeleteShiftSchedules);
            addPermission(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanViewPaymentTerms);
            addPermission(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanCreatePaymentTerm);
            addPermission(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanEditPaymentTerm);
            addPermission(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanDeletePaymentTerm);
            addPermission(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanViewDeliveryModes);
            addPermission(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanCreateDeliveryMode);
            addPermission(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanEditDeliveryMode);
            addPermission(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanDeleteDeliveryMode);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Charges, PermissionKeys.CanViewCharges);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Charges, PermissionKeys.CanCreateCharges);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Charges, PermissionKeys.CanEditCharges);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Charges, PermissionKeys.CanDeleteCharges);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanViewCodeSettings);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanCreateNewCodes);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanEditCodeSettings);
            addPermission(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanDeleteCodeSettings);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanViewQuestions);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanCreateQuestions);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanEditQuestions);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanDeleteQuestions);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanViewTemplate);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanCreateTemplate);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanEditTemplate);
            addPermission(PermissionModules.Settings, PermissionSubmodules.WorkflowBuilder, PermissionKeys.CanDeleteTemplate);
            addPermission(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanViewAlerts);
            addPermission(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanCreateNewAlerts);
            addPermission(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanEditAlerts);
            addPermission(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanEnableDisableAlerts);
            addPermission(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanDeleteAlerts);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanViewApprovals);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanCreateNewApproval);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanEditApprovalWorkflow);
            addPermission(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanDeleteApprovals);
            addPermission(PermissionModules.Settings, PermissionSubmodules.SignatureSettings, PermissionKeys.CanViewSignatureSettings);
            addPermission(PermissionModules.Settings, PermissionSubmodules.SignatureSettings, PermissionKeys.CanCreateSignatureSettings);
            addPermission(PermissionModules.Settings, PermissionSubmodules.ChangePassword, PermissionKeys.CanChangePassword);

            // Inventory Management
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Manufacturers, PermissionKeys.CanViewManufacturers);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Manufacturers, PermissionKeys.CanCreateManufacturer);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Manufacturers, PermissionKeys.CanUpdateManufacturerDetails);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Manufacturers, PermissionKeys.CanDeleteManufacturer);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Suppliers, PermissionKeys.CanViewVendors);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Suppliers, PermissionKeys.CanCreateVendor);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Suppliers, PermissionKeys.CanUpdateVendorDetails);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Suppliers, PermissionKeys.CanDeleteVendor);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Warehouses, PermissionKeys.CanViewWarehouses);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Locations, PermissionKeys.CanViewLocations);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Locations, PermissionKeys.CanAddNewLocation);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Locations, PermissionKeys.CanEditLocation);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Locations, PermissionKeys.CanDeleteLocation);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Racks, PermissionKeys.CanViewRacks);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Racks, PermissionKeys.CanAddNewRack);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Racks, PermissionKeys.CanEditRack);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Racks, PermissionKeys.CanDeleteRack);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Shelves, PermissionKeys.CanViewShelves);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Shelves, PermissionKeys.CanAddNewShelf);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Shelves, PermissionKeys.CanEditShelf);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Shelves, PermissionKeys.CanDeleteShelf);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Equipment, PermissionKeys.CanViewEquipment);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Equipment, PermissionKeys.CanAddNewEquipment);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Equipment, PermissionKeys.CanEditEquipmentDetails);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.Equipment, PermissionKeys.CanDeleteEquipment);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.UnitOfMeasure, PermissionKeys.CanViewUnitOfMeasure);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.UnitOfMeasure, PermissionKeys.CanCreateUnitOfMeasure);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.UnitOfMeasure, PermissionKeys.CanEditUnitOfMeasure);
            addPermission(PermissionModules.InventoryManagement, PermissionSubmodules.UnitOfMeasure, PermissionKeys.CanDeleteUnitOfMeasure);
            
            // Organizational Structure
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Departments, PermissionKeys.CanViewDepartments);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Departments, PermissionKeys.CanCreateNewDepartment);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Departments, PermissionKeys.CanEditDepartment);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Departments, PermissionKeys.CanDeleteDepartment);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.WorkingDays, PermissionKeys.CanViewWorkingDays);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.WorkingDays, PermissionKeys.CanCreateWorkingDays);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.WorkingDays, PermissionKeys.CanResetWorkingDays);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Holidays, PermissionKeys.CanViewHolidays);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Holidays, PermissionKeys.CanCreateHoliday);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Holidays, PermissionKeys.CanEditHoliday);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.Holidays, PermissionKeys.CanDeleteHoliday);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsType, PermissionKeys.CanViewShiftTypes);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsType, PermissionKeys.CanCreateShiftType);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsType, PermissionKeys.CanEditShiftType);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsType, PermissionKeys.CanDeleteShiftType);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsSchedule, PermissionKeys.CanViewShiftSchedule);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsSchedule, PermissionKeys.CanCreateShiftSchedule);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsSchedule, PermissionKeys.CanEditShiftSchedule);
            addPermission(PermissionModules.OrganizationalStructure, PermissionSubmodules.ShiftsSchedule, PermissionKeys.CanDeleteShiftSchedule);

            return permissions;
      }
}

