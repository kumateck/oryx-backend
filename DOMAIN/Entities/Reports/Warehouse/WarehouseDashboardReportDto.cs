namespace DOMAIN.Entities.Reports.Warehouse;

public class WarehouseDashboardReportDto
{
    public StockTransferDashboardDto StockTransfers { get; set; } 
    public StockRequisitionStatusCountDto StockRequisitions { get; set; }
}


public class StockTransferStatusCountDto
{
    public int InProgressCount { get; set; }
    public int ApprovedCount { get; set; }
    public int IssuedCount { get; set; }
    public int RejectedCount { get; set; }
}
public class StockTransferDashboardDto
{
    public StockTransferStatusCountDto Incoming { get; set; } = new();
    public StockTransferStatusCountDto Outgoing { get; set; } = new();
}

public class ExpiredMaterialReportDto
{
    public string MaterialName { get; set; }
    public string MaterialCode { get; set; }

    public string BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string ShelfName { get; set; }
    public decimal QuantityOnShelf { get; set; }
    public string UomSymbol { get; set; }

    public DateTime DateReceived { get; set; }
}

public class ReservedMaterialReportDto
{
    public string MaterialName { get; set; }
    public string MaterialCode { get; set; }


    public decimal ReservedQuantity { get; set; }
    public string UomSymbol { get; set; }

    public string ProductName { get; set; }
    public string ProductCode { get; set; }

    public string WarehouseName { get; set; }
    public string DepartmentName { get; set; }

    public DateTime DateTime { get; set; }
    public string Schedule { get; set; }
    public string ProductBatchNumber { get; set; }
    public string ArNumber { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal BalanceQuantity { get; set; }
    public string MaterialBatchNumber { get; set; }
}

public class MaterialsChecklistReportDto
{
    public List<MaterialChecklistItemDto> IncomingMaterials { get; set; } = new();
    public List<MaterialChecklistItemDto> CheckedMaterials { get; set; } = new();
}

public class MaterialChecklistItemDto
{
    public string MaterialName { get; set; }
    public string MaterialCode { get; set; }
    public decimal Quantity { get; set; }
    public string UomSymbol { get; set; }
}
public class StockRequisitionStatusCountDto
{
    public int NewCount { get; set; }
    public int PendingCount { get; set; }
    public int SourcedCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
}

public enum DateFilter
{
    Today,
    ThisWeek,
    ThisMonth,
    AllTime
}