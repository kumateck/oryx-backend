using DOMAIN.Entities.Permissions;

namespace APP.Utils;

public static class PermissionModules
{
    public const string Dashboard = "Dashboard";
    public const string ProductBoard = "ProductBoard";
    public const string Procurement = "Procurement";
    public const string Logistics = "Logistics";
    public const string HumanResources = "HumanResources";
    public const string Warehouse = "Warehouse";
    public const string Production = "Production";
    public const string Settings = "Settings";
    public const string QualityControl = "Quality Control";
    public const string QualityAssurance = "Quality Assurance";
    public const string Inventory = "Inventory";
    public const string Crm = "CRM";
}

public static class PermissionSubmodules
{
    // Procurement
    public const string Manufacturers = "Manufacturers";
    public const string Vendors = "Vendors";
    public const string PurchaseRequisition = "Purchase Requisition";
    public const string QuotationsRequest = "Quotations Request";
    public const string QuotationsResponses = "Quotations Responses";
    public const string PriceComparison = "Price Comparison";
    public const string AwardedQuotations = "Awarded Quotations";
    public const string ProformaResponses = "Proforma Responses";
    public const string PurchaseOrders = "Purchase Orders";
    public const string MaterialDistribution = "Material Distribution";

    // Logistics
    public const string ShipmentInvoice = "Shipment Invoice";
    public const string ShipmentDocument = "Shipment Document";
    public const string BillingSheet = "Billing Sheet";
    public const string Waybill = "Waybill";

    // Human Resources
    public const string EmployeeManagement = "Employee Management";
    public const string UserManagement = "User Management";
    public const string DesignationManagement = "Designation Management";
    public const string RolesPermissionsManagement = "Roles & Permissions Management";
    public const string LeaveManagement = "Leave Management";

    // Warehouse
    public const string ReceivingArea = "Receiving Area";
    public const string QuarantineAreaGrn = "Quarantine Area / GRN";
    public const string Departments = "Departments";
    public const string Warehouses = "Warehouses";
    public const string Locations = "Locations";
    public const string Racks = "Racks";
    public const string Shelves = "Shelves";
    public const string Materials = "Materials";
    public const string ApprovedMaterials = "Approved Materials";
    public const string RejectedMaterials = "Rejected Materials";
    public const string StockRequisitions = "Stock Requisitions";
    public const string StockTransferIssues = "Stock Transfer Issues";
    public const string LocationChartRecord = "Location Chart Record";

    // Production
    public const string RawMaterialRequisitions = "Raw Material Requisitions"; // Under REQUISITIONS group
    public const string PackageMaterialRequisitions = "Package Material Requisitions"; // Under REQUISITIONS group
    public const string Planning = "Planning";
    public const string StockTransferRequests = "Stock Transfer Requests";
    public const string ProductSchedule = "Product Schedule";

    // Settings
    public const string SystemSettings = "System Settings";
    public const string UserSettings = "User Settings";
    public const string AuditTrail = "Audit Trail";
    // Settings -> Configurations (Sub-Group)
    public const string Categories = "Categories"; // Group under Configurations
    public const string ProductCategory = "Product Category"; // Child of Categories
    public const string RawCategory = "Raw Category"; // Child of Categories
    public const string PackageCategory = "Package Category"; // Child of Categories
    public const string Procedures = "Procedures"; // Group under Configurations
    public const string Resource = "Resource"; // Child of Procedures
    public const string Operation = "Operation"; // Child of Procedures
    public const string WorkCenter = "Work Center"; // Child of Procedures
    public const string Products = "Products"; // Group under Configurations
    public const string MaterialType = "Material Type"; // Child of Products
    public const string UnitOfMeasure = "Unit of Measure";
    public const string Address = "Address"; // Group under Configurations
    public const string Country = "Country"; // Child of Address
    public const string Container = "Container"; // Group under Configurations
    public const string PackStyle = "Pack Style"; // Child of Container
    public const string BillingSheetCharge = "Billing Sheet Charge";
    public const string TermsOfPayment = "Terms of Payment";
    public const string DeliveryMode = "Delivery Mode";
    public const string CodeSettings = "Code Settings";
    public const string Approvals = "Approvals";
    public const string AlertsNotifications = "Alerts & Notifications";
    public const string Equipment = "Equipment";
    public const string WorkFlowForms = "Work Flow Forms"; // Group under Configurations
    public const string Questions = "Questions"; // Child of WorkFlowForms
    public const string Templates = "Templates"; // Child of WorkFlowForms

    // Quality Control
    public const string MaterialStp = "Material STP";
    public const string ProductStp = "Product STP";
    public const string MaterialArd = "Material ARD";
    public const string ProductArd = "Product ARD";
    public const string GoodsReceiptNote = "Goods Receipt Note";
    public const string MaterialSpecification = "Material Specification";
    public const string ProductSpecification = "Product Specification";

    // Quality Assurance
    public const string IssueBmr = "Issue BMR";
    public const string BmrBprRequest = "BMR/BPR Request";
    public const string AnalyticalTestRequests = "Analytical Test Requests";
    public const string PendingApprovals = "Pending Approvals";

    // Inventory
    public const string VendorManagement = "Vendor Management";
    public const string PurchaseRequisitionInventory = "Purchase Requisition";
    public const string StockRequisition = "Stock Requisition";

    // CRM
    public const string CustomerManagement = "Customer Management";
    public const string ProductionOrders = "Production Orders";
    public const string ProformaInvoice = "Proforma Invoice";
    public const string Invoice = "Invoice";
    public const string WaybillCrm = "Waybill";
}


public static class PermissionKeys
{
    // Procurement
    public const string CanCreateManufacturer = "CanCreateManufacturer";
    public const string CanViewManufacturerDetails = "CanViewManufacturerDetails";
    public const string CanUpdateManufacturerDetails = "CanUpdateManufacturerDetails";
    public const string CanDeleteManufacturer = "CanDeleteManufacturer";

    public const string CanCreateVendor = "CanCreateVendor";
    public const string CanViewVendorDetails = "CanViewVendorDetails";
    public const string CanUpdateVendorDetails = "CanUpdateVendorDetails";
    public const string CanDeleteVendor = "CanDeleteVendor";

    public const string CanViewPurchaseRequisitions = "CanViewPurchaseRequisitions";
    public const string CanSourceItemsBasedOnRequisition = "CanSourceItemsBasedOnRequisition";

    public const string CanSendQuotationRequest = "CanSendQuotationRequest";
    public const string CanInputResponses = "CanInputResponses";
    public const string CanSelectVendorPricing = "CanSelectVendorPricing";
    public const string CanSendAwardedQuotations = "CanSendAwardedQuotations";
    public const string CanUploadProformaInvoice = "CanUploadProformaInvoice";
    public const string CanCreatePurchaseOrder = "CanCreatePurchaseOrder";
    public const string CanReviseExistingPurchaseOrder = "CanReviseExistingPurchaseOrder";
    public const string CanDistributeMaterials = "CanDistributeMaterials";

    // Logistics
    public const string CanCreateShipmentInvoice = "CanCreateShipmentInvoice";
    public const string CanViewShipmentInvoice = "CanViewShipmentInvoice";
    public const string CanEditShipmentInvoice = "CanEditShipmentInvoice";
    public const string CanDeleteShipmentInvoice = "CanDeleteShipmentInvoice";

    public const string CanCreateShipmentDocument = "CanCreateShipmentDocument";
    public const string CanViewShipmentDocument = "CanViewShipmentDocument";
    public const string CanEditShipmentDocument = "CanEditShipmentDocument";
    public const string CanDeleteShipmentDocument = "CanDeleteShipmentDocument";
    public const string CanChangeShipmentDocumentStatus = "CanChangeShipmentDocumentStatus";

    public const string CanCreateBillingSheet = "CanCreateBillingSheet";
    public const string CanViewBillingSheet = "CanViewBillingSheet";
    public const string CanEditBillingSheet = "CanEditBillingSheet";
    public const string CanDeleteBillingSheet = "CanDeleteBillingSheet";

    public const string CanCreateWaybill = "CanCreateWaybill";
    public const string CanViewWaybill = "CanViewWaybill";
    public const string CanEditWaybill = "CanEditWaybill";
    public const string CanDeleteWaybill = "CanDeleteWaybill";
    public const string CanChangeWaybillStatus = "CanChangeWaybillStatus";

    // Human Resources
    public const string CanViewEmployee = "CanViewEmployee";
    public const string CanRegisterEmployee = "CanRegisterEmployee";
    public const string CanUpdateEmployeeDetails = "CanUpdateEmployeeDetails";
    public const string CanDeleteEmployee = "CanDeleteEmployee";

