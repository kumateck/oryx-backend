using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Reports;

/// <summary>
/// Shared filter for the production dashboard KPI widgets (KPI 6 - 12).
/// Department is optional ("All") and the date range is applied against the
/// most relevant timestamp of each KPI (see the individual repository methods).
/// </summary>
public class ProductionKpiFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
}

/// <summary>
/// KPI 6 - Schedule Adherence (On-Time Completion Rate).
/// </summary>
public class ScheduleAdherenceDto
{
    public Guid? DepartmentId { get; set; }
    public string Department { get; set; }
    public int OnTime { get; set; }
    public int Delayed { get; set; }
    public int TotalCompleted { get; set; }
    public decimal AdherencePercentage { get; set; }
}

/// <summary>
/// KPI 7 - Production Output Volume.
/// </summary>
public class ProductionOutputVolumeDto
{
    public Guid? DepartmentId { get; set; }
    public string Department { get; set; }
    public Division Division { get; set; }
    public int ProductCount { get; set; }
    public int TotalBatchCount { get; set; }
    public decimal TotalQuantityPacked { get; set; }
    public string Uom { get; set; }
}

/// <summary>
/// KPI 8 - ATR Testing Backlog.
/// </summary>
public class AtrTestingBacklogDto
{
    public Guid? DepartmentId { get; set; }
    public string Department { get; set; }
    public int NewNotSampled { get; set; }
    public int Testing { get; set; }
    public int TestTaken { get; set; }
    public int TotalBacklog { get; set; }
    public int OldestWaitingDays { get; set; }
}

/// <summary>
/// KPI 9 - Stock Requisition Pending for Production.
/// </summary>
public class StockRequisitionPendingDto
{
    public Guid? DepartmentId { get; set; }
    public string Department { get; set; }
    public int InProgress { get; set; }
    public int Reject { get; set; }
    public int Approved { get; set; }
    public int Total { get; set; }
    public int OldestPendingDays { get; set; }
}

/// <summary>
/// KPI 10 - FGTN Pending Approval.
/// </summary>
public class FgtnPendingApprovalDto
{
    public Guid? DepartmentId { get; set; }
    public string FromDepartment { get; set; }
    public int PendingApproval { get; set; }
    public int Approved { get; set; }
    public decimal AverageAgingDays { get; set; }
}

/// <summary>
/// KPI 11 - Production Order Delivery Status.
/// </summary>
public class ProductionOrderDeliveryStatusDto
{
    public ProductionOrderStatus Status { get; set; }
    public string StatusName { get; set; }
    public int OrderCount { get; set; }
    public int AllocatedCount { get; set; }
    public int Loaded { get; set; }
    public int Delivered { get; set; }
}

/// <summary>
/// KPI 12 - Material Return Rate.
/// </summary>
public class MaterialReturnRateDto
{
    public Guid? DepartmentId { get; set; }
    public string Department { get; set; }
    public decimal TotalIssuedQuantity { get; set; }
    public decimal TotalReturnedQuantity { get; set; }
    public decimal ReturnRatePercentage { get; set; }
    public int ReturnNoteCount { get; set; }
}
