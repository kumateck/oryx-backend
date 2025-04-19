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
}

// Note: PermissionSubmodules class seems less directly used by the desired structure,
// The structured text provides more specific submodule groupings.
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
        public const string QuarantineAreaGRN = "Quarantine Area / GRN";
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
            new(PermissionModules.Logistics, "Shipment Invoice", PermissionKeys.CanCreateShipmentInvoice, "Can Create shipment invoice", CreateDescription("Create shipment invoice")),
            new(PermissionModules.Logistics, "Shipment Invoice", PermissionKeys.CanViewShipmentInvoice, "Can View shipment invoice", CreateDescription("View shipment invoice")),
            new(PermissionModules.Logistics, "Shipment Invoice", PermissionKeys.CanEditShipmentInvoice, "Can Edit shipment invoice", CreateDescription("Edit shipment invoice")),
            new(PermissionModules.Logistics, "Shipment Invoice", PermissionKeys.CanDeleteShipmentInvoice, "Can Delete shipment invoice", CreateDescription("Delete shipment invoice")),

            new(PermissionModules.Logistics, "Shipment Document", PermissionKeys.CanCreateShipmentDocument, "Can Create Shipment document", CreateDescription("Create Shipment document")),
            new(PermissionModules.Logistics, "Shipment Document", PermissionKeys.CanViewShipmentDocument, "Can View Shipment document", CreateDescription("View Shipment document")),
            new(PermissionModules.Logistics, "Shipment Document", PermissionKeys.CanEditShipmentDocument, "Can Edit Shipment Document", CreateDescription("Edit Shipment Document")),
            new(PermissionModules.Logistics, "Shipment Document", PermissionKeys.CanDeleteShipmentDocument, "Can Delete Shipment document", CreateDescription("Delete Shipment document")),
            new(PermissionModules.Logistics, "Shipment Document", PermissionKeys.CanChangeShipmentDocumentStatus, "Change Shipment Document status", CreateDescription("Change Shipment Document status")),

            new(PermissionModules.Logistics, "Billing Sheet", PermissionKeys.CanCreateBillingSheet, "Can Create billing sheet", CreateDescription("Create billing sheet")),
            new(PermissionModules.Logistics, "Billing Sheet", PermissionKeys.CanViewBillingSheet, "Can View billing sheet", CreateDescription("View billing sheet")),
            new(PermissionModules.Logistics, "Billing Sheet", PermissionKeys.CanEditBillingSheet, "Can Edit billing sheet", CreateDescription("Edit billing sheet")),
            new(PermissionModules.Logistics, "Billing Sheet", PermissionKeys.CanDeleteBillingSheet, "Can Delete billing sheet", CreateDescription("Delete billing sheet")),

            new(PermissionModules.Logistics, "Waybill", PermissionKeys.CanCreateWaybill, "Can Create waybill", CreateDescription("Create waybill")),
            new(PermissionModules.Logistics, "Waybill", PermissionKeys.CanViewWaybill, "Can View waybill", CreateDescription("View waybill")),
            new(PermissionModules.Logistics, "Waybill", PermissionKeys.CanEditWaybill, "Can Edit waybill", CreateDescription("Edit waybill")),
            new(PermissionModules.Logistics, "Waybill", PermissionKeys.CanDeleteWaybill, "Can Delete waybill", CreateDescription("Delete waybill")),
            new(PermissionModules.Logistics, "Waybill", PermissionKeys.CanChangeWaybillStatus, "Change Waybill status", CreateDescription("Change Waybill status")),

            // Human Resources
            new(PermissionModules.HumanResources, "Employee Management", PermissionKeys.CanViewEmployee, "Can View Employee", CreateDescription("View Employee")),
            new(PermissionModules.HumanResources, "Employee Management", PermissionKeys.CanRegisterEmployee, "Can Register Employee", CreateDescription("Register Employee")),
            new(PermissionModules.HumanResources, "Employee Management", PermissionKeys.CanUpdateEmployeeDetails, "Can Update Employee Details", CreateDescription("Update Employee Details")),
            new(PermissionModules.HumanResources, "Employee Management", PermissionKeys.CanDeleteEmployee, "Can Delete Employee", CreateDescription("Delete Employee")),

            new(PermissionModules.HumanResources, "User Management", PermissionKeys.CanViewUser, "Can View User", CreateDescription("View User")),
            new(PermissionModules.HumanResources, "User Management", PermissionKeys.CanCreateUser, "Can Create User", CreateDescription("Create User")),
            new(PermissionModules.HumanResources, "User Management", PermissionKeys.CanUpdateUserDetails, "Can Update User Details", CreateDescription("Update User Details")),
            new(PermissionModules.HumanResources, "User Management", PermissionKeys.CanDeleteUser, "Can Delete User", CreateDescription("Delete User")),

            new(PermissionModules.HumanResources, "Designation Management", PermissionKeys.CanViewDesignation, "Can View Designation", CreateDescription("View Designation")),
            new(PermissionModules.HumanResources, "Designation Management", PermissionKeys.CanCreateDesignation, "Can Create Designation", CreateDescription("Create Designation")),
            new(PermissionModules.HumanResources, "Designation Management", PermissionKeys.CanEditDesignation, "Can Edit Designation", CreateDescription("Edit Designation")),
            new(PermissionModules.HumanResources, "Designation Management", PermissionKeys.CanDeleteDesignation, "Can Delete Designation", CreateDescription("Delete Designation")),

            new(PermissionModules.HumanResources, "Roles & Permissions Management", PermissionKeys.CanViewRoles, "Can View Roles", CreateDescription("View Roles")),
            new(PermissionModules.HumanResources, "Roles & Permissions Management", PermissionKeys.CanCreateRoleAndAssignPermissions, "Can Create Role & Assign Permissions", CreateDescription("Create Role & Assign Permissions")),
            new(PermissionModules.HumanResources, "Roles & Permissions Management", PermissionKeys.CanEditRoleWithItsPermissions, "Can Edit Role With Its Permissions", CreateDescription("Edit Role With Its Permissions")),
            new(PermissionModules.HumanResources, "Roles & Permissions Management", PermissionKeys.CanDeleteRole, "Can Delete A Role", CreateDescription("Delete A Role")), // Note slight name variation

            new(PermissionModules.HumanResources, "Leave Management", PermissionKeys.CanViewLeaveRequests, "Can View Leave Requests", CreateDescription("View Leave Requests")),
            new(PermissionModules.HumanResources, "Leave Management", PermissionKeys.CanCreateLeaveRequest, "Can Create Leave Request", CreateDescription("Create Leave Request")),
            new(PermissionModules.HumanResources, "Leave Management", PermissionKeys.CanEditLeaveRequest, "Can Edit Leave Request", CreateDescription("Edit Leave Request")),
            new(PermissionModules.HumanResources, "Leave Management", PermissionKeys.CanDeleteOrCancelLeaveRequest, "Can Delete/Cancel Leave Request", CreateDescription("Delete/Cancel Leave Request")),
            new(PermissionModules.HumanResources, "Leave Management", PermissionKeys.CanApproveOrRejectLeaveRequest, "Can Approve/Reject Leave Request", CreateDescription("Approve/Reject Leave Request")),

            // Warehouse
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanViewReceivedRawMaterialsItems, "Can View Received Raw Materials Items", CreateDescription("View Received Raw Materials Items")),
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanViewReceivedPackagingMaterialsItems, "Can View Received Packaging Materials Items", CreateDescription("View Received Packaging Materials Items")),
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanCreateChecklistForIncomingRawMaterialsGoods, "Can Create Checklist for Incoming Raw Materials Goods", CreateDescription("Create Checklist for Incoming Raw Materials Goods")),
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanCreateChecklistForIncomingPackagingMaterialsGoods, "Can Create Checklist for Incoming Packaging Materials Goods", CreateDescription("Create Checklist for Incoming Packaging Materials Goods")),
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanCreateGrnForRawMaterialsChecklistedItems, "Can Create GRN for Raw Materials Checklisted items", CreateDescription("Create GRN for Raw Materials Checklisted items")),
            new(PermissionModules.Warehouse, "Receiving Area", PermissionKeys.CanCreateGrnForPackagingMaterialsChecklistedItems, "Can Create GRN for Packaging Materials Checklisted items", CreateDescription("Create GRN for Packaging Materials Checklisted items")),

            new(PermissionModules.Warehouse, "Quarantine Area / GRN", PermissionKeys.CanViewQuarantineRawMaterialsRecords, "Can View Quarantine Raw Materials Records", CreateDescription("View Quarantine Raw Materials Records")),
            new(PermissionModules.Warehouse, "Quarantine Area / GRN", PermissionKeys.CanViewQuarantinePackagingMaterialsRecords, "Can View Quarantine Packaging Materials Records", CreateDescription("View Quarantine Packaging Materials Records")),
            new(PermissionModules.Warehouse, "Quarantine Area / GRN", PermissionKeys.CanAssignRawMaterialsStockToStorageLocations, "Can Assign Raw Materials Stock to Storage locations", CreateDescription("Assign Raw Materials Stock to Storage locations")),
            new(PermissionModules.Warehouse, "Quarantine Area / GRN", PermissionKeys.CanAssignPackagingMaterialsStockToStorageLocations, "Can Assign Packaging Materials Stock to Storage locations", CreateDescription("Assign Packaging Materials Stock to Storage locations")),

            new(PermissionModules.Warehouse, "Departments", PermissionKeys.CanViewDepartments, "Can View Departments", CreateDescription("View Departments")),
            new(PermissionModules.Warehouse, "Departments", PermissionKeys.CanCreateNewDepartment, "Can Create New Department", CreateDescription("Create New Department")),
            new(PermissionModules.Warehouse, "Departments", PermissionKeys.CanEditDepartment, "Can Edit Department", CreateDescription("Edit Department")),

            new(PermissionModules.Warehouse, "Warehouses", PermissionKeys.CanViewWarehouses, "Can View Warehouses", CreateDescription("View Warehouses")),
            new(PermissionModules.Warehouse, "Warehouses", PermissionKeys.CanAddWarehouse, "Can Add Warehouse", CreateDescription("Add Warehouse")),
            new(PermissionModules.Warehouse, "Warehouses", PermissionKeys.CanEditWarehouse, "Can Edit Warehouse", CreateDescription("Edit Warehouse")),

            new(PermissionModules.Warehouse, "Locations", PermissionKeys.CanViewLocations, "Can View Locations", CreateDescription("View Locations")),
            new(PermissionModules.Warehouse, "Locations", PermissionKeys.CanAddNewLocation, "Can Add New Location", CreateDescription("Add New Location")),
            new(PermissionModules.Warehouse, "Locations", PermissionKeys.CanEditLocation, "Can Edit Location", CreateDescription("Edit Location")),

            new(PermissionModules.Warehouse, "Racks", PermissionKeys.CanViewRacks, "Can View Racks", CreateDescription("View Racks")),
            new(PermissionModules.Warehouse, "Racks", PermissionKeys.CanAddNewRack, "Can Add New Rack", CreateDescription("Add New Rack")),
            new(PermissionModules.Warehouse, "Racks", PermissionKeys.CanEditRack, "Can Edit Rack", CreateDescription("Edit Rack")),

            new(PermissionModules.Warehouse, "Shelves", PermissionKeys.CanViewShelves, "Can View Shelves", CreateDescription("View Shelves")), // Corrected based on key name
            new(PermissionModules.Warehouse, "Shelves", PermissionKeys.CanAddNewShelf, "Can Add New Shelf", CreateDescription("Add New Shelf")), // Corrected based on key name
            new(PermissionModules.Warehouse, "Shelves", PermissionKeys.CanEditShelf, "Can Edit Shelf", CreateDescription("Edit Shelf")), // Corrected based on key name

            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanViewRawMaterials, "Can View Raw Materials", CreateDescription("View Raw Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanViewPackagingMaterials, "Can View Packaging Materials", CreateDescription("View Packaging Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanCreateNewRawMaterials, "Can Create New Raw Materials", CreateDescription("Create New Raw Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanCreateNewPackagingMaterials, "Can Create New Packaging Materials", CreateDescription("Create New Packaging Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanEditRawMaterials, "Can Edit Raw Materials", CreateDescription("Edit Raw Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanEditPackagingMaterials, "Can Edit Packaging Materials", CreateDescription("Edit Packaging Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanDeleteRawMaterials, "Can Delete Raw Materials", CreateDescription("Delete Raw Materials")),
            new(PermissionModules.Warehouse, "Materials", PermissionKeys.CanDeletePackagingMaterials, "Can Delete Packaging Materials", CreateDescription("Delete Packaging Materials")),

            new(PermissionModules.Warehouse, "Approved Materials", PermissionKeys.CanViewApprovedRawMaterials, "Can View Approved Raw Materials", CreateDescription("View Approved Raw Materials")),
            new(PermissionModules.Warehouse, "Approved Materials", PermissionKeys.CanViewApprovedPackagingMaterials, "Can View Approved Packaging Materials", CreateDescription("View Approved Packaging Materials")),

            new(PermissionModules.Warehouse, "Rejected Materials", PermissionKeys.CanViewRejectedRawMaterials, "Can View Rejected Raw Materials", CreateDescription("View Rejected Raw Materials")),
            new(PermissionModules.Warehouse, "Rejected Materials", PermissionKeys.CanViewRejectedPackagingMaterials, "Can View Rejected Packaging Materials", CreateDescription("View Rejected Packaging Materials")), // Used correct key

            new(PermissionModules.Warehouse, "Stock Requisitions", PermissionKeys.CanViewRawMaterialRequisitions, "Can View Raw Material Requisitions", CreateDescription("View Raw Material Requisitions")),
            new(PermissionModules.Warehouse, "Stock Requisitions", PermissionKeys.CanViewPackagingMaterialRequisitions, "Can View Packaging Material Requisitions", CreateDescription("View Packaging Material Requisitions")),
            new(PermissionModules.Warehouse, "Stock Requisitions", PermissionKeys.CanIssueRawMaterialRequisitions, "Can Issue Raw Material Requisitions", CreateDescription("Issue Raw Material Requisitions")),
            new(PermissionModules.Warehouse, "Stock Requisitions", PermissionKeys.CanIssuePackagingMaterialRequisitions, "Can Issue Packaging Material Requisitions", CreateDescription("Issue Packaging Material Requisitions")),

            new(PermissionModules.Warehouse, "Stock Transfer Issues", PermissionKeys.CanViewRawMaterialTransferList, "Can View Raw Material Transfer List", CreateDescription("View Raw Material Transfer List")),
            new(PermissionModules.Warehouse, "Stock Transfer Issues", PermissionKeys.CanViewPackagingMaterialTransferList, "Can View Packaging Material Transfer List", CreateDescription("View Packaging Material Transfer List")),
            new(PermissionModules.Warehouse, "Stock Transfer Issues", PermissionKeys.CanIssueRawMaterialStockTransfers, "Can Issue Raw Material Stock Transfers", CreateDescription("Issue Raw Material Stock Transfers")),
            new(PermissionModules.Warehouse, "Stock Transfer Issues", PermissionKeys.CanIssuePackagingMaterialStockTransfers, "Can Issue Packaging Material Stock Transfers", CreateDescription("Issue Packaging Material Stock Transfers")),

            new(PermissionModules.Warehouse, "Location Chart Record", PermissionKeys.CanViewRawMaterialLocationChartList, "Can View Raw Material Location Chart List", CreateDescription("View Raw Material Location Chart List")),
            new(PermissionModules.Warehouse, "Location Chart Record", PermissionKeys.CanViewPackagingMaterialLocationChartList, "Can View Packaging Material Location Chart List", CreateDescription("View Packaging Material Location Chart List")),
            new(PermissionModules.Warehouse, "Location Chart Record", PermissionKeys.CanReassignRawMaterialStock, "Can Reassign Raw Material Stock", CreateDescription("Reassign Raw Material Stock")),
            new(PermissionModules.Warehouse, "Location Chart Record", PermissionKeys.CanReassignPackagingMaterialStock, "Can Reassign Packaging Material Stock", CreateDescription("Reassign Packaging Material Stock")),

            // Production
            // Grouping the creation keys under 'Requisitions' as per text structure, though they are broad production actions.
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreateRawMaterialPurchaseRequisition, "Can Create Raw Material Purchase Requisition", CreateDescription("Create Raw Material Purchase Requisition")),
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreatePackagingMaterialPurchaseRequisition, "Can Create Packaging Material Purchase Requisition", CreateDescription("Create Packaging Material Purchase Requisition")),
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreateRawMaterialStockRequisition, "Can Create Raw Material Stock Requisition", CreateDescription("Create Raw Material Stock Requisition")),
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreatePackagingMaterialStockRequisition, "Can Create Packaging Material Stock Requisition", CreateDescription("Create Packaging Material Stock Requisition")),
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreateRawMaterialStockTransfer, "Can Create Raw Material Stock Transfer", CreateDescription("Create Raw Material Stock Transfer")),
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanCreatePackagingMaterialStockTransfer, "Can Create Packaging Material Stock Transfer", CreateDescription("Create Packaging Material Stock Transfer")),

            // These were listed under REQUISITIONS->Raw Material Requisitions in the text, reusing the Warehouse view key here.
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanViewRawMaterialRequisitions, "Can View Raw Material Requisitions", CreateDescription("View Raw Material Requisitions")),
            // These were listed under REQUISITIONS->Package Material Requisitions in the text, reusing the Warehouse view key here.
            new(PermissionModules.Production, "Requisitions", PermissionKeys.CanViewPackagingMaterialRequisitions, "Can View Package Material Requisitions", CreateDescription("View Package Material Requisitions")),


            new(PermissionModules.Production, "Planning", PermissionKeys.CanViewPlannedProducts, "Can View Planned Products", CreateDescription("View Planned Products")),
            new(PermissionModules.Production, "Planning", PermissionKeys.CanCreateNewProductionPlan, "Can Create New Production Plan", CreateDescription("Create New Production Plan")),
            new(PermissionModules.Production, "Planning", PermissionKeys.CanEditProductionPlan, "Can Edit Production Plan", CreateDescription("Edit Production Plan")),

            new(PermissionModules.Production, "Stock Transfer Requests", PermissionKeys.CanViewStockTransferRequests, "Can View Stock Transfer Requests", CreateDescription("View Stock Transfer Requests")),
            new(PermissionModules.Production, "Stock Transfer Requests", PermissionKeys.CanApproveOrRejectStockTransferRequest, "Can Approve or Reject Stock Transfer Request", CreateDescription("Approve or Reject Stock Transfer Request")),

            new(PermissionModules.Production, "Product Schedule", PermissionKeys.CanViewProductSchedules, "Can View Product Schedules", CreateDescription("View Product Schedules")),
            new(PermissionModules.Production, "Product Schedule", PermissionKeys.CanCreateProductSchedule, "Can Create Product Schedule", CreateDescription("Create Product Schedule")),
            new(PermissionModules.Production, "Product Schedule", PermissionKeys.CanUpdateProductSchedule, "Can Update Product Schedule", CreateDescription("Update Product Schedule")),

            // Settings
            new(PermissionModules.Settings, "System Settings", PermissionKeys.CanViewSystemSettings, "Can View System Settings", CreateDescription("View System Settings")),
            new(PermissionModules.Settings, "User Settings", PermissionKeys.CanViewUserSettings, "Can View User Settings", CreateDescription("View User Settings")),
            new(PermissionModules.Settings, "Audit Trail", PermissionKeys.CanViewAuditLogs, "Can View Audit Logs", CreateDescription("View Audit Logs")),
            new(PermissionModules.Settings, "Audit Trail", PermissionKeys.CanExportAuditLogs, "Can Export Audit Logs", CreateDescription("Export Audit Logs")),

            // Settings - Configurations - Categories
            new(PermissionModules.Settings, "Product Category", PermissionKeys.CanViewProductCategories, "Can View Product Categories", CreateDescription("View Product Categories")),
            new(PermissionModules.Settings, "Product Category", PermissionKeys.CanCreateNewProductCategory, "Can Create New Product Category", CreateDescription("Create New Product Category")),
            new(PermissionModules.Settings, "Product Category", PermissionKeys.CanEditProductCategory, "Can Edit Product Category", CreateDescription("Edit Product Category")),
            new(PermissionModules.Settings, "Product Category", PermissionKeys.CanDeleteProductCategory, "Can Delete Product Category", CreateDescription("Delete Product Category")),

            new(PermissionModules.Settings, "Raw Category", PermissionKeys.CanViewRawCategories, "Can View Raw Categories", CreateDescription("View Raw Categories")),
            new(PermissionModules.Settings, "Raw Category", PermissionKeys.CanCreateNewRawCategory, "Can Create New Raw Category", CreateDescription("Create New Raw Category")),
            new(PermissionModules.Settings, "Raw Category", PermissionKeys.CanEditRawCategory, "Can Edit Raw Category", CreateDescription("Edit Raw Category")),
            new(PermissionModules.Settings, "Raw Category", PermissionKeys.CanDeleteRawCategory, "Can Delete Raw Category", CreateDescription("Delete Raw Category")),

            new(PermissionModules.Settings, "Package Category", PermissionKeys.CanViewPackageCategories, "Can View Package Categories", CreateDescription("View Package Categories")),
            new(PermissionModules.Settings, "Package Category", PermissionKeys.CanCreateNewPackageCategory, "Can Create New Package Category", CreateDescription("Create New Package Category")),
            new(PermissionModules.Settings, "Package Category", PermissionKeys.CanEditPackageCategory, "Can Edit Package Category", CreateDescription("Edit Package Category")),
            new(PermissionModules.Settings, "Package Category", PermissionKeys.CanDeletePackageCategory, "Can Delete Package Category", CreateDescription("Delete Package Category")),

            // Settings - Configurations - Procedures
            new(PermissionModules.Settings, "Resource", PermissionKeys.CanViewResources, "Can View Resources", CreateDescription("View Resources")),
            new(PermissionModules.Settings, "Resource", PermissionKeys.CanCreateNewResource, "Can Create New Resource", CreateDescription("Create New Resource")),
            new(PermissionModules.Settings, "Resource", PermissionKeys.CanEditResource, "Can Edit Resource", CreateDescription("Edit Resource")),
            new(PermissionModules.Settings, "Resource", PermissionKeys.CanDeleteResource, "Can Delete Resource", CreateDescription("Delete Resource")),

            new(PermissionModules.Settings, "Operation", PermissionKeys.CanViewOperations, "Can View Operations", CreateDescription("View Operations")),
            new(PermissionModules.Settings, "Operation", PermissionKeys.CanCreateNewOperation, "Can Create New Operation", CreateDescription("Create New Operation")),
            new(PermissionModules.Settings, "Operation", PermissionKeys.CanEditOperation, "Can Edit Operation", CreateDescription("Edit Operation")),
            new(PermissionModules.Settings, "Operation", PermissionKeys.CanDeleteOperation, "Can Delete Operation", CreateDescription("Delete Operation")),

            new(PermissionModules.Settings, "Work Center", PermissionKeys.CanViewWorkCenters, "Can View Work Centers", CreateDescription("View Work Centers")),
            new(PermissionModules.Settings, "Work Center", PermissionKeys.CanCreateNewWorkCenter, "Can Create New Work Center", CreateDescription("Create New Work Center")),
            new(PermissionModules.Settings, "Work Center", PermissionKeys.CanEditWorkCenter, "Can Edit Work Center", CreateDescription("Edit Work Center")),
            new(PermissionModules.Settings, "Work Center", PermissionKeys.CanDeleteWorkCenter, "Can Delete Work Center", CreateDescription("Delete Work Center")),

            // Settings - Configurations - Products
            new(PermissionModules.Settings, "Material Type", PermissionKeys.CanViewMaterialTypes, "Can View Material Types", CreateDescription("View Material Types")),
            new(PermissionModules.Settings, "Material Type", PermissionKeys.CanCreateNewMaterialType, "Can Create New Material Type", CreateDescription("Create New Material Type")),
            new(PermissionModules.Settings, "Material Type", PermissionKeys.CanEditMaterialType, "Can Edit Material Type", CreateDescription("Edit Material Type")),
            new(PermissionModules.Settings, "Material Type", PermissionKeys.CanDeleteMaterialType, "Can Delete Material Type", CreateDescription("Delete Material Type")),

            new(PermissionModules.Settings, "Unit of Measure", PermissionKeys.CanViewUnitOfMeasure, "Can View Unit of Measure", CreateDescription("View Unit of Measure")),

            // Settings - Configurations - Address
            new(PermissionModules.Settings, "Country", PermissionKeys.CanViewCountries, "Can View Countries", CreateDescription("View Countries")),

            // Settings - Configurations - Container
            new(PermissionModules.Settings, "Pack Style", PermissionKeys.CanViewPackStyles, "Can View Pack Styles", CreateDescription("View Pack Styles")),
            new(PermissionModules.Settings, "Pack Style", PermissionKeys.CanCreateNewPackStyle, "Can Create New Pack Style", CreateDescription("Create New Pack Style")),
            new(PermissionModules.Settings, "Pack Style", PermissionKeys.CanEditPackStyle, "Can Edit Pack Style", CreateDescription("Edit Pack Style")),
            new(PermissionModules.Settings, "Pack Style", PermissionKeys.CanDeletePackStyle, "Can Delete Pack Style", CreateDescription("Delete Pack Style")),

            // Settings - Configurations - Billing Sheet Charge
            new(PermissionModules.Settings, "Billing Sheet Charge", PermissionKeys.CanViewBillingCharges, "Can View Billing Charges", CreateDescription("View Billing Charges")),
            new(PermissionModules.Settings, "Billing Sheet Charge", PermissionKeys.CanCreateNewBillingSheetCharge, "Can Create New Billing Sheet Charge", CreateDescription("Create New Billing Sheet Charge")),
            new(PermissionModules.Settings, "Billing Sheet Charge", PermissionKeys.CanEditBillingSheetCharge, "Can Edit Billing Sheet Charge", CreateDescription("Edit Billing Sheet Charge")),
            new(PermissionModules.Settings, "Billing Sheet Charge", PermissionKeys.CanDeleteBillingSheetCharge, "Can Delete Billing Sheet Charge", CreateDescription("Delete Billing Sheet Charge")),

            // Settings - Configurations - Terms of Payment
            new(PermissionModules.Settings, "Terms of Payment", PermissionKeys.CanViewPaymentTerms, "Can View Payment Terms", CreateDescription("View Payment Terms")),
            new(PermissionModules.Settings, "Terms of Payment", PermissionKeys.CanCreateNewPaymentTerm, "Can Create New Payment Term", CreateDescription("Create New Payment Term")),
            new(PermissionModules.Settings, "Terms of Payment", PermissionKeys.CanEditPaymentTerm, "Can Edit Payment Term", CreateDescription("Edit Payment Term")),
            new(PermissionModules.Settings, "Terms of Payment", PermissionKeys.CanDeletePaymentTerm, "Can Delete Payment Term", CreateDescription("Delete Payment Term")),

            // Settings - Configurations - Delivery Mode
            new(PermissionModules.Settings, "Delivery Mode", PermissionKeys.CanViewDeliveryModes, "Can View Delivery Modes", CreateDescription("View Delivery Modes")),
            new(PermissionModules.Settings, "Delivery Mode", PermissionKeys.CanCreateNewDeliveryMode, "Can Create New Delivery Mode", CreateDescription("Create New Delivery Mode")),
            new(PermissionModules.Settings, "Delivery Mode", PermissionKeys.CanEditDeliveryMode, "Can Edit Delivery Mode", CreateDescription("Edit Delivery Mode")),
            new(PermissionModules.Settings, "Delivery Mode", PermissionKeys.CanDeleteDeliveryMode, "Can Delete Delivery Mode", CreateDescription("Delete Delivery Mode")),

            // Settings - Configurations - Code Settings
            new(PermissionModules.Settings, "Code Settings", PermissionKeys.CanViewCodeSettings, "Can View Code Settings", CreateDescription("View Code Settings")),
            new(PermissionModules.Settings, "Code Settings", PermissionKeys.CanAddNewCodes, "Can Add New Codes", CreateDescription("Add New Codes")),
            new(PermissionModules.Settings, "Code Settings", PermissionKeys.CanEditCodeSettings, "Can Edit Code Settings", CreateDescription("Edit Code Settings")),
            new(PermissionModules.Settings, "Code Settings", PermissionKeys.CanDeleteCodeSettings, "Can Delete Code Settings", CreateDescription("Delete Code Settings")),

            // Settings - Configurations - Approvals
            new(PermissionModules.Settings, "Approvals", PermissionKeys.CanViewApproval, "Can View Approval", CreateDescription("View Approval")),
            new(PermissionModules.Settings, "Approvals", PermissionKeys.CanCreateOrConfigureNewApproval, "Can Create/Configure New Approval", CreateDescription("Create/Configure New Approval")),
            new(PermissionModules.Settings, "Approvals", PermissionKeys.CanEditApprovalWorkflow, "Can Edit Approval Workflow", CreateDescription("Edit Approval Workflow")),
            new(PermissionModules.Settings, "Approvals", PermissionKeys.CanDeleteOrDisableApprovals, "Can Delete/Disable Approvals", CreateDescription("Delete/Disable Approvals")),

            // Settings - Configurations - Alerts & Notifications
            new(PermissionModules.Settings, "Alerts & Notifications", PermissionKeys.CanViewAlerts, "Can View Alerts", CreateDescription("View Alerts")),
            new(PermissionModules.Settings, "Alerts & Notifications", PermissionKeys.CanCreateNewAlerts, "Can Create New Alerts", CreateDescription("Create New Alerts")),
            new(PermissionModules.Settings, "Alerts & Notifications", PermissionKeys.CanEditAlerts, "Can Edit Alerts", CreateDescription("Edit Alerts")),
            new(PermissionModules.Settings, "Alerts & Notifications", PermissionKeys.CanEnableOrDisableAlerts, "Can Enable/Disable Alerts", CreateDescription("Enable/Disable Alerts")),
            new(PermissionModules.Settings, "Alerts & Notifications", PermissionKeys.CanDeleteAlerts, "Can Delete Alerts", CreateDescription("Delete Alerts")),

            // Settings - Configurations - Equipment
            new(PermissionModules.Settings, "Equipment", PermissionKeys.CanViewEquipments, "Can View Equipments", CreateDescription("View Equipments")),
            new(PermissionModules.Settings, "Equipment", PermissionKeys.CanAddNewEquipment, "Can Add New Equipment", CreateDescription("Add New Equipment")),
            new(PermissionModules.Settings, "Equipment", PermissionKeys.CanEditEquipmentDetails, "Can Edit Equipment Details", CreateDescription("Edit Equipment Details")),
            new(PermissionModules.Settings, "Equipment", PermissionKeys.CanDeleteEquipment, "Can Delete Equipment", CreateDescription("Delete Equipment")),

            // Settings - Configurations - Work Flow Forms
            new(PermissionModules.Settings, "Work Flow Forms - Questions", PermissionKeys.CanViewQuestions, "Can View Questions", CreateDescription("View Questions")),
            new(PermissionModules.Settings, "Work Flow Forms - Questions", PermissionKeys.CanCreateNewQuestions, "Can Create New Questions", CreateDescription("Create New Questions")),
            new(PermissionModules.Settings, "Work Flow Forms - Questions", PermissionKeys.CanEditQuestions, "Can Edit Questions", CreateDescription("Edit Questions")),
            new(PermissionModules.Settings, "Work Flow Forms - Questions", PermissionKeys.CanDeleteQuestions, "Can Delete Questions", CreateDescription("Delete Questions")),

            new(PermissionModules.Settings, "Work Flow Forms - Templates", PermissionKeys.CanViewTemplates, "Can View Templates", CreateDescription("View Templates")),
            new(PermissionModules.Settings, "Work Flow Forms - Templates", PermissionKeys.CanCreateNewTemplates, "Can Create New Templates", CreateDescription("Create New Templates")),
            new(PermissionModules.Settings, "Work Flow Forms - Templates", PermissionKeys.CanEditTemplates, "Can Edit Templates", CreateDescription("Edit Templates")),
            new(PermissionModules.Settings, "Work Flow Forms - Templates", PermissionKeys.CanDeleteTemplates, "Can Delete Templates", CreateDescription("Delete Templates")),

        };
    }
}