    public const string CanViewUser = "CanViewUser";
    public const string CanCreateUser = "CanCreateUser";
    public const string CanUpdateUserDetails = "CanUpdateUserDetails";
    public const string CanDeleteUser = "CanDeleteUser";

    public const string CanViewDesignation = "CanViewDesignation";
    public const string CanCreateDesignation = "CanCreateDesignation";
    public const string CanEditDesignation = "CanEditDesignation";
    public const string CanDeleteDesignation = "CanDeleteDesignation";

    public const string CanViewRoles = "CanViewRoles";
    public const string CanCreateRoleAndAssignPermissions = "CanCreateRoleAndAssignPermissions";
    public const string CanEditRoleWithItsPermissions = "CanEditRoleWithItsPermissions";
    public const string CanDeleteRole = "CanDeleteRole";

    public const string CanViewLeaveRequests = "CanViewLeaveRequests";
    public const string CanCreateLeaveRequest = "CanCreateLeaveRequest";
    public const string CanEditLeaveRequest = "CanEditLeaveRequest";
    public const string CanDeleteOrCancelLeaveRequest = "CanDeleteOrCancelLeaveRequest";
    public const string CanApproveOrRejectLeaveRequest = "CanApproveOrRejectLeaveRequest";

    // Warehouse (Receiving + Quarantine + Master)
    public const string CanViewReceivedRawMaterialsItems = "CanViewReceivedRawMaterialsItems";
    public const string CanViewReceivedPackagingMaterialsItems = "CanViewReceivedPackagingMaterialsItems";
    public const string CanCreateChecklistForIncomingRawMaterialsGoods = "CanCreateChecklistForIncomingRawMaterialsGoods";
    public const string CanCreateChecklistForIncomingPackagingMaterialsGoods = "CanCreateChecklistForIncomingPackagingMaterialsGoods";
    public const string CanCreateGrnForRawMaterialsChecklistedItems = "CanCreateGrnForRawMaterialsChecklistedItems";
    public const string CanCreateGrnForPackagingMaterialsChecklistedItems = "CanCreateGrnForPackagingMaterialsChecklistedItems";

    public const string CanViewQuarantineRawMaterialsRecords = "CanViewQuarantineRawMaterialsRecords";
    public const string CanViewQuarantinePackagingMaterialsRecords = "CanViewQuarantinePackagingMaterialsRecords";
    public const string CanAssignRawMaterialsStockToStorageLocations = "CanAssignRawMaterialsStockToStorageLocations";
    public const string CanAssignPackagingMaterialsStockToStorageLocations = "CanAssignPackagingMaterialsStockToStorageLocations";

    public const string CanViewDepartments = "CanViewDepartments";
    public const string CanCreateNewDepartment = "CanCreateNewDepartment";
    public const string CanEditDepartment = "CanEditDepartment";

    public const string CanViewWarehouses = "CanViewWarehouses";
    public const string CanAddWarehouse = "CanAddWarehouse";
    public const string CanEditWarehouse = "CanEditWarehouse";

    public const string CanViewLocations = "CanViewLocations";
    public const string CanAddNewLocation = "CanAddNewLocation";
    public const string CanEditLocation = "CanEditLocation";

    public const string CanViewRacks = "CanViewRacks";
    public const string CanAddNewRack = "CanAddNewRack";
    public const string CanEditRack = "CanEditRack";

    public const string CanViewShelves = "CanViewShelves";
    public const string CanAddNewShelf = "CanAddNewShelf";
    public const string CanEditShelf = "CanEditShelf";

    public const string CanViewRawMaterials = "CanViewRawMaterials";
    public const string CanViewPackagingMaterials = "CanViewPackagingMaterials";
    public const string CanCreateNewRawMaterials = "CanCreateNewRawMaterials";
    public const string CanCreateNewPackagingMaterials = "CanCreateNewPackagingMaterials";
    public const string CanEditRawMaterials = "CanEditRawMaterials";
    public const string CanEditPackagingMaterials = "CanEditPackagingMaterials";
    public const string CanDeleteRawMaterials = "CanDeleteRawMaterials";
    public const string CanDeletePackagingMaterials = "CanDeletePackagingMaterials";

    public const string CanViewApprovedRawMaterials = "CanViewApprovedRawMaterials";
    public const string CanViewApprovedPackagingMaterials = "CanViewApprovedPackagingMaterials";
    public const string CanViewRejectedRawMaterials = "CanViewRejectedRawMaterials";
    public const string CanViewRejectedPackagingMaterials = "CanViewRejectedPackagingMaterials"; // Corrected typo from text

    public const string CanViewRawMaterialRequisitions = "CanViewRawMaterialRequisitions";
    public const string CanViewPackagingMaterialRequisitions = "CanViewPackagingMaterialRequisitions";
    public const string CanIssueRawMaterialRequisitions = "CanIssueRawMaterialRequisitions";
    public const string CanIssuePackagingMaterialRequisitions = "CanIssuePackagingMaterialRequisitions";

    public const string CanViewRawMaterialTransferList = "CanViewRawMaterialTransferList";
    public const string CanViewPackagingMaterialTransferList = "CanViewPackagingMaterialTransferList";
    public const string CanIssueRawMaterialStockTransfers = "CanIssueRawMaterialStockTransfers";
    public const string CanIssuePackagingMaterialStockTransfers = "CanIssuePackagingMaterialStockTransfers";

    public const string CanViewRawMaterialLocationChartList = "CanViewRawMaterialLocationChartList";
    public const string CanViewPackagingMaterialLocationChartList = "CanViewPackagingMaterialLocationChartList";
    public const string CanReassignRawMaterialStock = "CanReassignRawMaterialStock";
    public const string CanReassignPackagingMaterialStock = "CanReassignPackagingMaterialStock";

    // Production
    public const string CanCreateRawMaterialPurchaseRequisition = "CanCreateRawMaterialPurchaseRequisition";
    public const string CanCreatePackagingMaterialPurchaseRequisition = "CanCreatePackagingMaterialPurchaseRequisition";
    public const string CanCreateRawMaterialStockRequisition = "CanCreateRawMaterialStockRequisition";
    public const string CanCreatePackagingMaterialStockRequisition = "CanCreatePackagingMaterialStockRequisition";
    public const string CanCreateRawMaterialStockTransfer = "CanCreateRawMaterialStockTransfer";
    public const string CanCreatePackagingMaterialStockTransfer = "CanCreatePackagingMaterialStockTransfer";

    public const string CanViewPlannedProducts = "CanViewPlannedProducts";
    public const string CanCreateNewProductionPlan = "CanCreateNewProductionPlan";
    public const string CanEditProductionPlan = "CanEditProductionPlan";

    public const string CanViewStockTransferRequests = "CanViewStockTransferRequests";
    public const string CanApproveOrRejectStockTransferRequest = "CanApproveOrRejectStockTransferRequest";

    public const string CanViewProductSchedules = "CanViewProductSchedules";
    public const string CanCreateProductSchedule = "CanCreateProductSchedule";
    public const string CanUpdateProductSchedule = "CanUpdateProductSchedule";

    // Settings: Audit, Configs, and System
    public const string CanViewSystemSettings = "CanViewSystemSettings";
    public const string CanViewUserSettings = "CanViewUserSettings";

    public const string CanViewAuditLogs = "CanViewAuditLogs";
    public const string CanExportAuditLogs = "CanExportAuditLogs";

    // Categories - Product Category
    public const string CanViewProductCategories = "CanViewProductCategories";
    public const string CanCreateNewProductCategory = "CanCreateNewProductCategory";
    public const string CanEditProductCategory = "CanEditProductCategory";
    public const string CanDeleteProductCategory = "CanDeleteProductCategory";

    // Categories - Raw Category
    public const string CanViewRawCategories = "CanViewRawCategories";
    public const string CanCreateNewRawCategory = "CanCreateNewRawCategory";
    public const string CanEditRawCategory = "CanEditRawCategory";
    public const string CanDeleteRawCategory = "CanDeleteRawCategory";

    // Categories - Package Category
    public const string CanViewPackageCategories = "CanViewPackageCategories";
    public const string CanCreateNewPackageCategory = "CanCreateNewPackageCategory";
    public const string CanEditPackageCategory = "CanEditPackageCategory";
    public const string CanDeletePackageCategory = "CanDeletePackageCategory";

