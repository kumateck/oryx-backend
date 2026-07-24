using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Reports.WarehouseDashboardKpi;

public class WarehouseCapacityUtilisationDto
{
    public string Department { get; set; }
    public string Warehouse { get; set; }
    public WarehouseType WarehouseType { get; set; }
    public int TotalShelves { get; set; }
    public int OccupiedShelves { get; set; }
    public int AvailableShelves { get; set; }
    public decimal UtilisationPercentage { get; set; }
}

public class DockToStockTimeDto
{
    public string Department { get; set; }
    public string Warehouse { get; set; }
    public string Period { get; set; }
    public double AverageHours { get; set; }
    public double MedianHours { get; set; }
    public int TotalRecords { get; set; }
}

public class StockTransferFulfilmentRateDto
{
    public string Department { get; set; }
    public string Direction { get; set; }
    public int TotalTransfers { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Issued { get; set; }
    public decimal FulfilmentPercentage { get; set; }
}

public class ReceivingPipelineSnapshotDto
{
    public string Department { get; set; }
    public int Pending { get; set; }
    public int Arrived { get; set; }
    public int Checked { get; set; }
    public int GrnGenerated { get; set; }
    public int Distributed { get; set; }
    public int Assigned { get; set; }
    public int Total { get; set; }
}

public class ExpiryRiskIndexDto
{
    public string Department { get; set; }
    public string Warehouse { get; set; }
    public string ExpiryWindow { get; set; }
    public int BatchCount { get; set; }
    public string TotalQuantity { get; set; }
    public string Uom { get; set; }
}

public class ReorderAlertCountDto
{
    public string Department { get; set; }
    public string Material { get; set; }
    public string MaterialCode { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal Shortfall { get; set; }
    public string Uom { get; set; }
}

public class SwapRequestActivityDto
{
    public string Department { get; set; }
    public string Period { get; set; }
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Total { get; set; }
}

public class MaterialMovementCountDto
{
    public string Department { get; set; }
    public string Period { get; set; }
    public int NewPutaways { get; set; }
    public int Adjustments { get; set; }
    public int TotalMovements { get; set; }
}

public enum ExpiryWindowFilter
{
    Within30Days,
    Within31To60Days,
    Within61To90Days,
    Over90Days
}

public class WarehouseKpiFilterDto
{
    // Warehouse Filters
    public Guid? WarehouseId { get; set; }

    // Date Filters
    public DateFilter? DatePreset { get; set; }
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }

    // Department Filters
    public Guid? DepartmentId { get; set; }
    public Guid? FromDepartmentId { get; set; }
    public Guid? ToDepartmentId { get; set; }

    // Inventory Filters
    public ExpiryWindowFilter? ExpiryWindow { get; set; }
    public MaterialKind? MaterialKind { get; set; }
}
