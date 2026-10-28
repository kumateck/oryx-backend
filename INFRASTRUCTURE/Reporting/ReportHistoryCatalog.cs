namespace INFRASTRUCTURE.Reporting;

/// <summary>Only reporting dependencies are versioned; authentication grants/tokens are never replayed.</summary>
public static class ReportHistoryCatalog
{
    public static readonly string[] AuthenticationTables = ["roles", "userroles", "roleclaims", "userclaims", "usertokens", "userlogins", "PermissionTypes", "PermissionModules"];
    public static readonly string[] Tables = [
        "Employees", "Departments", "Designations", "users", "UnitOfMeasures", "CollectionItems",
        "Materials", "MaterialTypes", "MaterialCategories", "MaterialSpecifications", "MaterialDepartments",
        "MaterialBatches", "MaterialBatchEvents", "MaterialBatchReservedQuantities", "Warehouses", "WarehouseLocations",
        "WarehouseLocationRacks", "WarehouseLocationShelves", "ShelfMaterialBatches",
        "Products", "ProductPrices", "ProductCategories", "ProductPackings", "ProductPackages", "ProductPackageSubstitutes", "ProductSpecifications", "Equipments",
        "FinishedGoodsTransferNotes", "FinishedGoodsTransferNoteQuantities", "FinishedProducts", "DistributedFinishedProducts",
        "Items", "ItemCategories", "Vendors", "VendorItems", "ItemTransactionLogs", "Suppliers", "Manufacturers",
        "SupplierManufacturers", "ManufacturerMaterials", "SupplierMaterials", "PurchaseOrders", "PurchaseOrderItems",
        "AttendanceRecords", "ShiftAssignments", "ShiftTypes", "ShiftSchedules", "ShiftCategories",
        "LeaveRequests", "LeaveTypes", "LeaveEntitlements", "Holidays", "CompanyWorkingDays",
    ];
}