    // Procedures - Resource
    public const string CanViewResources = "CanViewResources";
    public const string CanCreateNewResource = "CanCreateNewResource";
    public const string CanEditResource = "CanEditResource";
    public const string CanDeleteResource = "CanDeleteResource";

    // Procedures - Operation
    public const string CanViewOperations = "CanViewOperations";
    public const string CanCreateNewOperation = "CanCreateNewOperation";
    public const string CanEditOperation = "CanEditOperation";
    public const string CanDeleteOperation = "CanDeleteOperation";

    // Procedures - Work Center
    public const string CanViewWorkCenters = "CanViewWorkCenters";
    public const string CanCreateNewWorkCenter = "CanCreateNewWorkCenter";
    public const string CanEditWorkCenter = "CanEditWorkCenter";
    public const string CanDeleteWorkCenter = "CanDeleteWorkCenter";

    // Products - Material Type
    public const string CanViewMaterialTypes = "CanViewMaterialTypes";
    public const string CanCreateNewMaterialType = "CanCreateNewMaterialType";
    public const string CanEditMaterialType = "CanEditMaterialType";
    public const string CanDeleteMaterialType = "CanDeleteMaterialType";

    // Products - Unit of Measure
    public const string CanViewUnitOfMeasure = "CanViewUnitOfMeasure";

    // Address - Country
    public const string CanViewCountries = "CanViewCountries";

    // Container - Pack Style
    public const string CanViewPackStyles = "CanViewPackStyles";
    public const string CanCreateNewPackStyle = "CanCreateNewPackStyle";
    public const string CanEditPackStyle = "CanEditPackStyle";
    public const string CanDeletePackStyle = "CanDeletePackStyle";

    // Billing Sheet Charges
    public const string CanViewBillingCharges = "CanViewBillingCharges";
    public const string CanCreateNewBillingSheetCharge = "CanCreateNewBillingSheetCharge";
    public const string CanEditBillingSheetCharge = "CanEditBillingSheetCharge";
    public const string CanDeleteBillingSheetCharge = "CanDeleteBillingSheetCharge";

    // Terms of Payment
    public const string CanViewPaymentTerms = "CanViewPaymentTerms";
    public const string CanCreateNewPaymentTerm = "CanCreateNewPaymentTerm";
    public const string CanEditPaymentTerm = "CanEditPaymentTerm";
    public const string CanDeletePaymentTerm = "CanDeletePaymentTerm";

    // Delivery Mode
    public const string CanViewDeliveryModes = "CanViewDeliveryModes";
    public const string CanCreateNewDeliveryMode = "CanCreateNewDeliveryMode";
    public const string CanEditDeliveryMode = "CanEditDeliveryMode";
    public const string CanDeleteDeliveryMode = "CanDeleteDeliveryMode";

    // Code Settings
    public const string CanViewCodeSettings = "CanViewCodeSettings";
    public const string CanAddNewCodes = "CanAddNewCodes";
    public const string CanEditCodeSettings = "CanEditCodeSettings";
    public const string CanDeleteCodeSettings = "CanDeleteCodeSettings";

    // Approvals
    public const string CanViewApproval = "CanViewApproval";
    public const string CanCreateOrConfigureNewApproval = "CanCreateOrConfigureNewApproval";
    public const string CanEditApprovalWorkflow = "CanEditApprovalWorkflow";
    public const string CanDeleteOrDisableApprovals = "CanDeleteOrDisableApprovals";

    // Alerts & Notifications
    public const string CanViewAlerts = "CanViewAlerts";
    public const string CanCreateNewAlerts = "CanCreateNewAlerts";
    public const string CanEditAlerts = "CanEditAlerts";
    public const string CanEnableOrDisableAlerts = "CanEnableOrDisableAlerts";
    public const string CanDeleteAlerts = "CanDeleteAlerts";

    // Equipment
    public const string CanViewEquipments = "CanViewEquipments";
    public const string CanAddNewEquipment = "CanAddNewEquipment";
    public const string CanEditEquipmentDetails = "CanEditEquipmentDetails";
    public const string CanDeleteEquipment = "CanDeleteEquipment";

    // Work Flow Forms - Questions
    public const string CanViewQuestions = "CanViewQuestions";
    public const string CanCreateNewQuestions = "CanCreateNewQuestions";
    public const string CanEditQuestions = "CanEditQuestions";
    public const string CanDeleteQuestions = "CanDeleteQuestions";

    // Work Flow Forms - Templates
    public const string CanViewTemplates = "CanViewTemplates";
    public const string CanCreateNewTemplates = "CanCreateNewTemplates";
    public const string CanEditTemplates = "CanEditTemplates";
    public const string CanDeleteTemplates = "CanDeleteTemplates";

    // Quality Control
    public const string CanViewMaterialSTPs = "CanViewMaterialSTPs";
    public const string CanCreateMaterialSTP = "CanCreateMaterialSTP";
    public const string CanEditMaterialSTP = "CanEditMaterialSTP";
    public const string CanDeleteMaterialSTP = "CanDeleteMaterialSTP";
    public const string CanViewProductSTPs = "CanViewProductSTPs";
    public const string CanCreateProductSTP = "CanCreateProductSTP";
    public const string CanEditProductSTP = "CanEditProductSTP";
    public const string CanDeleteProductSTP = "CanDeleteProductSTP";
    public const string CanViewMaterialARDs = "CanViewMaterialARDs";
    public const string CanCreateMaterialARD = "CanCreateMaterialARD";
    public const string CanEditMaterialARD = "CanEditMaterialARD";
    public const string CanDeleteMaterialARD = "CanDeleteMaterialARD";
    public const string CanViewProductARDs = "CanViewProductARDs";
    public const string CanCreateProductARD = "CanCreateProductARD";
    public const string CanEditProductARD = "CanEditProductARD";
    public const string CanDeleteProductARD = "CanDeleteProductARD";
    public const string CanViewGoodsReceiptNotes = "CanViewGoodsReceiptNotes";
    public const string CanTakeSample = "CanTakeSample";
    public const string CanStartTest = "CanStartTest";
    public const string CanViewMaterialSpecifications = "CanViewMaterialSpecifications";
    public const string CanCreateMaterialSpecification = "CanCreateMaterialSpecification";
    public const string CanEditMaterialSpecification = "CanEditMaterialSpecification";
    public const string CanDeleteMaterialSpecification = "CanDeleteMaterialSpecification";
    public const string CanViewProductSpecifications = "CanViewProductSpecifications";
    public const string CanCreateProductSpecification = "CanCreateProductSpecification";
    public const string CanEditProductSpecification = "CanEditProductSpecification";
    public const string CanDeleteProductSpecification = "CanDeleteProductSpecification";

    // Quality Assurance
    public const string CanViewIssuedBmRs = "CanViewIssuedBMRs";
    public const string CanIssueBmr = "CanIssueBMR";
    public const string CanViewBmrbprRequests = "CanViewBMRBPRRequests";
    public const string CanCreateBmrbprRequest = "CanCreateBMRBPRRequest";
    public const string CanApproveOrRejectBmrbprRequest = "CanApproveOrRejectBMRBPRRequest";
    public const string CanViewAnalyticalTestRequests = "CanViewAnalyticalTestRequests";
    public const string CanCreateAnalyticalTestRequest = "CanCreateAnalyticalTestRequest";
    public const string CanTakeSamples = "CanTakeSamples";
    public const string CanStartTestAnalytical = "CanStartTestAnalytical";
    public const string CanViewPendingApprovals = "CanViewPendingApprovals";

    // Inventory
    public const string CanViewVendorsInventory = "CanViewVendorsInventory";
    public const string CanCreateVendorInventory = "CanCreateVendorInventory";
    public const string CanEditVendorInventory = "CanEditVendorInventory";
    public const string CanDeleteVendorInventory = "CanDeleteVendorInventory";
    public const string CanViewPurchaseRequisitionsInventory = "CanViewPurchaseRequisitionsInventory";
    public const string CanCreatePurchaseRequisitionInventory = "CanCreatePurchaseRequisitionInventory";
    public const string CanEditPurchaseRequisitionInventory = "CanEditPurchaseRequisitionInventory";
    public const string CanDeletePurchaseRequisitionInventory = "CanDeletePurchaseRequisitionInventory";
    public const string CanViewStockRequisitionsInventory = "CanViewStockRequisitionsInventory";
    public const string CanCreateStockRequisitionInventory = "CanCreateStockRequisitionInventory";
    public const string CanIssueOrRejectStockRequisitions = "CanIssueOrRejectStockRequisitions";

