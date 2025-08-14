namespace APP.Utils;

public class MenuItem(
    string module,
    List<string> requiredPermissionKey,
    List<MenuItem> children = null,
    string icon = null,
    string route = null,
    string name =null,
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
            requiredPermissionKey: [..RequiredPermissionKey],
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
            new( // Order 1: Dashboard
                name: "Main", // Or "Dashboard" if preferred
                module: PermissionModules.Dashboard,
                requiredPermissionKey: [], // Typically open or requires a basic view permission
                route: "/dashboard",
                icon: "dashboard",
                order: 1
            ),

            new( // Order 2: Product Board
                name: "Main", // Or "Product Board" if preferred
                module: PermissionModules.ProductBoard,
                requiredPermissionKey: [], // Define specific permission if needed
                route: "/product-board",
                icon: "product-board",
                order: 2
            ),

            // Order 3: Procurement (Supply Chain) - Partially provided, completed below
            new(
                name: "Supply Chain",
                module: PermissionModules.Procurement,
                requiredPermissionKey: [], // Parent - visible if any child is visible
                route: "/manufacturer", // Route to first child
                icon: "procurement", // General icon for the module
                order: 3,
                children: [
                    new MenuItem( // 3.1
                        module: PermissionSubmodules.Manufacturers,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateManufacturer,
                            PermissionKeys.CanViewManufacturerDetails,
                            PermissionKeys.CanUpdateManufacturerDetails,
                            PermissionKeys.CanDeleteManufacturer
                            ],
                        route: "/manufacturer",
                        icon: "manufacturer",
                        order: 1
                    ),
                    new( // 3.2
                        module: PermissionSubmodules.Vendors,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateVendor,
                            PermissionKeys.CanViewVendorDetails,
                            PermissionKeys.CanUpdateVendorDetails,
                            PermissionKeys.CanDeleteVendor
                            ],
                        route: "/vendor",
                        icon: "vendor",
                        order: 2
                    ),
                    new( // 3.3
                        module: PermissionSubmodules.PurchaseRequisition,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewPurchaseRequisitions,
                            PermissionKeys.CanSourceItemsBasedOnRequisition
                            ],
                        route: "/purchase-requisition",
                        icon: "purchase-requisition",
                        order: 3
                    ),
                    new( // 3.4
                        module: PermissionSubmodules.QuotationsRequest,
                        requiredPermissionKey: [PermissionKeys.CanSendQuotationRequest],
                        route: "/quotation-request",
                        icon: "quotation-request",
                        order: 4
                    ),
                    new MenuItem( // 3.5
                        module: PermissionSubmodules.QuotationsResponses,
                        requiredPermissionKey: [PermissionKeys.CanInputResponses],
                        route: "/quotations-response",
                        icon: "quotation-response",
                        order: 5
                    ),
                    new MenuItem( // 3.6
                        module: PermissionSubmodules.PriceComparison,
                        requiredPermissionKey: [PermissionKeys.CanSelectVendorPricing],
                        route: "/price-comparison",
                        icon: "price-comparison",
                        order: 6
                    ),
                    new MenuItem( // 3.7
                        module: PermissionSubmodules.AwardedQuotations,
                        requiredPermissionKey: [PermissionKeys.CanSendAwardedQuotations],
                        route: "/awarded-quotations",
                        icon: "awarded-quotation",
                        order: 7
                    ),
                    new MenuItem( // 3.8
                        module: PermissionSubmodules.ProformaResponses,
                        requiredPermissionKey: [PermissionKeys.CanUploadProformaInvoice],
                        route: "/proforma-response",
                        icon: "proforma-response",
                        order: 8
                    ),
                    new MenuItem( // 3.9
                        module: PermissionSubmodules.PurchaseOrders,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreatePurchaseOrder,
                            PermissionKeys.CanReviseExistingPurchaseOrder
                            // Add CanViewPurchaseOrders if such a permission exists and is required
                            ],
                        route: "/purchase-orders", // Changed route to be specific
                        icon: "purchase-order", // Changed icon
                        order: 9
                    ),
                    new MenuItem( // 3.10
                        module: PermissionSubmodules.MaterialDistribution,
                        requiredPermissionKey: [PermissionKeys.CanDistributeMaterials],
                        route: "/material-distribution",
                        icon: "material-distribution",
                        order: 10
                    )
                ]
            ),

            // Order 4: Logistics
            new(
                name: "Logistics",
                module: PermissionModules.Logistics,
                requiredPermissionKey: [],
                route: "/shipment-invoice", // Route to first child
                icon: "logistics", // General icon
                order: 4,
                children: [
                    new MenuItem( // 4.1
                        module: PermissionSubmodules.ShipmentInvoice,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateShipmentInvoice,
                            PermissionKeys.CanViewShipmentInvoice,
                            PermissionKeys.CanEditShipmentInvoice,
                            PermissionKeys.CanDeleteShipmentInvoice
                            ],
                        route: "/shipment-invoice",
                        icon: "shipment-invoice",
                        order: 1
                    ),
                    new MenuItem( // 4.2
                        module: PermissionSubmodules.ShipmentDocument,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateShipmentDocument,
                            PermissionKeys.CanViewShipmentDocument,
                            PermissionKeys.CanEditShipmentDocument,
                            PermissionKeys.CanDeleteShipmentDocument,
                            PermissionKeys.CanChangeShipmentDocumentStatus
                            ],
                        route: "/shipment-document",
                        icon: "shipment-document",
                        order: 2
                    ),
                    new MenuItem( // 4.3
                        module: PermissionSubmodules.BillingSheet,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateBillingSheet,
                            PermissionKeys.CanViewBillingSheet,
                            PermissionKeys.CanEditBillingSheet,
                            PermissionKeys.CanDeleteBillingSheet
                            ],
                        route: "/billing-sheet",
                        icon: "billing-sheet",
                        order: 3
                    ),
                    new MenuItem( // 4.4
                        module: PermissionSubmodules.Waybill,
                        requiredPermissionKey: [
                            PermissionKeys.CanCreateWaybill,
                            PermissionKeys.CanViewWaybill,
                            PermissionKeys.CanEditWaybill,
                            PermissionKeys.CanDeleteWaybill,
                            PermissionKeys.CanChangeWaybillStatus
                            ],
                        route: "/waybill",
                        icon: "waybill",
                        order: 4
                    )
                ]
            ),

            // Order 5: Human Resources
            new(
                name: "Human Resources",
                module: PermissionModules.HumanResources,
                requiredPermissionKey: [],
                route: "/employee-management", // Route to first child
                icon: "human-resources", // General icon
                order: 5,
                children: [
                    new MenuItem( // 5.1
                        module: PermissionSubmodules.EmployeeManagement,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewEmployee,
                            PermissionKeys.CanRegisterEmployee,
                            PermissionKeys.CanUpdateEmployeeDetails,
                            PermissionKeys.CanDeleteEmployee
                            ],
                        route: "/employee-management",
                        icon: "employee-management",
                        order: 1
                    ),
                    new MenuItem( // 5.2
                        module: PermissionSubmodules.UserManagement,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewUser,
                            PermissionKeys.CanCreateUser,
                            PermissionKeys.CanUpdateUserDetails,
                            PermissionKeys.CanDeleteUser
                            ],
                        route: "/user-management",
                        icon: "user-management",
                        order: 2
                    ),
                    new MenuItem( // 5.3
                        module: PermissionSubmodules.DesignationManagement,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewDesignation,
                            PermissionKeys.CanCreateDesignation,
                            PermissionKeys.CanEditDesignation,
                            PermissionKeys.CanDeleteDesignation
                            ],
                        route: "/designation-management",
                        icon: "designation-management",
                        order: 3
                    ),
                    new MenuItem( // 5.4
                        module: PermissionSubmodules.RolesPermissionsManagement,
                        name: "Roles & Permissions", // Custom Name
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRoles,
                            PermissionKeys.CanCreateRoleAndAssignPermissions,
                            PermissionKeys.CanEditRoleWithItsPermissions,
                            PermissionKeys.CanDeleteRole
                            ],
                        route: "/roles-permissions",
                        icon: "roles-permissions",
                        order: 4
                    ),
                    new MenuItem( // 5.5
                        module: PermissionSubmodules.LeaveManagement,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewLeaveRequests,
                            PermissionKeys.CanCreateLeaveRequest,
                            PermissionKeys.CanEditLeaveRequest,
                            PermissionKeys.CanDeleteOrCancelLeaveRequest,
                            PermissionKeys.CanApproveOrRejectLeaveRequest
                            ],
                        route: "/leave-management",
                        icon: "leave-management",
                        order: 5
                    )
                ]
            ),

             // Order 6: Warehouse
            new(
                name: "Warehouse",
                module: PermissionModules.Warehouse,
                requiredPermissionKey: [],
                route: "/receiving-area", // Route to first child
                icon: "warehouse", // General icon
                order: 6,
                children: [
                    new MenuItem( // 6.1
                        module: PermissionSubmodules.ReceivingArea,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewReceivedRawMaterialsItems,
                            PermissionKeys.CanViewReceivedPackagingMaterialsItems,
                            PermissionKeys.CanCreateChecklistForIncomingRawMaterialsGoods,
                            PermissionKeys.CanCreateChecklistForIncomingPackagingMaterialsGoods,
                            PermissionKeys.CanCreateGrnForRawMaterialsChecklistedItems,
                            PermissionKeys.CanCreateGrnForPackagingMaterialsChecklistedItems
                            ],
                        route: "/receiving-area",
                        icon: "receiving-area",
                        order: 1
                    ),
                    new MenuItem( // 6.2
                        module: PermissionSubmodules.QuarantineAreaGrn,
                        name: "Quarantine / GRN", // Custom Name
                        requiredPermissionKey: [
                            PermissionKeys.CanViewQuarantineRawMaterialsRecords,
                            PermissionKeys.CanViewQuarantinePackagingMaterialsRecords,
                            PermissionKeys.CanAssignRawMaterialsStockToStorageLocations,
                            PermissionKeys.CanAssignPackagingMaterialsStockToStorageLocations
                            ],
                        route: "/quarantine-grn",
                        icon: "quarantine",
                        order: 2
                    ),
                    new MenuItem( // 6.3
                        module: PermissionSubmodules.Departments,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewDepartments,
                            PermissionKeys.CanCreateNewDepartment,
                            PermissionKeys.CanEditDepartment
                            ],
                        route: "/departments",
                        icon: "departments",
                        order: 3
                    ),
                     new MenuItem( // 6.4
                        module: PermissionSubmodules.Warehouses,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewWarehouses,
                            PermissionKeys.CanAddWarehouse,
                            PermissionKeys.CanEditWarehouse
                            ],
                        route: "/warehouses",
                        icon: "warehouses", // Consider singular icon name if preferred
                        order: 4
                    ),
                     new MenuItem( // 6.5
                        module: PermissionSubmodules.Locations,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewLocations,
                            PermissionKeys.CanAddNewLocation,
                            PermissionKeys.CanEditLocation
                            ],
                        route: "/locations",
                        icon: "locations", // Consider singular icon name
                        order: 5
                    ),
                     new MenuItem( // 6.6
                        module: PermissionSubmodules.Racks,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRacks,
                            PermissionKeys.CanAddNewRack,
                            PermissionKeys.CanEditRack
                            ],
                        route: "/racks",
                        icon: "racks", // Consider singular icon name
                        order: 6
                    ),
                     new MenuItem( // 6.7
                        module: PermissionSubmodules.Shelves,
                        requiredPermissionKey: [ // Assuming distinct permissions or mapped from Rack permissions in list
                            PermissionKeys.CanViewShelves,
                            PermissionKeys.CanAddNewShelf,
                            PermissionKeys.CanEditShelf
                            ],
                        route: "/shelves",
                        icon: "shelves", // Consider singular icon name
                        order: 7
                    ),
                    new MenuItem( // 6.8
                        module: PermissionSubmodules.Materials,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRawMaterials,
                            PermissionKeys.CanViewPackagingMaterials,
                            PermissionKeys.CanCreateNewRawMaterials,
                            PermissionKeys.CanCreateNewPackagingMaterials,
                            PermissionKeys.CanEditRawMaterials,
                            PermissionKeys.CanEditPackagingMaterials,
                            PermissionKeys.CanDeleteRawMaterials,
                            PermissionKeys.CanDeletePackagingMaterials
                            ],
                        route: "/materials", // Or specific like /raw-materials
                        icon: "materials",
                        order: 8
                        // Consider splitting into Raw/Packaging if needed
                    ),
                     new MenuItem( // 6.9
                        module: PermissionSubmodules.ApprovedMaterials,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewApprovedRawMaterials,
                            PermissionKeys.CanViewApprovedPackagingMaterials
                            ],
                        route: "/approved-materials",
                        icon: "approved-materials",
                        order: 9
                    ),
                     new MenuItem( // 6.10
                        module: PermissionSubmodules.RejectedMaterials,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRejectedRawMaterials,
                            PermissionKeys.CanViewRejectedPackagingMaterials
                            ],
                        route: "/rejected-materials",
                        icon: "rejected-materials",
                        order: 10
                    ),
                     new MenuItem( // 6.11
                        module: PermissionSubmodules.StockRequisitions,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRawMaterialRequisitions,
                            PermissionKeys.CanViewPackagingMaterialRequisitions,
                            PermissionKeys.CanIssueRawMaterialRequisitions,
                            PermissionKeys.CanIssuePackagingMaterialRequisitions
                            ],
                        route: "/stock-requisitions",
                        icon: "stock-requisitions",
                        order: 11
                    ),
                    new MenuItem( // 6.12
                        module: PermissionSubmodules.StockTransferIssues,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRawMaterialTransferList,
                            PermissionKeys.CanViewPackagingMaterialTransferList,
                            PermissionKeys.CanIssueRawMaterialStockTransfers,
                            PermissionKeys.CanIssuePackagingMaterialStockTransfers
                            ],
                        route: "/stock-transfer-issues",
                        icon: "stock-transfer",
                        order: 12
                    ),
                    new MenuItem( // 6.13
                        module: PermissionSubmodules.LocationChartRecord,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewRawMaterialLocationChartList,
                            PermissionKeys.CanViewPackagingMaterialLocationChartList,
                            PermissionKeys.CanReassignRawMaterialStock,
                            PermissionKeys.CanReassignPackagingMaterialStock
                            ],
                        route: "/location-chart-record",
                        icon: "location-chart",
                        order: 13
                    )
                ]
            ),

            // Order 7: Production
            new(
                name: "Production",
                module: PermissionModules.Production,
                requiredPermissionKey: [],
                route: "/production-requisitions", // Route to first logical group/child
                icon: "production", // General icon
                order: 7,
                children: [
                    // Grouping Requisitions under Production might need a non-routable parent item
                    // Or list them directly. Listing directly for simplicity:
                    new MenuItem( // 7.1 (Was under REQUISITIONS header)
                         module: PermissionSubmodules.RawMaterialRequisitions, // Using specific submodule name
                         name: "Raw Material Requisitions", // Explicit name
                         requiredPermissionKey: [ PermissionKeys.CanViewRawMaterialRequisitions],
                         route: "/production-raw-requisitions", // Unique route
                         icon: "raw-material",
                         order: 1
                    ),
                     new MenuItem( // 7.2 (Was under REQUISITIONS header)
                         module: PermissionSubmodules.PackageMaterialRequisitions, // Using specific submodule name
                         name: "Package Material Requisitions", // Explicit name
                         requiredPermissionKey: [ PermissionKeys.CanViewPackagingMaterialRequisitions],
                         route: "/production-package-requisitions", // Unique route
                         icon: "package-material",
                         order: 2
                    ),
                    new MenuItem( // 7.3
                        module: PermissionSubmodules.Planning,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewPlannedProducts,
                            PermissionKeys.CanCreateNewProductionPlan,
                            PermissionKeys.CanEditProductionPlan
                            ],
                        route: "/production-planning",
                        icon: "planning",
                        order: 3
                    ),
                    new MenuItem( // 7.4
                        module: PermissionSubmodules.StockTransferRequests,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewStockTransferRequests,
                            PermissionKeys.CanApproveOrRejectStockTransferRequest
                            ],
                        route: "/production-stock-transfer-requests",
                        icon: "stock-transfer-request",
                        order: 4
                    ),
                     new MenuItem( // 7.5
                        module: PermissionSubmodules.ProductSchedule,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewProductSchedules,
                            PermissionKeys.CanCreateProductSchedule,
                            PermissionKeys.CanUpdateProductSchedule,
                            PermissionKeys.CanCreateRawMaterialPurchaseRequisition,
                            PermissionKeys.CanCreatePackagingMaterialPurchaseRequisition,
                            PermissionKeys.CanCreateRawMaterialStockRequisition,
                            PermissionKeys.CanCreatePackagingMaterialStockRequisition,
                            PermissionKeys.CanCreateRawMaterialStockTransfer,
                            PermissionKeys.CanCreatePackagingMaterialStockTransfer
                            ],
                        route: "/product-schedule",
                        icon: "product-schedule",
                        order: 5
                    )
                    // Add more Production submodules if any were missed
                ]
            ),

             // Order 8: Settings
            new(
                name: "Settings",
                module: PermissionModules.Settings,
                requiredPermissionKey: [],
                route: "/system-settings", // Route to first child or overview
                icon: "settings", // General icon
                order: 8,
                children: [
                    new MenuItem( // 8.1
                        module: PermissionSubmodules.SystemSettings,
                        requiredPermissionKey: [ PermissionKeys.CanViewSystemSettings ],
                        route: "/system-settings",
                        icon: "system-settings",
                        order: 1
                    ),
                     new MenuItem( // 8.2
                        module: PermissionSubmodules.UserSettings,
                        requiredPermissionKey: [ PermissionKeys.CanViewUserSettings ],
                        route: "/user-settings",
                        icon: "user-settings",
                        order: 2
                    ),
                     new MenuItem( // 8.3
                        module: PermissionSubmodules.AuditTrail,
                        requiredPermissionKey: [
                            PermissionKeys.CanViewAuditLogs,
                            PermissionKeys.CanExportAuditLogs
                            ],
                        route: "/audit-trail",
                        icon: "audit-trail",
                        order: 3
                    ),
                    // 8.4: Configurations (as a nested parent item)
                     new MenuItem(
                        name: "Configurations",
                        module: PermissionModules.Settings, // Using a dedicated module for the group
                        requiredPermissionKey: [], // Parent - visible if children are
                        route: "/product-categories", // Route to its first child
                        icon: "configurations",
                        order: 4,
                        children: [
                            // 8.4.1 Category Grouping (Optional Parent - or list children directly)
                            new MenuItem(
                                name:"Categories", // Group name
                                module: PermissionSubmodules.Categories, // Group module
                                requiredPermissionKey: [],
                                route: "/product-categories", // Route to first child
                                icon: "category",
                                order: 1,
                                children: [
                                     new MenuItem( // 8.4.1.1
                                        module: PermissionSubmodules.ProductCategory,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewProductCategories,
                                            PermissionKeys.CanCreateNewProductCategory,
                                            PermissionKeys.CanEditProductCategory,
                                            PermissionKeys.CanDeleteProductCategory
                                            ],
                                        route: "/product-categories",
                                        icon: "product-category",
                                        order: 1
                                    ),
                                     new MenuItem( // 8.4.1.2
                                        module: PermissionSubmodules.RawCategory,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewRawCategories,
                                            PermissionKeys.CanCreateNewRawCategory,
                                            PermissionKeys.CanEditRawCategory,
                                            PermissionKeys.CanDeleteRawCategory
                                            ],
                                        route: "/raw-categories",
                                        icon: "raw-category",
                                        order: 2
                                    ),
                                    new MenuItem( // 8.4.1.3
                                        module: PermissionSubmodules.PackageCategory,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewPackageCategories,
                                            PermissionKeys.CanCreateNewPackageCategory,
                                            PermissionKeys.CanEditPackageCategory,
                                            PermissionKeys.CanDeletePackageCategory
                                            ],
                                        route: "/package-categories",
                                        icon: "package-category",
                                        order: 3
                                    )
                                ]
                            ),
                            // 8.4.2 Procedures Grouping
                            new MenuItem(
                                name:"Procedures",
                                module: PermissionSubmodules.Procedures,
                                requiredPermissionKey: [],
                                route: "/resources",
                                icon: "procedures",
                                order: 2,
                                children: [
                                    new MenuItem( // 8.4.2.1
                                        module: PermissionSubmodules.Resource,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewResources,
                                            PermissionKeys.CanCreateNewResource,
                                            PermissionKeys.CanEditResource,
                                            PermissionKeys.CanDeleteResource
                                            ],
                                        route: "/resources",
                                        icon: "resource",
                                        order: 1
                                    ),
                                    new MenuItem( // 8.4.2.2
                                        module: PermissionSubmodules.Operation,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewOperations,
                                            PermissionKeys.CanCreateNewOperation,
                                            PermissionKeys.CanEditOperation,
                                            PermissionKeys.CanDeleteOperation
                                            ],
                                        route: "/operations",
                                        icon: "operation",
                                        order: 2
                                    ),
                                     new MenuItem( // 8.4.2.3
                                        module: PermissionSubmodules.WorkCenter,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewWorkCenters,
                                            PermissionKeys.CanCreateNewWorkCenter,
                                            PermissionKeys.CanEditWorkCenter,
                                            PermissionKeys.CanDeleteWorkCenter
                                            ],
                                        route: "/work-centers",
                                        icon: "work-center",
                                        order: 3
                                    )
                                ]
                            ),
                            // 8.4.3 Products Grouping
                             new MenuItem(
                                name:"Products",
                                module: PermissionSubmodules.Products,
                                requiredPermissionKey: [],
                                route: "/material-types",
                                icon: "products", // Reusing product icon
                                order: 3,
                                children: [
                                    new MenuItem( // 8.4.3.1
                                        module: PermissionSubmodules.MaterialType,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewMaterialTypes,
                                            PermissionKeys.CanCreateNewMaterialType,
                                            PermissionKeys.CanEditMaterialType,
                                            PermissionKeys.CanDeleteMaterialType
                                            ],
                                        route: "/material-types",
                                        icon: "material-type",
                                        order: 1
                                    )
                                    // Add other Product sub-items if any
                                ]
                            ),
                            // 8.4.4 Unit of Measure (Directly under Configurations)
                             new MenuItem( // 8.4.4
                                module: PermissionSubmodules.UnitOfMeasure,
                                requiredPermissionKey: [ PermissionKeys.CanViewUnitOfMeasure ],
                                route: "/unit-of-measure",
                                icon: "uom", // Abbreviation or full name
                                order: 4
                            ),
                            // 8.4.5 Address Grouping
                             new MenuItem(
                                name:"Address",
                                module: PermissionSubmodules.Address,
                                requiredPermissionKey: [],
                                route: "/countries",
                                icon: "address",
                                order: 5,
                                children: [
                                     new MenuItem( // 8.4.5.1
                                        module: PermissionSubmodules.Country,
                                        requiredPermissionKey: [ PermissionKeys.CanViewCountries ],
                                        route: "/countries",
                                        icon: "country",
                                        order: 1
                                    )
                                    // Add State, City etc. if needed
                                ]
                            ),
                             // 8.4.6 Container Grouping
                             new MenuItem(
                                name:"Container",
                                module: PermissionSubmodules.Container,
                                requiredPermissionKey: [],
                                route: "/pack-styles",
                                icon: "container",
                                order: 6,
                                children: [
                                     new MenuItem( // 8.4.6.1
                                        module: PermissionSubmodules.PackStyle,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewPackStyles,
                                            PermissionKeys.CanCreateNewPackStyle,
                                            PermissionKeys.CanEditPackStyle,
                                            PermissionKeys.CanDeletePackStyle
                                            ],
                                        route: "/pack-styles",
                                        icon: "pack-style",
                                        order: 1
                                    )
                                ]
                            ),
                             // 8.4.7 Billing Sheet Charge (Directly under Configurations)
                             new MenuItem( // 8.4.7
                                module: PermissionSubmodules.BillingSheetCharge,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewBillingCharges,
                                    PermissionKeys.CanCreateNewBillingSheetCharge,
                                    PermissionKeys.CanEditBillingSheetCharge,
                                    PermissionKeys.CanDeleteBillingSheetCharge
                                    ],
                                route: "/billing-charges",
                                icon: "billing-charge",
                                order: 7
                            ),
                            // 8.4.8 Terms of Payment (Directly under Configurations)
                            new MenuItem( // 8.4.8
                                module: PermissionSubmodules.TermsOfPayment,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewPaymentTerms,
                                    PermissionKeys.CanCreateNewPaymentTerm,
                                    PermissionKeys.CanEditPaymentTerm,
                                    PermissionKeys.CanDeletePaymentTerm
                                    ],
                                route: "/payment-terms",
                                icon: "payment-terms",
                                order: 8
                            ),
                            // 8.4.9 Delivery Mode (Directly under Configurations)
                             new MenuItem( // 8.4.9
                                module: PermissionSubmodules.DeliveryMode,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewDeliveryModes,
                                    PermissionKeys.CanCreateNewDeliveryMode,
                                    PermissionKeys.CanEditDeliveryMode,
                                    PermissionKeys.CanDeleteDeliveryMode
                                    ],
                                route: "/delivery-modes",
                                icon: "delivery-mode",
                                order: 9
                            ),
                             // 8.4.10 Code Settings (Directly under Configurations)
                            new MenuItem( // 8.4.10
                                module: PermissionSubmodules.CodeSettings,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewCodeSettings,
                                    PermissionKeys.CanAddNewCodes,
                                    PermissionKeys.CanEditCodeSettings,
                                    PermissionKeys.CanDeleteCodeSettings
                                    ],
                                route: "/code-settings",
                                icon: "code-settings",
                                order: 10
                            ),
                            // 8.4.11 Approvals (Directly under Configurations)
                             new MenuItem( // 8.4.11
                                module: PermissionSubmodules.Approvals,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewApproval,
                                    PermissionKeys.CanCreateOrConfigureNewApproval,
                                    PermissionKeys.CanEditApprovalWorkflow,
                                    PermissionKeys.CanDeleteOrDisableApprovals
                                    ],
                                route: "/approvals",
                                icon: "approvals",
                                order: 11
                            ),
                            // 8.4.12 Alerts & Notifications (Directly under Configurations)
                            new MenuItem( // 8.4.12
                                module: PermissionSubmodules.AlertsNotifications,
                                name: "Alerts & Notifications", // Custom Name
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewAlerts,
                                    PermissionKeys.CanCreateNewAlerts,
                                    PermissionKeys.CanEditAlerts,
                                    PermissionKeys.CanEnableOrDisableAlerts,
                                    PermissionKeys.CanDeleteAlerts
                                    ],
                                route: "/alerts-notifications",
                                icon: "notifications",
                                order: 12
                            ),
                            // 8.4.13 Equipment (Directly under Configurations)
                            new MenuItem( // 8.4.13
                                module: PermissionSubmodules.Equipment,
                                requiredPermissionKey: [
                                    PermissionKeys.CanViewEquipments,
                                    PermissionKeys.CanAddNewEquipment,
                                    PermissionKeys.CanEditEquipmentDetails,
                                    PermissionKeys.CanDeleteEquipment
                                    ],
                                route: "/equipment",
                                icon: "equipment",
                                order: 13
                            ),
                             // 8.4.14 Work Flow Forms Grouping
                             new MenuItem(
                                name:"Work Flow Forms",
                                module: PermissionSubmodules.WorkFlowForms,
                                requiredPermissionKey: [],
                                route: "/workflow-questions",
                                icon: "workflow",
                                order: 14,
                                children: [
                                     new MenuItem( // 8.4.14.1
                                        module: PermissionSubmodules.Questions,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewQuestions,
                                            PermissionKeys.CanCreateNewQuestions,
                                            PermissionKeys.CanEditQuestions,
                                            PermissionKeys.CanDeleteQuestions
                                        ],
                                        route: "/workflow-questions",
                                        icon: "question",
                                        order: 1
                                    ),
                                     new MenuItem( // 8.4.14.2
                                        module: PermissionSubmodules.Templates,
                                        requiredPermissionKey: [
                                            PermissionKeys.CanViewTemplates,
                                            PermissionKeys.CanCreateNewTemplates,
                                            PermissionKeys.CanEditTemplates,
                                            PermissionKeys.CanDeleteTemplates
                                        ],
                                        route: "/workflow-templates",
                                        icon: "template",
                                        order: 2
                                    )
                                ]
                            )
                        ]
                    )
                ]
            )
            // Add more top-level menu items if needed
        ];
    }