using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;

namespace DOMAIN.Entities.Reports;

/// <summary>Batches of the production schedules that start in one calendar month (UTC).</summary>
public sealed class ProductionMonthlyFilter
{
    [Required, Range(2000, 2200)] public int? Year { get; set; }
    [Required, Range(1, 12)] public int? Month { get; set; }
    /// <summary>Production users are limited to their own department, as on the other report pages.</summary>
    public Guid? DepartmentId { get; set; }
}

/// <summary>One planned batch with what was expected, packed, retained and transferred.</summary>
public sealed class ProductionMonthlyRowDto
{
    public Guid ScheduleProductId { get; set; }
    public string? ScheduleCode { get; set; }
    public DateTime ScheduleStart { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? MarketId { get; set; }
    public string? MarketName { get; set; }
    /// <summary>The issued BMR's batch number, or the planned one before the BMR is issued.</summary>
    public string? BatchNumber { get; set; }
    public BatchSize BatchSize { get; set; }
    /// <summary>1 for a full batch, 0.5 for a half batch.</summary>
    public decimal BatchShare { get; set; }
    public Guid? ProductPackingId { get; set; }
    public string? PackingStyle { get; set; }
    public int? PacksPerShipper { get; set; }
    /// <summary>The packing style's expected yield for this batch's size, in base units; null when not set.</summary>
    public decimal? ExpectedYield { get; set; }
    /// <summary>True once final packing is recorded; the packed figures below are null until then.</summary>
    public bool Packed { get; set; }
    public decimal? ActualYield { get; set; }
    public decimal? RetainedSamples { get; set; }
    public decimal? BottlesPerShipper { get; set; }
    public decimal? FullShippers { get; set; }
    public decimal? LeftOver { get; set; }
    /// <summary>Total of the batch's finished goods transfer notes; null when none, or when they use different units.</summary>
    public decimal? TransferredToWarehouse { get; set; }
    public string? TransferUnit { get; set; }
    public int TransferNotes { get; set; }
    /// <summary>The list price in force when the schedule started; null when the product had no price by then.</summary>
    public decimal? UnitPrice { get; set; }
    public string? PriceUnit { get; set; }
    public string? Currency { get; set; }
}

public sealed class ProductionMonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<ProductionMonthlyRowDto> Rows { get; set; } = [];
}

/// <summary>Stock issued to production batches. Every filter is optional.</summary>
public sealed class StockIssuedFilter
{
    public Guid? DepartmentId { get; set; }
    public Guid? ProductId { get; set; }
    /// <summary>The production batch number, matched exactly (case-insensitive).</summary>
    [StringLength(100)] public string? BatchNumber { get; set; }
    /// <summary>Optional inclusive UTC calendar dates of the issue; give both or neither.</summary>
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public enum StockIssueSource { Requisition = 0, ExtraPacking = 1 }

/// <summary>One material batch issued against a stock requisition, or one extra packing issue.</summary>
public sealed class StockIssuedRowDto
{
    public Guid Id { get; set; }
    public StockIssueSource Source { get; set; }
    public DateTime IssuedAt { get; set; }
    public string? IssuedByName { get; set; }
    public string? RequisitionCode { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? BatchNumber { get; set; }
    public Guid? MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialName { get; set; }
    public MaterialKind? MaterialKind { get; set; }
    /// <summary>The material batch taken from stock; null for extra packing, which is not recorded per batch.</summary>
    public string? MaterialBatchNumber { get; set; }
    public string? ArNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public string? WarehouseName { get; set; }
}

public sealed class StockIssuedReportDto
{
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? ProductId { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<StockIssuedRowDto> Rows { get; set; } = [];
}