    // CRM
    public const string CanViewCustomers = "CanViewCustomers";
    public const string CanCreateCustomer = "CanCreateCustomer";
    public const string CanEditCustomer = "CanEditCustomer";
    public const string CanDeleteCustomer = "CanDeleteCustomer";
    public const string CanViewOrder = "CanViewOrder";
    public const string CanCreateOrders = "CanCreateOrders";
    public const string CanEditOrders = "CanEditOrders";
    public const string CanDeleteOrders = "CanDeleteOrders";
    public const string CanViewProformaInvoice = "CanViewProformaInvoice";
    public const string CanCreateProformaInvoice = "CanCreateProformaInvoice";
    public const string CanEditProformaInvoice = "CanEditProformaInvoice";
    public const string CanCancelProformaInvoice = "CanCancelProformaInvoice";
    public const string CanViewInvoice = "CanViewInvoice";
    public const string CanCreateInvoice = "CanCreateInvoice";
    public const string CanCancelInvoice = "CanCancelInvoice";
    public const string CanViewWaybillCrm = "CanViewWaybillCRM";
    public const string CanCreateWaybillCrm = "CanCreateWaybillCRM";
}

public static class PermissionUtils
{
    public static IEnumerable<PermissionDto> GeneratePermissions()
    {
        // Helper function to generate a basic description
        string CreateDescription(string name) => $"Allows the user to {char.ToLower(name[0]) + name.Substring(1)}.";

        return new List<PermissionDto>
        {
            // Procurement
            new(PermissionModules.Procurement, PermissionSubmodules.Manufacturers, PermissionKeys.CanCreateManufacturer, "Can Create manufacturer", CreateDescription("Create manufacturer")),
            new(PermissionModules.Procurement, PermissionSubmodules.Manufacturers, PermissionKeys.CanViewManufacturerDetails, "Can View manufacturer details", CreateDescription("View manufacturer details")),
            new(PermissionModules.Procurement, PermissionSubmodules.Manufacturers, PermissionKeys.CanUpdateManufacturerDetails, "Can Update manufacturer details", CreateDescription("Update manufacturer details")),
            new(PermissionModules.Procurement, PermissionSubmodules.Manufacturers, PermissionKeys.CanDeleteManufacturer, "Can Delete manufacturer", CreateDescription("Delete manufacturer")),

            new(PermissionModules.Procurement, PermissionSubmodules.Vendors, PermissionKeys.CanCreateVendor, "Can Create vendor", CreateDescription("Create vendor")),
            new(PermissionModules.Procurement, PermissionSubmodules.Vendors, PermissionKeys.CanViewVendorDetails, "Can View Vendor details", CreateDescription("View Vendor details")),
            new(PermissionModules.Procurement, PermissionSubmodules.Vendors, PermissionKeys.CanUpdateVendorDetails, "Can Update Vendor details", CreateDescription("Update Vendor details")),
            new(PermissionModules.Procurement, PermissionSubmodules.Vendors, PermissionKeys.CanDeleteVendor, "Can Delete Vendor", CreateDescription("Delete Vendor")), // Note slight name variation from key

            new(PermissionModules.Procurement, PermissionSubmodules.PurchaseRequisition, PermissionKeys.CanViewPurchaseRequisitions, "Can View purchase requisitions", CreateDescription("View purchase requisitions")),
            new(PermissionModules.Procurement, PermissionSubmodules.PurchaseRequisition, PermissionKeys.CanSourceItemsBasedOnRequisition, "Can Source Items based on requisition", CreateDescription("Source Items based on requisition")),

            new(PermissionModules.Procurement, PermissionSubmodules.QuotationsRequest, PermissionKeys.CanSendQuotationRequest, "Can Send quotation request", CreateDescription("Send quotation request")),

            new(PermissionModules.Procurement, PermissionSubmodules.QuotationsResponses, PermissionKeys.CanInputResponses, "Can Input responses", CreateDescription("Input responses")),

            new(PermissionModules.Procurement, PermissionSubmodules.PriceComparison, PermissionKeys.CanSelectVendorPricing, "Can Select vendor pricing", CreateDescription("Select vendor pricing")),

            new(PermissionModules.Procurement, PermissionSubmodules.AwardedQuotations, PermissionKeys.CanSendAwardedQuotations, "Can Send awarded quotations", CreateDescription("Send awarded quotations")),

            new(PermissionModules.Procurement, PermissionSubmodules.ProformaResponses, PermissionKeys.CanUploadProformaInvoice, "Can Upload proforma invoice", CreateDescription("Upload proforma invoice")),

            new(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrders, PermissionKeys.CanCreatePurchaseOrder, "Can Create purchase order", CreateDescription("Create purchase order")),
            new(PermissionModules.Procurement, PermissionSubmodules.PurchaseOrders, PermissionKeys.CanReviseExistingPurchaseOrder, "Can Revise existing purchase order", CreateDescription("Revise existing purchase order")),

            new(PermissionModules.Procurement, PermissionSubmodules.MaterialDistribution, PermissionKeys.CanDistributeMaterials, "Can Distribute materials", CreateDescription("Distribute materials")),

            // Logistics
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanCreateShipmentInvoice, "Can Create shipment invoice", CreateDescription("Create shipment invoice")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanViewShipmentInvoice, "Can View shipment invoice", CreateDescription("View shipment invoice")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanEditShipmentInvoice, "Can Edit shipment invoice", CreateDescription("Edit shipment invoice")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentInvoice, PermissionKeys.CanDeleteShipmentInvoice, "Can Delete shipment invoice", CreateDescription("Delete shipment invoice")),

            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanCreateShipmentDocument, "Can Create Shipment document", CreateDescription("Create Shipment document")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanViewShipmentDocument, "Can View Shipment document", CreateDescription("View Shipment document")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanEditShipmentDocument, "Can Edit Shipment Document", CreateDescription("Edit Shipment Document")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanDeleteShipmentDocument, "Can Delete Shipment document", CreateDescription("Delete Shipment document")),
            new(PermissionModules.Logistics, PermissionSubmodules.ShipmentDocument, PermissionKeys.CanChangeShipmentDocumentStatus, "Change Shipment Document status", CreateDescription("Change Shipment Document status")),

            new(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanCreateBillingSheet, "Can Create billing sheet", CreateDescription("Create billing sheet")),
            new(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanViewBillingSheet, "Can View billing sheet", CreateDescription("View billing sheet")),
            new(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanEditBillingSheet, "Can Edit billing sheet", CreateDescription("Edit billing sheet")),
            new(PermissionModules.Logistics, PermissionSubmodules.BillingSheet, PermissionKeys.CanDeleteBillingSheet, "Can Delete billing sheet", CreateDescription("Delete billing sheet")),

            new(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanCreateWaybill, "Can Create waybill", CreateDescription("Create waybill")),
            new(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanViewWaybill, "Can View waybill", CreateDescription("View waybill")),
            new(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanEditWaybill, "Can Edit waybill", CreateDescription("Edit waybill")),
            new(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanDeleteWaybill, "Can Delete waybill", CreateDescription("Delete waybill")),
            new(PermissionModules.Logistics, PermissionSubmodules.Waybill, PermissionKeys.CanChangeWaybillStatus, "Change Waybill status", CreateDescription("Change Waybill status")),

            // Human Resources
            new(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanViewEmployee, "Can View Employee", CreateDescription("View Employee")),
            new(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanRegisterEmployee, "Can Register Employee", CreateDescription("Register Employee")),
            new(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanUpdateEmployeeDetails, "Can Update Employee Details", CreateDescription("Update Employee Details")),
            new(PermissionModules.HumanResources, PermissionSubmodules.EmployeeManagement, PermissionKeys.CanDeleteEmployee, "Can Delete Employee", CreateDescription("Delete Employee")),

            new(PermissionModules.HumanResources, PermissionSubmodules.UserManagement, PermissionKeys.CanViewUser, "Can View User", CreateDescription("View User")),
            new(PermissionModules.HumanResources, PermissionSubmodules.UserManagement, PermissionKeys.CanCreateUser, "Can Create User", CreateDescription("Create User")),
            new(PermissionModules.HumanResources, PermissionSubmodules.UserManagement, PermissionKeys.CanUpdateUserDetails, "Can Update User Details", CreateDescription("Update User Details")),
            new(PermissionModules.HumanResources, PermissionSubmodules.UserManagement, PermissionKeys.CanDeleteUser, "Can Delete User", CreateDescription("Delete User")),

