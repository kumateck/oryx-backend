namespace APP.Utils;

public class MenuItem(
    string module,
    List<string> requiredPermissionKey,
    List<MenuItem> children = null,
    string icon = null,
    string route = null,
    string name = null,
    int order = 0)
{
    public string Name { get; set; } = name ?? module;
    public string Module { get; set; } = module;
    public List<string> RequiredPermissionKey { get; set; } = requiredPermissionKey ?? [];
    public List<MenuItem> Children { get; set; } = children ?? [];
    public string Icon { get; set; } = icon;
    public string Route { get; set; } = route;
    public int Order { get; set; } = order;
    public bool IsVisible { get; set; } = true;

    public MenuItem Clone()
    {
        return new MenuItem(
            name: Name,
            module: Module,
            requiredPermissionKey: [.. RequiredPermissionKey],
            children: Children?.Select(child => child.Clone()).ToList(),
            icon: Icon,
            route: Route,
            order: Order
        );
    }
}

public static class MenuConfig
{
    public static List<MenuItem> MenuItems =
    [
        // Order 1: Dashboard
        new(
            name: "Dashboard",
            module: "Dashboard", // A conceptual module, not in permissions
            requiredPermissionKey: [], // Open to all logged-in users
            route: "/dashboard",
            icon: "dashboard",
            order: 1
        ),

        // Order 2: Procurement
        new(
            name: "Procurement",
            module: PermissionModules.Procurement,
            requiredPermissionKey: [],
            route: "/procurement/purchase-requisition",
            icon: "procurement",
            order: 2,
            children:
            [
                new("Purchase Requisition", [PermissionKeys.CanViewPurchaseRequisitions, PermissionKeys.CanSourcePurchaseRequisition], route: "/procurement/purchase-requisition", order: 1),
                new("Quotations Request", [PermissionKeys.CanViewForeignQuotation, PermissionKeys.CanSendForeignQuotationRequest, PermissionKeys.CanViewLocalQuotation, PermissionKeys.CanSendLocalQuotationRequest], route: "/procurement/quotations-request", order: 2),
                new("Quotations Responses", [PermissionKeys.CanViewForeignQuotationResponse, PermissionKeys.CanSendForeignQuotationResponse, PermissionKeys.CanViewLocalQuotationResponse, PermissionKeys.CanSendLocalQuotationResponse], route: "/procurement/quotations-responses", order: 3),
                new("Price Comparison", [PermissionKeys.CanViewForeignVendorPricing, PermissionKeys.CanApplyChangesForForeignVendorPricingSelection, PermissionKeys.CanViewLocalVendorPricing, PermissionKeys.CanApplyChangesForLocalVendorPricingSelection], route: "/procurement/price-comparison", order: 4),
                new("Proforma Request", [PermissionKeys.CanViewForeignProformaRequest, PermissionKeys.CanSendForeignProformaRequest, PermissionKeys.CanViewLocalProformaRequest, PermissionKeys.CanSendLocalProformaRequest], route: "/procurement/proforma-request", order: 5),
                new("Proforma Responses", [PermissionKeys.CanViewForeignProformaInvoiceSubmissions, PermissionKeys.CanSendForeignProformaInvoice, PermissionKeys.CanViewLocalProformaInvoiceSubmissions, PermissionKeys.CanSendLocalProformaInvoice], route: "/procurement/proforma-responses", order: 6),
                new("Create Purchase Orders", [PermissionKeys.CanViewForeignPurchaseOrder, PermissionKeys.CanCreateForeignPurchaseOrder, PermissionKeys.CanViewLocalPurchaseOrder, PermissionKeys.CanCreateLocalPurchaseOrder], route: "/procurement/create-purchase-orders", order: 7),
                new("Purchase Order List", [PermissionKeys.CanReviseForeignPurchaseOrder, PermissionKeys.CanReviseLocalPurchaseOrder], route: "/procurement/purchase-order-list", order: 8),
                new("Material Distribution", [PermissionKeys.CanViewMaterialDistribution, PermissionKeys.CanDistributeMaterial], route: "/procurement/material-distribution", order: 9)
            ]
        ),

        // Order 3: Logistics
        new(
            name: "Logistics",
            module: PermissionModules.Logistics,
            requiredPermissionKey: [],
            route: "/logistics/shipment-invoice",
            icon: "logistics",
            order: 3,
            children:
            [
                new(PermissionSubmodules.ShipmentInvoice, [PermissionKeys.CanCreateShipmentInvoice, PermissionKeys.CanViewShipmentInvoice, PermissionKeys.CanEditShipmentInvoice, PermissionKeys.CanDeleteShipmentInvoice], route: "/logistics/shipment-invoice", order: 1),
                new(PermissionSubmodules.ShipmentDocument, [PermissionKeys.CanCreateShipmentDocument, PermissionKeys.CanViewShipmentDocument, PermissionKeys.CanChangeShipmentDocumentStatus, PermissionKeys.CanEditShipmentDocument, PermissionKeys.CanDeleteShipmentDocument], route: "/logistics/shipment-document", order: 2),
                new(PermissionSubmodules.BillingSheet, [PermissionKeys.CanCreateBillingSheet, PermissionKeys.CanViewBillingSheet, PermissionKeys.CanEditBillingSheet], route: "/logistics/billing-sheet", order: 3),
                new(PermissionSubmodules.Waybill, [PermissionKeys.CanCreateWaybill, PermissionKeys.CanViewWaybill, PermissionKeys.CanChangeWaybillStatus], route: "/logistics/waybill", order: 4),
                new(PermissionSubmodules.AvailableStock, [PermissionKeys.CanViewRawMaterialStock, PermissionKeys.CanViewRawMaterialStock, PermissionKeys.CanViewPackingMaterialStock], route: "/logistics/available-stock", order: 5)
            ]
        ),

        // Order 4: Warehouse
        new(
            name: "Warehouse",
            module: PermissionModules.Warehouse,
            requiredPermissionKey: [],
            route: "/warehouse/receiving-area",
            icon: "warehouse",
            order: 4,
            children:
            [
                new(PermissionSubmodules.ReceivingArea, [PermissionKeys.CanViewRawMaterialsItems, PermissionKeys.CanCreateChecklistForRawMaterials, PermissionKeys.CanCreateGrnForPackagingMaterials, PermissionKeys.CanViewPackagingMaterialsItems, PermissionKeys.CanCreateChecklistForPackagingMaterials, PermissionKeys.CanCreateGrnForPackagingMaterials], route: "/warehouse/receiving-area", order: 1),
                new(PermissionSubmodules.QuarantineAreaGrn, [PermissionKeys.CanViewRawMaterials, PermissionKeys.CanAssignRawMaterialsStockToShelves, PermissionKeys.CanViewPackageMaterialRequisitionsForCreation, PermissionKeys.CanAssignPackagingMaterialsStockToShelves], name: "Quarantine / GRN", route: "/warehouse/quarantine-grn", order: 2),
                new(PermissionSubmodules.LinkedMaterials, [PermissionKeys.CanViewLinkedRawMaterials, PermissionKeys.CanUnlinkRawMaterials, PermissionKeys.CanViewLinkedPackagingMaterials, PermissionKeys.CanUnlinkPackagingMaterials], route: "/warehouse/linked-materials", order: 3),
                new(PermissionSubmodules.UnlinkedMaterials, [PermissionKeys.CanViewUnlinkedRawMaterials, PermissionKeys.CanLinkRawMaterials, PermissionKeys.CanViewUnlinkedPackagingMaterials, PermissionKeys.CanLinkPackagingMaterials], route: "/warehouse/unlinked-materials", order: 4),
                new(PermissionSubmodules.Materials, [PermissionKeys.CanViewRawMaterials, PermissionKeys.CanCreateNewRawMaterials, PermissionKeys.CanEditRawMaterials, PermissionKeys.CanDeleteRawMaterials, PermissionKeys.CanViewPackagingMaterials, PermissionKeys.CanCreateNewPackagingMaterials, PermissionKeys.CanEditPackagingMaterials, PermissionKeys.CanDeletePackagingMaterials], route: "/warehouse/materials", order: 5),
                new(PermissionSubmodules.ApprovedMaterials, [PermissionKeys.CanViewApprovedRawMaterials, PermissionKeys.CanViewApprovedPackagingMaterials], route: "/warehouse/approved-materials", order: 6),
                new(PermissionSubmodules.RejectedMaterials, [PermissionKeys.CanViewRejectedRawMaterials, PermissionKeys.CanViewRejectedPackagingMaterials], route: "/warehouse/rejected-materials", order: 7),
                new(PermissionSubmodules.IssueStockRequisitions, [PermissionKeys.CanViewRawMaterialRequisitions, PermissionKeys.CanIssueRawMaterialRequisitions, PermissionKeys.CanViewPackagingMaterialRequisitions, PermissionKeys.CanIssuePackagingMaterialRequisitions], route: "/warehouse/issue-stock-requisitions", order: 8),
                new(PermissionSubmodules.StockTransferIssues, [PermissionKeys.CanViewRawMaterialTransferList, PermissionKeys.CanIssueRawMaterialStockTransfers, PermissionKeys.CanViewPackagingMaterialTransferList, PermissionKeys.CanIssuePackagingMaterialStockTransfers], route: "/warehouse/stock-transfer-issues", order: 9),
                new(PermissionSubmodules.Locations, [PermissionKeys.CanViewRawMaterialLocationChartList, PermissionKeys.CanReassignRawMaterialStock, PermissionKeys.CanViewPackagingMaterialLocationChartList, PermissionKeys.CanReassignPackagingMaterialStock], route: "/warehouse/location-chart-record", order: 10)
            ]
        ),

        // Order 5: Production
        new(
            name: "Production",
            module: PermissionModules.Production,
            requiredPermissionKey: [],
            route: "/production/requisitions",
            icon: "production",
            order: 5,
            children:
            [
                new(PermissionSubmodules.Requisitions, [PermissionKeys.CanViewRawMaterialRequisitions], route: "/production/requisitions", order: 1),
                new(PermissionSubmodules.CreatePurchaseRequisitions, [PermissionKeys.CanViewRawMaterialRequisitionsForCreation, PermissionKeys.CanCreateRawMaterialRequisitions, PermissionKeys.CanViewPackageMaterialRequisitionsForCreation, PermissionKeys.CanCreatePackageMaterialRequisitions], route: "/production/create-purchase-requisitions", order: 2),
                //new(PermissionSubmodules.Planning, [PermissionKeys.CanViewPlannedProducts, PermissionKeys.CanCreateNewProductionPlan, PermissionKeys.CanEditProductionPlan], route: "/production/planning", order: 3),
                new(PermissionSubmodules.StockTransferRequests, [PermissionKeys.CanViewIncomingStockTransferRequests, PermissionKeys.CanApproveIncomingStockTransferRequest, PermissionKeys.CanViewOutgoingStockTransferRequests], route: "/production/stock-transfer-requests", order: 4),
                new(PermissionSubmodules.ProductSchedule, [PermissionKeys.CanViewProductSchedules, PermissionKeys.CanCreateProductSchedule], route: "/production/product-schedule", order: 5)
            ]
        ),

        // Order 6: Quality Control
        new(
            name: "Quality Control",
            module: PermissionModules.QualityControl,
            requiredPermissionKey: [],
            route: "/quality-control/goods-receipt-note",
            icon: "quality-control",
            order: 6,
            children:
            [
                new(PermissionSubmodules.GoodsReceiptNote, [PermissionKeys.CanViewRawMaterialGoodsReceiptNotes, PermissionKeys.CanTakeRawMaterialSample, PermissionKeys.CanStartRawMaterialTest, PermissionKeys.CanCheckRawMaterialTestResult, PermissionKeys.CanViewPackagingMaterialGoodsReceiptNotes, PermissionKeys.CanTakePackagingMaterialSample, PermissionKeys.CanStartPackagingMaterialTest, PermissionKeys.CanCheckPackagingMaterialTestResult], route: "/quality-control/goods-receipt-note", order: 1),
                new(PermissionSubmodules.AnalyticalTestRequestProducts, [PermissionKeys.CanViewProductAnalyticalTestRequests, PermissionKeys.CanAcknowledgeSampleTaken, PermissionKeys.CanStartProductTest, PermissionKeys.CanCheckProductTest], route: "/quality-control/analytical-test-request-products", order: 2),
                new(PermissionSubmodules.MaterialStp, [PermissionKeys.CanViewRawMaterialStps, PermissionKeys.CanCreateRawMaterialStp, PermissionKeys.CanViewPackagingMaterialStps, PermissionKeys.CanCreatePackagingMaterialStp], route: "/quality-control/material-stp", order: 3),
                new(PermissionSubmodules.MaterialSpecification, [PermissionKeys.CanViewRawMaterialSpecifications, PermissionKeys.CanCreateRawMaterialSpecification, PermissionKeys.CanViewPackagingMaterialSpecifications, PermissionKeys.CanCreatePackagingMaterialSpecification], route: "/quality-control/material-specification", order: 4),
                new(PermissionSubmodules.MaterialArd, [PermissionKeys.CanViewRawMaterialArds, PermissionKeys.CanCreateRawMaterialArd, PermissionKeys.CanViewPackagingMaterialArds, PermissionKeys.CanCreatePackagingMaterialArd], route: "/quality-control/material-ard", order: 5),
                new(PermissionSubmodules.ProductStp, [PermissionKeys.CanViewProductStps, PermissionKeys.CanCreateProductStp], route: "/quality-control/product-stp", order: 6),
                new(PermissionSubmodules.ProductArd, [PermissionKeys.CanViewProductArds, PermissionKeys.CanCreateProductArd], route: "/quality-control/product-ard", order: 7),
                new(PermissionSubmodules.ProductSpecification, [PermissionKeys.CanViewProductSpecifications, PermissionKeys.CanCreateProductSpecification], route: "/quality-control/product-specification", order: 8)
            ]
        ),

        // Order 7: Quality Assurance
        new(
            name: "Quality Assurance",
            module: PermissionModules.QualityAssurance,
            requiredPermissionKey: [],
            route: "/quality-assurance/issue-bmr",
            icon: "quality-assurance",
            order: 7,
            children:
            [
                new(PermissionSubmodules.IssueBmr, [PermissionKeys.CanViewIssuedBmrBprs, PermissionKeys.CanIssueBmr], route: "/quality-assurance/issue-bmr", order: 1),
                new(PermissionSubmodules.AnalyticalTestRequests, [PermissionKeys.CanViewAnalyticalTestRequests, PermissionKeys.CanTakeSamples], route: "/quality-assurance/analytical-test-requests", order: 2),
                new(PermissionSubmodules.PendingApprovals, [PermissionKeys.CanViewPendingApprovals, PermissionKeys.CanApprovePendingApproval, PermissionKeys.CanRejectPendingApproval], route: "/quality-assurance/pending-approvals", order: 3)
            ]
        ),

        // Order 8: Finished Goods
        new(
            name: "Finished Goods",
            module: PermissionModules.FinishedGoodsWarehouse,
            requiredPermissionKey: [],
            route: "/finished-goods/customer-management",
            icon: "finished-goods",
            order: 8,
            children:
            [
                new(PermissionSubmodules.CustomerManagement, [PermissionKeys.CanViewCustomers, PermissionKeys.CanCreateCustomer, PermissionKeys.CanEditCustomer, PermissionKeys.CanDeleteCustomer], route: "/finished-goods/customer-management", order: 1),
                new(PermissionSubmodules.ProductionOrders, [PermissionKeys.CanViewOrder, PermissionKeys.CanCreateOrders, PermissionKeys.CanGeneratePackingList], route: "/finished-goods/production-orders", order: 2),
                new(PermissionSubmodules.PackingList, [PermissionKeys.CanViewPackingList], route: "/finished-goods/packing-list", order: 3),
                new(PermissionSubmodules.ProformaInvoice, [PermissionKeys.CanGenerateProformaInvoice, PermissionKeys.CanViewProformaInvoice], route: "/finished-goods/proforma-invoice", order: 4),
                new(PermissionSubmodules.Invoice, [PermissionKeys.CanViewInvoice], route: "/finished-goods/invoice", order: 5),
                new(PermissionSubmodules.Waybill, [PermissionKeys.CanViewWaybillForFgw, PermissionKeys.CanCreateWaybillForFgw, PermissionKeys.CanEditWaybillForFgw, PermissionKeys.CanDeleteWaybillForFgw], name: "Waybill", route: "/finished-goods/waybill", order: 6)
            ]
        ),

        // Order 9: Inventory Management
        new(
            name: "Inventory",
            module: PermissionModules.InventoryManagement,
            requiredPermissionKey: [],
            route: "/inventory/manufacturers",
            icon: "inventory",
            order: 9,
            children:
            [
                new(PermissionSubmodules.Manufacturers, [PermissionKeys.CanViewManufacturers, PermissionKeys.CanCreateManufacturer, PermissionKeys.CanUpdateManufacturerDetails, PermissionKeys.CanDeleteManufacturer], route: "/inventory/manufacturers", order: 1),
                new(PermissionSubmodules.Suppliers, [PermissionKeys.CanViewVendors, PermissionKeys.CanCreateVendor, PermissionKeys.CanUpdateVendorDetails, PermissionKeys.CanDeleteVendor], route: "/inventory/suppliers", order: 2),
                new(PermissionSubmodules.Warehouses, [PermissionKeys.CanViewWarehouses], route: "/inventory/warehouses", order: 3),
                new(PermissionSubmodules.Locations, [PermissionKeys.CanViewLocations, PermissionKeys.CanAddNewLocation, PermissionKeys.CanEditLocation, PermissionKeys.CanDeleteLocation], route: "/inventory/locations", order: 4),
                new(PermissionSubmodules.Racks, [PermissionKeys.CanViewRacks, PermissionKeys.CanAddNewRack, PermissionKeys.CanEditRack, PermissionKeys.CanDeleteRack], route: "/inventory/racks", order: 5),
                new(PermissionSubmodules.Shelves, [PermissionKeys.CanViewShelves, PermissionKeys.CanAddNewShelf, PermissionKeys.CanEditShelf, PermissionKeys.CanDeleteShelf], route: "/inventory/shelves", order: 6),
                new(PermissionSubmodules.Equipment, [PermissionKeys.CanViewEquipment, PermissionKeys.CanCreateNewItemInEquipmentStore, PermissionKeys.CanEditEquipmentDetails, PermissionKeys.CanDeleteEquipment], route: "/inventory/equipment", order: 7),
                new(PermissionSubmodules.UnitOfMeasure, [PermissionKeys.CanViewUnitOfMeasure, PermissionKeys.CanCreateUnitOfMeasure, PermissionKeys.CanEditUnitOfMeasure, PermissionKeys.CanDeleteUnitOfMeasure], route: "/inventory/unit-of-measure", order: 8)
            ]
        ),

        // Order 10: Human Resources
        new(
            name: "Human Resources",
            module: PermissionModules.HumanResources,
            requiredPermissionKey: [],
            route: "/hr/employee-management",
            icon: "human-resources",
            order: 10,
            children:
            [
                new(PermissionSubmodules.EmployeeManagement, [PermissionKeys.CanViewEmployeeDetails, PermissionKeys.CanRegisterEmployee, PermissionKeys.CanUpdateEmployeeInfo, PermissionKeys.CanViewEmployeeDetails], route: "/hr/employee-management", order: 1),
                new(PermissionSubmodules.DepartmentEmployeeExport, [PermissionKeys.CanViewDepartmentEmployee, PermissionKeys.CanExportDepartmentEmployee], name: "Department Employees", route: "/hr/department-employees", order: 2),
                new(PermissionSubmodules.DesignationManagement, [PermissionKeys.CanViewDesignation, PermissionKeys.CanCreateDesignation, PermissionKeys.CanEditDesignation, PermissionKeys.CanDeleteDesignation], route: "/hr/designation-management", order: 3),
                new(PermissionSubmodules.LeaveManagement, [PermissionKeys.CanViewLeaveRequests, PermissionKeys.CanCreateLeaveRequest, PermissionKeys.CanEditLeaveRequest, PermissionKeys.CanRecallLeave, PermissionKeys.CanRecallLeave], route: "/hr/leave-management", order: 4),
                new(PermissionSubmodules.LeaveTypeConfiguration, [PermissionKeys.CanViewLeaveType, PermissionKeys.CanEditLeaveType, PermissionKeys.CanDeleteLeaveType], route: "/hr/leave-type-config", order: 5),
                new(PermissionSubmodules.StaffRequisition, [PermissionKeys.CanViewStaffRequisition, PermissionKeys.CanCreateStaffRequisition, PermissionKeys.CanEditStaffRequisition, PermissionKeys.CanDeleteStaffRequisition], route: "/hr/staff-requisition", order: 6),
                new(PermissionSubmodules.AttendanceReportUpload, [PermissionKeys.CanViewAttendanceReportUpload, PermissionKeys.CanSubmitAttendanceReportUpload, PermissionKeys.CanCancelAttendanceReportUpload], route: "/hr/attendance-upload", order: 7),
                new(PermissionSubmodules.ShiftScheduleReportUpload, [PermissionKeys.CanViewShiftScheduleReportUpload, PermissionKeys.CanSubmitShiftScheduleReportUpload], route: "/hr/shift-schedule-upload", order: 8),
                new(PermissionSubmodules.OvertimeManagement, [PermissionKeys.CanViewOvertimeManagement, PermissionKeys.CanCreateOvertimeManagement, PermissionKeys.CanEditOvertimeManagement, PermissionKeys.CanDeleteOvertimeManagement], route: "/hr/overtime-management", order: 9)
            ]
        ),
        
        // Order 11: Organizational Structure
        new(
            name: "Organization",
            module: PermissionModules.OrganizationalStructure,
            requiredPermissionKey: [],
            route: "/organization/departments",
            icon: "organization",
            order: 11,
            children:
            [
                new(PermissionSubmodules.Departments, [PermissionKeys.CanViewDepartments, PermissionKeys.CanCreateNewDepartment, PermissionKeys.CanEditDepartment, PermissionKeys.CanDeleteDepartment], route: "/organization/departments", order: 1),
                new(PermissionSubmodules.WorkingDays, [PermissionKeys.CanViewWorkingDays, PermissionKeys.CanCreateWorkingDays, PermissionKeys.CanResetWorkingDays], route: "/organization/working-days", order: 2),
                new(PermissionSubmodules.Holidays, [PermissionKeys.CanViewHolidays, PermissionKeys.CanCreateHoliday, PermissionKeys.CanEditHoliday, PermissionKeys.CanDeleteHoliday], route: "/organization/holidays", order: 3),
                new(PermissionSubmodules.ShiftsType, [PermissionKeys.CanViewShiftTypes, PermissionKeys.CanCreateShiftType, PermissionKeys.CanEditShiftType, PermissionKeys.CanDeleteShiftType], name: "Shift Types", route: "/organization/shift-types", order: 4),
                new(PermissionSubmodules.ShiftsSchedule, [PermissionKeys.CanViewShiftSchedule, PermissionKeys.CanCreateShiftSchedule, PermissionKeys.CanEditShiftSchedule, PermissionKeys.CanDeleteShiftSchedule], name: "Shift Schedules", route: "/organization/shift-schedules", order: 5)
            ]
        ),

        // Order 12: IT Support
        new(
            name: "IT Support",
            module: PermissionModules.ItSupport,
            requiredPermissionKey: [],
            route: "/it/user-management",
            icon: "it-support",
            order: 12,
            children:
            [
                new(PermissionSubmodules.UserManagement, [PermissionKeys.CanViewActiveUser, PermissionKeys.CanCreateUser, PermissionKeys.CanEditUser, PermissionKeys.CanBlockUser], route: "/it/user-management", order: 1),
                new(PermissionSubmodules.AuditTrail, [PermissionKeys.CanViewAuditTrail], route: "/it/audit-trail", order: 2),
                new(PermissionSubmodules.ManageRoles, [PermissionKeys.CanViewRoles, PermissionKeys.CanCreateRole, PermissionKeys.CanEditRole, PermissionKeys.CanDeleteRole], route: "/it/manage-roles", order: 3),
                new(PermissionSubmodules.ManagePermissions, [PermissionKeys.CanViewPermissions, PermissionKeys.CanUpdateExistingPermission, PermissionKeys.CanResetPermission], route: "/it/manage-permissions", order: 4)
            ]
        ),

        // Order 13: Settings
        new(
            name: "Settings",
            module: PermissionModules.Settings,
            requiredPermissionKey: [],
            route: "/settings/system-settings",
            icon: "settings",
            order: 13,
            children:
            [
                new(PermissionSubmodules.GeneralSettings, [PermissionKeys.CanViewCodeSettings, PermissionKeys.CanViewSignatureSettings], route: "/settings/system-settings", order: 1),
                new(PermissionSubmodules.ProductsCategory, [PermissionKeys.CanViewProductCategories, PermissionKeys.CanCreateProductCategory, PermissionKeys.CanEditProductCategory, PermissionKeys.CanDeleteProductCategory], route: "/settings/product-categories", order: 2),
                new(PermissionSubmodules.Products, [PermissionKeys.CanViewRawCategories, PermissionKeys.CanCreateRawCategory, PermissionKeys.CanEditRawCategory, PermissionKeys.CanDeleteRawCategory, PermissionKeys.CanViewPackageCategories, PermissionKeys.CanCreatePackageCategory, PermissionKeys.CanEditPackageCategory, PermissionKeys.CanDeletePackageCategory, PermissionKeys.CanViewMaterialTypes, PermissionKeys.CanCreateMaterialType, PermissionKeys.CanEditMaterialType, PermissionKeys.CanDeleteMaterialType, PermissionKeys.CanViewPackageStyle, PermissionKeys.CanCreatePackageStyle, PermissionKeys.CanEditPackageStyle, PermissionKeys.CanDeletePackageStyle, PermissionKeys.CanViewProductState, PermissionKeys.CanCreateProductState, PermissionKeys.CanEditProductState, PermissionKeys.CanDeleteProductState], route: "/settings/products", order: 3),
                new(PermissionSubmodules.Procedures, [PermissionKeys.CanViewResources, PermissionKeys.CanCreateResource, PermissionKeys.CanEditResource, PermissionKeys.CanDeleteResource, PermissionKeys.CanViewOperations, PermissionKeys.CanCreateOperation, PermissionKeys.CanEditOperation, PermissionKeys.CanDeleteOperation, PermissionKeys.CanViewWorkCenters, PermissionKeys.CanCreateWorkCenter, PermissionKeys.CanEditWorkCenter, PermissionKeys.CanDeleteWorkCenter], route: "/settings/procedures", order: 4),
                new(PermissionSubmodules.CountryAddress, [PermissionKeys.CanViewCountries, PermissionKeys.CanCreateCountries, PermissionKeys.CanEditCountries, PermissionKeys.CanDeleteCountries], route: "/settings/country-address", order: 5),
                new(PermissionSubmodules.Schedules, [PermissionKeys.CanViewShiftSchedules, PermissionKeys.CanCreateShiftSchedules, PermissionKeys.CanEditShiftSchedules, PermissionKeys.CanDeleteShiftSchedules], route: "/settings/schedules", order: 6),
                new(PermissionSubmodules.TermsOfPayment, [PermissionKeys.CanViewPaymentTerms, PermissionKeys.CanCreatePaymentTerm, PermissionKeys.CanEditPaymentTerm, PermissionKeys.CanDeletePaymentTerm], route: "/settings/terms-of-payment", order: 7),
                new(PermissionSubmodules.DeliveryMode, [PermissionKeys.CanViewDeliveryModes, PermissionKeys.CanCreateDeliveryMode, PermissionKeys.CanEditDeliveryMode, PermissionKeys.CanDeleteDeliveryMode], route: "/settings/delivery-mode", order: 8),
                new(PermissionSubmodules.Charges, [PermissionKeys.CanViewCharges, PermissionKeys.CanCreateCharges, PermissionKeys.CanEditCharges, PermissionKeys.CanDeleteCharges], route: "/settings/charges", order: 9),
                new(PermissionSubmodules.CodeSettings, [PermissionKeys.CanViewCodeSettings, PermissionKeys.CanCreateNewCodes, PermissionKeys.CanEditCodeSettings, PermissionKeys.CanDeleteCodeSettings], route: "/settings/code-settings", order: 10),
                new(PermissionSubmodules.WorkflowBuilder, [PermissionKeys.CanViewQuestions, PermissionKeys.CanCreateQuestions, PermissionKeys.CanEditQuestions, PermissionKeys.CanDeleteQuestions, PermissionKeys.CanViewTemplate, PermissionKeys.CanCreateTemplate, PermissionKeys.CanEditTemplate, PermissionKeys.CanDeleteTemplate], route: "/settings/workflow-builder", order: 11),
                new(PermissionSubmodules.AlertsNotifications, [PermissionKeys.CanViewAlerts, PermissionKeys.CanCreateNewAlerts, PermissionKeys.CanEditAlerts, PermissionKeys.CanEnableDisableAlerts, PermissionKeys.CanDeleteAlerts], route: "/settings/alerts-notifications", order: 12),
                new(PermissionSubmodules.Approvals, [PermissionKeys.CanViewApprovals, PermissionKeys.CanCreateNewApproval, PermissionKeys.CanEditApprovalWorkflow, PermissionKeys.CanDeleteApprovals], route: "/settings/approvals", order: 13),
                new(PermissionSubmodules.SignatureSettings, [PermissionKeys.CanViewSignatureSettings, PermissionKeys.CanCreateSignatureSettings], route: "/settings/signature-settings", order: 14),
                new(PermissionSubmodules.ChangePassword, [PermissionKeys.CanChangePassword], route: "/settings/change-password", order: 15)
            ]
        )
    ];
}