            new(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanViewDesignation, "Can View Designation", CreateDescription("View Designation")),
            new(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanCreateDesignation, "Can Create Designation", CreateDescription("Create Designation")),
            new(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanEditDesignation, "Can Edit Designation", CreateDescription("Edit Designation")),
            new(PermissionModules.HumanResources, PermissionSubmodules.DesignationManagement, PermissionKeys.CanDeleteDesignation, "Can Delete Designation", CreateDescription("Delete Designation")),

            new(PermissionModules.HumanResources, PermissionSubmodules.RolesPermissionsManagement, PermissionKeys.CanViewRoles, "Can View Roles", CreateDescription("View Roles")),
            new(PermissionModules.HumanResources, PermissionSubmodules.RolesPermissionsManagement, PermissionKeys.CanCreateRoleAndAssignPermissions, "Can Create Role and assign permissions", CreateDescription("Create Role and assign permissions")),
            new(PermissionModules.HumanResources, PermissionSubmodules.RolesPermissionsManagement, PermissionKeys.CanEditRoleWithItsPermissions, "Can Edit Role with its permissions", CreateDescription("Edit Role with its permissions")),
            new(PermissionModules.HumanResources, PermissionSubmodules.RolesPermissionsManagement, PermissionKeys.CanDeleteRole, "Can Delete Role", CreateDescription("Delete Role")),

            new(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanViewLeaveRequests, "Can View Leave requests", CreateDescription("View Leave requests")),
            new(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanCreateLeaveRequest, "Can Create Leave request", CreateDescription("Create Leave request")),
            new(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanEditLeaveRequest, "Can Edit Leave request", CreateDescription("Edit Leave request")),
            new(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanDeleteOrCancelLeaveRequest, "Can Delete or cancel Leave request", CreateDescription("Delete or cancel Leave request")),
            new(PermissionModules.HumanResources, PermissionSubmodules.LeaveManagement, PermissionKeys.CanApproveOrRejectLeaveRequest, "Can Approve or reject Leave request", CreateDescription("Approve or reject Leave request")),

            // Warehouse (Receiving + Quarantine + Master)
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanViewReceivedRawMaterialsItems, "Can View received raw materials items", CreateDescription("View received raw materials items")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanViewReceivedPackagingMaterialsItems, "Can View received packaging materials items", CreateDescription("View received packaging materials items")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateChecklistForIncomingRawMaterialsGoods, "Can Create checklist for incoming raw materials goods", CreateDescription("Create checklist for incoming raw materials goods")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateChecklistForIncomingPackagingMaterialsGoods, "Can Create checklist for incoming packaging materials goods", CreateDescription("Create checklist for incoming packaging materials goods")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateGrnForRawMaterialsChecklistedItems, "Can Create GRN for raw materials checklisted items", CreateDescription("Create GRN for raw materials checklisted items")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ReceivingArea, PermissionKeys.CanCreateGrnForPackagingMaterialsChecklistedItems, "Can Create GRN for packaging materials checklisted items", CreateDescription("Create GRN for packaging materials checklisted items")),

            new(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanViewQuarantineRawMaterialsRecords, "Can View quarantine raw materials records", CreateDescription("View quarantine raw materials records")),
            new(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanViewQuarantinePackagingMaterialsRecords, "Can View quarantine packaging materials records", CreateDescription("View quarantine packaging materials records")),
            new(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanAssignRawMaterialsStockToStorageLocations, "Can Assign raw materials stock to storage locations", CreateDescription("Assign raw materials stock to storage locations")),
            new(PermissionModules.Warehouse, PermissionSubmodules.QuarantineAreaGrn, PermissionKeys.CanAssignPackagingMaterialsStockToStorageLocations, "Can Assign packaging materials stock to storage locations", CreateDescription("Assign packaging materials stock to storage locations")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Departments, PermissionKeys.CanViewDepartments, "Can View departments", CreateDescription("View departments")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Departments, PermissionKeys.CanCreateNewDepartment, "Can Create new department", CreateDescription("Create new department")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Departments, PermissionKeys.CanEditDepartment, "Can Edit department", CreateDescription("Edit department")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Warehouses, PermissionKeys.CanViewWarehouses, "Can View warehouses", CreateDescription("View warehouses")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Warehouses, PermissionKeys.CanAddWarehouse, "Can Add warehouse", CreateDescription("Add warehouse")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Warehouses, PermissionKeys.CanEditWarehouse, "Can Edit warehouse", CreateDescription("Edit warehouse")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Locations, PermissionKeys.CanViewLocations, "Can View locations", CreateDescription("View locations")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Locations, PermissionKeys.CanAddNewLocation, "Can Add new location", CreateDescription("Add new location")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Locations, PermissionKeys.CanEditLocation, "Can Edit location", CreateDescription("Edit location")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Racks, PermissionKeys.CanViewRacks, "Can View racks", CreateDescription("View racks")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Racks, PermissionKeys.CanAddNewRack, "Can Add new rack", CreateDescription("Add new rack")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Racks, PermissionKeys.CanEditRack, "Can Edit rack", CreateDescription("Edit rack")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Shelves, PermissionKeys.CanViewShelves, "Can View shelves", CreateDescription("View shelves")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Shelves, PermissionKeys.CanAddNewShelf, "Can Add new shelf", CreateDescription("Add new shelf")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Shelves, PermissionKeys.CanEditShelf, "Can Edit shelf", CreateDescription("Edit shelf")),

            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanViewRawMaterials, "Can View raw materials", CreateDescription("View raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanViewPackagingMaterials, "Can View packaging materials", CreateDescription("View packaging materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanCreateNewRawMaterials, "Can Create new raw materials", CreateDescription("Create new raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanCreateNewPackagingMaterials, "Can Create new packaging materials", CreateDescription("Create new packaging materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanEditRawMaterials, "Can Edit raw materials", CreateDescription("Edit raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanEditPackagingMaterials, "Can Edit packaging materials", CreateDescription("Edit packaging materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanDeleteRawMaterials, "Can Delete raw materials", CreateDescription("Delete raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.Materials, PermissionKeys.CanDeletePackagingMaterials, "Can Delete packaging materials", CreateDescription("Delete packaging materials")),

            new(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewApprovedRawMaterials, "Can View approved raw materials", CreateDescription("View approved raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.ApprovedMaterials, PermissionKeys.CanViewApprovedPackagingMaterials, "Can View approved packaging materials", CreateDescription("View approved packaging materials")),

            new(PermissionModules.Warehouse, PermissionSubmodules.RejectedMaterials, PermissionKeys.CanViewRejectedRawMaterials, "Can View rejected raw materials", CreateDescription("View rejected raw materials")),
            new(PermissionModules.Warehouse, PermissionSubmodules.RejectedMaterials, PermissionKeys.CanViewRejectedPackagingMaterials, "Can View rejected packaging materials", CreateDescription("View rejected packaging materials")), // Corrected typo from text

            new(PermissionModules.Warehouse, PermissionSubmodules.StockRequisitions, PermissionKeys.CanViewRawMaterialRequisitions, "Can View raw material requisitions", CreateDescription("View raw material requisitions")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockRequisitions, PermissionKeys.CanViewPackagingMaterialRequisitions, "Can View packaging material requisitions", CreateDescription("View packaging material requisitions")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockRequisitions, PermissionKeys.CanIssueRawMaterialRequisitions, "Can Issue raw material requisitions", CreateDescription("Issue raw material requisitions")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockRequisitions, PermissionKeys.CanIssuePackagingMaterialRequisitions, "Can Issue packaging material requisitions", CreateDescription("Issue packaging material requisitions")),

            new(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanViewRawMaterialTransferList, "Can View raw material transfer list", CreateDescription("View raw material transfer list")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanViewPackagingMaterialTransferList, "Can View packaging material transfer list", CreateDescription("View packaging material transfer list")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanIssueRawMaterialStockTransfers, "Can Issue raw material stock transfers", CreateDescription("Issue raw material stock transfers")),
            new(PermissionModules.Warehouse, PermissionSubmodules.StockTransferIssues, PermissionKeys.CanIssuePackagingMaterialStockTransfers, "Can Issue packaging material stock transfers", CreateDescription("Issue packaging material stock transfers")),

            new(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanViewRawMaterialLocationChartList, "Can View raw material location chart list", CreateDescription("View raw material location chart list")),
            new(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanViewPackagingMaterialLocationChartList, "Can View packaging material location chart list", CreateDescription("View packaging material location chart list")),
            new(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanReassignRawMaterialStock, "Can Reassign raw material stock", CreateDescription("Reassign raw material stock")),
            new(PermissionModules.Warehouse, PermissionSubmodules.LocationChartRecord, PermissionKeys.CanReassignPackagingMaterialStock, "Can Reassign packaging material stock", CreateDescription("Reassign packaging material stock")),

            // Production
            new(PermissionModules.Production, PermissionSubmodules.RawMaterialRequisitions, PermissionKeys.CanCreateRawMaterialPurchaseRequisition, "Can Create raw material purchase requisition", CreateDescription("Create raw material purchase requisition")),
            new(PermissionModules.Production, PermissionSubmodules.PackageMaterialRequisitions, PermissionKeys.CanCreatePackagingMaterialPurchaseRequisition, "Can Create packaging material purchase requisition", CreateDescription("Create packaging material purchase requisition")),
            new(PermissionModules.Production, PermissionSubmodules.RawMaterialRequisitions, PermissionKeys.CanCreateRawMaterialStockRequisition, "Can Create raw material stock requisition", CreateDescription("Create raw material stock requisition")),
            new(PermissionModules.Production, PermissionSubmodules.PackageMaterialRequisitions, PermissionKeys.CanCreatePackagingMaterialStockRequisition, "Can Create packaging material stock requisition", CreateDescription("Create packaging material stock requisition")),
            new(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanCreateRawMaterialStockTransfer, "Can Create raw material stock transfer", CreateDescription("Create raw material stock transfer")),
            new(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanCreatePackagingMaterialStockTransfer, "Can Create packaging material stock transfer", CreateDescription("Create packaging material stock transfer")),

            new(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanViewPlannedProducts, "Can View planned products", CreateDescription("View planned products")),
            new(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanCreateNewProductionPlan, "Can Create new production plan", CreateDescription("Create new production plan")),
            new(PermissionModules.Production, PermissionSubmodules.Planning, PermissionKeys.CanEditProductionPlan, "Can Edit production plan", CreateDescription("Edit production plan")),

            new(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanViewStockTransferRequests, "Can View stock transfer requests", CreateDescription("View stock transfer requests")),
            new(PermissionModules.Production, PermissionSubmodules.StockTransferRequests, PermissionKeys.CanApproveOrRejectStockTransferRequest, "Can Approve or reject stock transfer request", CreateDescription("Approve or reject stock transfer request")),

            new(PermissionModules.Production, PermissionSubmodules.ProductSchedule, PermissionKeys.CanViewProductSchedules, "Can View product schedules", CreateDescription("View product schedules")),
            new(PermissionModules.Production, PermissionSubmodules.ProductSchedule, PermissionKeys.CanCreateProductSchedule, "Can Create product schedule", CreateDescription("Create product schedule")),
            new(PermissionModules.Production, PermissionSubmodules.ProductSchedule, PermissionKeys.CanUpdateProductSchedule, "Can Update product schedule", CreateDescription("Update product schedule")),

            // Settings: Audit, Configs, and System
            new(PermissionModules.Settings, PermissionSubmodules.SystemSettings, PermissionKeys.CanViewSystemSettings, "Can View system settings", CreateDescription("View system settings")),
            new(PermissionModules.Settings, PermissionSubmodules.UserSettings, PermissionKeys.CanViewUserSettings, "Can View user settings", CreateDescription("View user settings")),
            new(PermissionModules.Settings, PermissionSubmodules.AuditTrail, PermissionKeys.CanViewAuditLogs, "Can View audit logs", CreateDescription("View audit logs")),
            new(PermissionModules.Settings, PermissionSubmodules.AuditTrail, PermissionKeys.CanExportAuditLogs, "Can Export audit logs", CreateDescription("Export audit logs")),

            // Categories - Product Category
            new(PermissionModules.Settings, PermissionSubmodules.ProductCategory, PermissionKeys.CanViewProductCategories, "Can View product categories", CreateDescription("View product categories")),
            new(PermissionModules.Settings, PermissionSubmodules.ProductCategory, PermissionKeys.CanCreateNewProductCategory, "Can Create new product category", CreateDescription("Create new product category")),
            new(PermissionModules.Settings, PermissionSubmodules.ProductCategory, PermissionKeys.CanEditProductCategory, "Can Edit product category", CreateDescription("Edit product category")),
            new(PermissionModules.Settings, PermissionSubmodules.ProductCategory, PermissionKeys.CanDeleteProductCategory, "Can Delete product category", CreateDescription("Delete product category")),

            // Categories - Raw Category
            new(PermissionModules.Settings, PermissionSubmodules.RawCategory, PermissionKeys.CanViewRawCategories, "Can View raw categories", CreateDescription("View raw categories")),
            new(PermissionModules.Settings, PermissionSubmodules.RawCategory, PermissionKeys.CanCreateNewRawCategory, "Can Create new raw category", CreateDescription("Create new raw category")),
            new(PermissionModules.Settings, PermissionSubmodules.RawCategory, PermissionKeys.CanEditRawCategory, "Can Edit raw category", CreateDescription("Edit raw category")),
            new(PermissionModules.Settings, PermissionSubmodules.RawCategory, PermissionKeys.CanDeleteRawCategory, "Can Delete raw category", CreateDescription("Delete raw category")),

            // Categories - Package Category
            new(PermissionModules.Settings, PermissionSubmodules.PackageCategory, PermissionKeys.CanViewPackageCategories, "Can View package categories", CreateDescription("View package categories")),
            new(PermissionModules.Settings, PermissionSubmodules.PackageCategory, PermissionKeys.CanCreateNewPackageCategory, "Can Create new package category", CreateDescription("Create new package category")),
            new(PermissionModules.Settings, PermissionSubmodules.PackageCategory, PermissionKeys.CanEditPackageCategory, "Can Edit package category", CreateDescription("Edit package category")),
            new(PermissionModules.Settings, PermissionSubmodules.PackageCategory, PermissionKeys.CanDeletePackageCategory, "Can Delete package category", CreateDescription("Delete package category")),

            // Procedures - Resource
            new(PermissionModules.Settings, PermissionSubmodules.Resource, PermissionKeys.CanViewResources, "Can View resources", CreateDescription("View resources")),
            new(PermissionModules.Settings, PermissionSubmodules.Resource, PermissionKeys.CanCreateNewResource, "Can Create new resource", CreateDescription("Create new resource")),
            new(PermissionModules.Settings, PermissionSubmodules.Resource, PermissionKeys.CanEditResource, "Can Edit resource", CreateDescription("Edit resource")),
            new(PermissionModules.Settings, PermissionSubmodules.Resource, PermissionKeys.CanDeleteResource, "Can Delete resource", CreateDescription("Delete resource")),

            // Procedures - Operation
            new(PermissionModules.Settings, PermissionSubmodules.Operation, PermissionKeys.CanViewOperations, "Can View operations", CreateDescription("View operations")),
            new(PermissionModules.Settings, PermissionSubmodules.Operation, PermissionKeys.CanCreateNewOperation, "Can Create new operation", CreateDescription("Create new operation")),
            new(PermissionModules.Settings, PermissionSubmodules.Operation, PermissionKeys.CanEditOperation, "Can Edit operation", CreateDescription("Edit operation")),
            new(PermissionModules.Settings, PermissionSubmodules.Operation, PermissionKeys.CanDeleteOperation, "Can Delete operation", CreateDescription("Delete operation")),

            // Procedures - Work Center
            new(PermissionModules.Settings, PermissionSubmodules.WorkCenter, PermissionKeys.CanViewWorkCenters, "Can View work centers", CreateDescription("View work centers")),
            new(PermissionModules.Settings, PermissionSubmodules.WorkCenter, PermissionKeys.CanCreateNewWorkCenter, "Can Create new work center", CreateDescription("Create new work center")),
            new(PermissionModules.Settings, PermissionSubmodules.WorkCenter, PermissionKeys.CanEditWorkCenter, "Can Edit work center", CreateDescription("Edit work center")),
            new(PermissionModules.Settings, PermissionSubmodules.WorkCenter, PermissionKeys.CanDeleteWorkCenter, "Can Delete work center", CreateDescription("Delete work center")),

            // Products - Material Type
            new(PermissionModules.Settings, PermissionSubmodules.MaterialType, PermissionKeys.CanViewMaterialTypes, "Can View material types", CreateDescription("View material types")),
            new(PermissionModules.Settings, PermissionSubmodules.MaterialType, PermissionKeys.CanCreateNewMaterialType, "Can Create new material type", CreateDescription("Create new material type")),
            new(PermissionModules.Settings, PermissionSubmodules.MaterialType, PermissionKeys.CanEditMaterialType, "Can Edit material type", CreateDescription("Edit material type")),
            new(PermissionModules.Settings, PermissionSubmodules.MaterialType, PermissionKeys.CanDeleteMaterialType, "Can Delete material type", CreateDescription("Delete material type")),

            // Products - Unit of Measure
            new(PermissionModules.Settings, PermissionSubmodules.UnitOfMeasure, PermissionKeys.CanViewUnitOfMeasure, "Can View unit of measure", CreateDescription("View unit of measure")),

            // Address - Country
            new(PermissionModules.Settings, PermissionSubmodules.Country, PermissionKeys.CanViewCountries, "Can View countries", CreateDescription("View countries")),

            // Container - Pack Style
            new(PermissionModules.Settings, PermissionSubmodules.PackStyle, PermissionKeys.CanViewPackStyles, "Can View pack styles", CreateDescription("View pack styles")),
            new(PermissionModules.Settings, PermissionSubmodules.PackStyle, PermissionKeys.CanCreateNewPackStyle, "Can Create new pack style", CreateDescription("Create new pack style")),
            new(PermissionModules.Settings, PermissionSubmodules.PackStyle, PermissionKeys.CanEditPackStyle, "Can Edit pack style", CreateDescription("Edit pack style")),
            new(PermissionModules.Settings, PermissionSubmodules.PackStyle, PermissionKeys.CanDeletePackStyle, "Can Delete pack style", CreateDescription("Delete pack style")),

            // Billing Sheet Charges
            new(PermissionModules.Settings, PermissionSubmodules.BillingSheetCharge, PermissionKeys.CanViewBillingCharges, "Can View billing charges", CreateDescription("View billing charges")),
            new(PermissionModules.Settings, PermissionSubmodules.BillingSheetCharge, PermissionKeys.CanCreateNewBillingSheetCharge, "Can Create new billing sheet charge", CreateDescription("Create new billing sheet charge")),
            new(PermissionModules.Settings, PermissionSubmodules.BillingSheetCharge, PermissionKeys.CanEditBillingSheetCharge, "Can Edit billing sheet charge", CreateDescription("Edit billing sheet charge")),
            new(PermissionModules.Settings, PermissionSubmodules.BillingSheetCharge, PermissionKeys.CanDeleteBillingSheetCharge, "Can Delete billing sheet charge", CreateDescription("Delete billing sheet charge")),

            // Terms of Payment
            new(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanViewPaymentTerms, "Can View payment terms", CreateDescription("View payment terms")),
            new(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanCreateNewPaymentTerm, "Can Create new payment term", CreateDescription("Create new payment term")),
            new(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanEditPaymentTerm, "Can Edit payment term", CreateDescription("Edit payment term")),
            new(PermissionModules.Settings, PermissionSubmodules.TermsOfPayment, PermissionKeys.CanDeletePaymentTerm, "Can Delete payment term", CreateDescription("Delete payment term")),

            // Delivery Mode
            new(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanViewDeliveryModes, "Can View delivery modes", CreateDescription("View delivery modes")),
            new(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanCreateNewDeliveryMode, "Can Create new delivery mode", CreateDescription("Create new delivery mode")),
            new(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanEditDeliveryMode, "Can Edit delivery mode", CreateDescription("Edit delivery mode")),
            new(PermissionModules.Settings, PermissionSubmodules.DeliveryMode, PermissionKeys.CanDeleteDeliveryMode, "Can Delete delivery mode", CreateDescription("Delete delivery mode")),

            // Code Settings
            new(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanViewCodeSettings, "Can View code settings", CreateDescription("View code settings")),
            new(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanAddNewCodes, "Can Add new codes", CreateDescription("Add new codes")),
            new(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanEditCodeSettings, "Can Edit code settings", CreateDescription("Edit code settings")),
            new(PermissionModules.Settings, PermissionSubmodules.CodeSettings, PermissionKeys.CanDeleteCodeSettings, "Can Delete code settings", CreateDescription("Delete code settings")),

            // Approvals
            new(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanViewApproval, "Can View approval", CreateDescription("View approval")),
            new(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanCreateOrConfigureNewApproval, "Can Create or configure new approval", CreateDescription("Create or configure new approval")),
            new(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanEditApprovalWorkflow, "Can Edit approval workflow", CreateDescription("Edit approval workflow")),
            new(PermissionModules.Settings, PermissionSubmodules.Approvals, PermissionKeys.CanDeleteOrDisableApprovals, "Can Delete or disable approvals", CreateDescription("Delete or disable approvals")),

            // Alerts & Notifications
            new(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanViewAlerts, "Can View alerts", CreateDescription("View alerts")),
            new(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanCreateNewAlerts, "Can Create new alerts", CreateDescription("Create new alerts")),
            new(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanEditAlerts, "Can Edit alerts", CreateDescription("Edit alerts")),
            new(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanEnableOrDisableAlerts, "Can Enable or disable alerts", CreateDescription("Enable or disable alerts")),
            new(PermissionModules.Settings, PermissionSubmodules.AlertsNotifications, PermissionKeys.CanDeleteAlerts, "Can Delete alerts", CreateDescription("Delete alerts")),

            // Equipment
            new(PermissionModules.Settings, PermissionSubmodules.Equipment, PermissionKeys.CanViewEquipments, "Can View equipments", CreateDescription("View equipments")),
            new(PermissionModules.Settings, PermissionSubmodules.Equipment, PermissionKeys.CanAddNewEquipment, "Can Add new equipment", CreateDescription("Add new equipment")),
            new(PermissionModules.Settings, PermissionSubmodules.Equipment, PermissionKeys.CanEditEquipmentDetails, "Can Edit equipment details", CreateDescription("Edit equipment details")),
            new(PermissionModules.Settings, PermissionSubmodules.Equipment, PermissionKeys.CanDeleteEquipment, "Can Delete equipment", CreateDescription("Delete equipment")),

            // Work Flow Forms - Questions
            new(PermissionModules.Settings, PermissionSubmodules.Questions, PermissionKeys.CanViewQuestions, "Can View questions", CreateDescription("View questions")),
            new(PermissionModules.Settings, PermissionSubmodules.Questions, PermissionKeys.CanCreateNewQuestions, "Can Create new questions", CreateDescription("Create new questions")),
            new(PermissionModules.Settings, PermissionSubmodules.Questions, PermissionKeys.CanEditQuestions, "Can Edit questions", CreateDescription("Edit questions")),
            new(PermissionModules.Settings, PermissionSubmodules.Questions, PermissionKeys.CanDeleteQuestions, "Can Delete questions", CreateDescription("Delete questions")),

            // Work Flow Forms - Templates
            new(PermissionModules.Settings, PermissionSubmodules.Templates, PermissionKeys.CanViewTemplates, "Can View templates", CreateDescription("View templates")),
            new(PermissionModules.Settings, PermissionSubmodules.Templates, PermissionKeys.CanCreateNewTemplates, "Can Create new templates", CreateDescription("Create new templates")),
            new(PermissionModules.Settings, PermissionSubmodules.Templates, PermissionKeys.CanEditTemplates, "Can Edit templates", CreateDescription("Edit templates")),
            new(PermissionModules.Settings, PermissionSubmodules.Templates, PermissionKeys.CanDeleteTemplates, "Can Delete templates", CreateDescription("Delete templates")),

            // Quality Control
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanViewMaterialSTPs, "Can View Material STPs", CreateDescription("View Material STPs")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanCreateMaterialSTP, "Can Create Material STP", CreateDescription("Create Material STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanEditMaterialSTP, "Can Edit Material STP", CreateDescription("Edit Material STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialStp, PermissionKeys.CanDeleteMaterialSTP, "Can Delete Material STP", CreateDescription("Delete Material STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanViewProductSTPs, "Can View Product STPs", CreateDescription("View Product STPs")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanCreateProductSTP, "Can Create Product STP", CreateDescription("Create Product STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanEditProductSTP, "Can Edit Product STP", CreateDescription("Edit Product STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductStp, PermissionKeys.CanDeleteProductSTP, "Can Delete Product STP", CreateDescription("Delete Product STP")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanViewMaterialARDs, "Can View Material ARDs", CreateDescription("View Material ARDs")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanCreateMaterialARD, "Can Create Material ARD", CreateDescription("Create Material ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanEditMaterialARD, "Can Edit Material ARD", CreateDescription("Edit Material ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialArd, PermissionKeys.CanDeleteMaterialARD, "Can Delete Material ARD", CreateDescription("Delete Material ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanViewProductARDs, "Can View Product ARDs", CreateDescription("View Product ARDs")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanCreateProductARD, "Can Create Product ARD", CreateDescription("Create Product ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanEditProductARD, "Can Edit Product ARD", CreateDescription("Edit Product ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductArd, PermissionKeys.CanDeleteProductARD, "Can Delete Product ARD", CreateDescription("Delete Product ARD")),
            new(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanViewGoodsReceiptNotes, "Can View Goods Receipt Notes", CreateDescription("View Goods Receipt Notes")),
            new(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanTakeSample, "Can Take Sample", CreateDescription("Take Sample")),
            new(PermissionModules.QualityControl, PermissionSubmodules.GoodsReceiptNote, PermissionKeys.CanStartTest, "Can Start Test", CreateDescription("Start Test")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanViewMaterialSpecifications, "Can View Material Specifications", CreateDescription("View Material Specifications")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanCreateMaterialSpecification, "Can Create Material Specification", CreateDescription("Create Material Specification")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanEditMaterialSpecification, "Can Edit Material Specification", CreateDescription("Edit Material Specification")),
            new(PermissionModules.QualityControl, PermissionSubmodules.MaterialSpecification, PermissionKeys.CanDeleteMaterialSpecification, "Can Delete Material Specification", CreateDescription("Delete Material Specification")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanViewProductSpecifications, "Can View Product Specifications", CreateDescription("View Product Specifications")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanCreateProductSpecification, "Can Create Product Specification", CreateDescription("Create Product Specification")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanEditProductSpecification, "Can Edit Product Specification", CreateDescription("Edit Product Specification")),
            new(PermissionModules.QualityControl, PermissionSubmodules.ProductSpecification, PermissionKeys.CanDeleteProductSpecification, "Can Delete Product Specification", CreateDescription("Delete Product Specification")),

            // Quality Assurance
            new(PermissionModules.QualityAssurance, PermissionSubmodules.IssueBmr, PermissionKeys.CanViewIssuedBmRs, "Can View Issued BMRs", CreateDescription("View Issued BMRs")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.IssueBmr, PermissionKeys.CanIssueBmr, "Can Issue BMR", CreateDescription("Issue BMR")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.BmrBprRequest, PermissionKeys.CanViewBmrbprRequests, "Can View BMR/BPR Requests", CreateDescription("View BMR/BPR Requests")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.BmrBprRequest, PermissionKeys.CanCreateBmrbprRequest, "Can Create BMR/BPR Request", CreateDescription("Create BMR/BPR Request")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.BmrBprRequest, PermissionKeys.CanApproveOrRejectBmrbprRequest, "Can Approve or Reject BMR/BPR Request", CreateDescription("Approve or Reject BMR/BPR Request")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanViewAnalyticalTestRequests, "Can View Analytical Test Requests", CreateDescription("View Analytical Test Requests")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanCreateAnalyticalTestRequest, "Can Create Analytical Test Request", CreateDescription("Create Analytical Test Request")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanTakeSamples, "Can Take Samples", CreateDescription("Take Samples")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.AnalyticalTestRequests, PermissionKeys.CanStartTestAnalytical, "Can Start Test", CreateDescription("Start Test")),
            new(PermissionModules.QualityAssurance, PermissionSubmodules.PendingApprovals, PermissionKeys.CanViewPendingApprovals, "Can View Pending Approvals", CreateDescription("View Pending Approvals")),

            // Inventory
            new(PermissionModules.Inventory, PermissionSubmodules.VendorManagement, PermissionKeys.CanViewVendorsInventory, "Can View Vendors", CreateDescription("View Vendors")),
            new(PermissionModules.Inventory, PermissionSubmodules.VendorManagement, PermissionKeys.CanCreateVendorInventory, "Can Create Vendor", CreateDescription("Create Vendor")),
            new(PermissionModules.Inventory, PermissionSubmodules.VendorManagement, PermissionKeys.CanEditVendorInventory, "Can Edit Vendor", CreateDescription("Edit Vendor")),
            new(PermissionModules.Inventory, PermissionSubmodules.VendorManagement, PermissionKeys.CanDeleteVendorInventory, "Can Delete Vendor", CreateDescription("Delete Vendor")),
            new(PermissionModules.Inventory, PermissionSubmodules.PurchaseRequisitionInventory, PermissionKeys.CanViewPurchaseRequisitionsInventory, "Can View Purchase Requisitions", CreateDescription("View Purchase Requisitions")),
            new(PermissionModules.Inventory, PermissionSubmodules.PurchaseRequisitionInventory, PermissionKeys.CanCreatePurchaseRequisitionInventory, "Can Create Purchase Requisition", CreateDescription("Create Purchase Requisition")),
            new(PermissionModules.Inventory, PermissionSubmodules.PurchaseRequisitionInventory, PermissionKeys.CanEditPurchaseRequisitionInventory, "Can Edit Purchase Requisition", CreateDescription("Edit Purchase Requisition")),
            new(PermissionModules.Inventory, PermissionSubmodules.PurchaseRequisitionInventory, PermissionKeys.CanDeletePurchaseRequisitionInventory, "Can Delete Purchase Requisition", CreateDescription("Delete Purchase Requisition")),
            new(PermissionModules.Inventory, PermissionSubmodules.StockRequisition, PermissionKeys.CanViewStockRequisitionsInventory, "Can View Stock Requisitions", CreateDescription("View Stock Requisitions")),
            new(PermissionModules.Inventory, PermissionSubmodules.StockRequisition, PermissionKeys.CanCreateStockRequisitionInventory, "Can Create Stock Requisition", CreateDescription("Create Stock Requisition")),
            new(PermissionModules.Inventory, PermissionSubmodules.StockRequisition, PermissionKeys.CanIssueOrRejectStockRequisitions, "Can Issue or reject Stock Requisitions", CreateDescription("Issue or reject Stock Requisitions")),

            // CRM
            new(PermissionModules.Crm, PermissionSubmodules.CustomerManagement, PermissionKeys.CanViewCustomers, "Can View Customers", CreateDescription("View Customers")),
            new(PermissionModules.Crm, PermissionSubmodules.CustomerManagement, PermissionKeys.CanCreateCustomer, "Can Create Customer", CreateDescription("Create Customer")),
            new(PermissionModules.Crm, PermissionSubmodules.CustomerManagement, PermissionKeys.CanEditCustomer, "Can Edit Customer", CreateDescription("Edit Customer")),
            new(PermissionModules.Crm, PermissionSubmodules.CustomerManagement, PermissionKeys.CanDeleteCustomer, "Can Delete Customer", CreateDescription("Delete Customer")),
            new(PermissionModules.Crm, PermissionSubmodules.ProductionOrders, PermissionKeys.CanViewOrder, "Can View Order", CreateDescription("View Order")),
            new(PermissionModules.Crm, PermissionSubmodules.ProductionOrders, PermissionKeys.CanCreateOrders, "Can Create Orders", CreateDescription("Create Orders")),
            new(PermissionModules.Crm, PermissionSubmodules.ProductionOrders, PermissionKeys.CanEditOrders, "Can Edit Orders", CreateDescription("Edit Orders")),
            new(PermissionModules.Crm, PermissionSubmodules.ProductionOrders, PermissionKeys.CanDeleteOrders, "Can Delete Orders", CreateDescription("Delete Orders")),
            new(PermissionModules.Crm, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanViewProformaInvoice, "Can View Proforma Invoice", CreateDescription("View Proforma Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanCreateProformaInvoice, "Can Create Proforma Invoice", CreateDescription("Create Proforma Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanEditProformaInvoice, "Can Edit Proforma Invoice", CreateDescription("Edit Proforma Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.ProformaInvoice, PermissionKeys.CanCancelProformaInvoice, "Can Cancel Proforma Invoice", CreateDescription("Cancel Proforma Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.Invoice, PermissionKeys.CanViewInvoice, "Can View Invoice", CreateDescription("View Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.Invoice, PermissionKeys.CanCreateInvoice, "Can Create Invoice", CreateDescription("Create Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.Invoice, PermissionKeys.CanCancelInvoice, "Can Cancel Invoice", CreateDescription("Cancel Invoice")),
            new(PermissionModules.Crm, PermissionSubmodules.WaybillCrm, PermissionKeys.CanViewWaybillCrm, "Can View Waybill", CreateDescription("View Waybill")),
            new(PermissionModules.Crm, PermissionSubmodules.WaybillCrm, PermissionKeys.CanCreateWaybillCrm, "Can Create Waybill", CreateDescription("Create Waybill")),
        };
    }
}