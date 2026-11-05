using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;

namespace DOMAIN.Entities.Reports;

/// <summary>Month of the production plan to forecast, for the signed-in user's department.</summary>
public class ProductionMaterialForecastFilter
{
    public int Year { get; set; }
    /// <summary>1–12.</summary>
    public int Month { get; set; }
}

/// <summary>
/// The month's production schedules for one department and, per product (product, batch size and packing
/// style), what one batch needs, what all planned batches need and how much of it the department's
/// storage warehouses can cover now. Free stock is shared out to batches in schedule order, so a material
/// used by several products is never counted twice.
/// </summary>
public class ProductionMaterialForecastDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Inclusive UTC start of the month.</summary>
    public DateTime PeriodStart { get; set; }
    /// <summary>Exclusive UTC end of the month.</summary>
    public DateTime PeriodEnd { get; set; }
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalBatches { get; set; }
    /// <summary>Batches whose raw materials are all reserved, issued or coverable from free stock.</summary>
    public int RawReadyBatches { get; set; }
    /// <summary>Batches whose packaging materials are all reserved, issued or coverable from free stock.</summary>
    public int PackagingReadyBatches { get; set; }
    public List<ProductionMaterialForecastScheduleDto> Schedules { get; set; } = [];
    public List<ProductionMaterialForecastProductDto> Products { get; set; } = [];
    /// <summary>Department-wide problems, such as a missing storage warehouse.</summary>
    public List<string> Warnings { get; set; } = [];
}

public class ProductionMaterialForecastScheduleDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public DateTime ScheduledStartTime { get; set; }
    public DateTime ScheduledEndTime { get; set; }
    public ProductionStatus Status { get; set; }
    public List<ProductionMaterialForecastBatchDto> Batches { get; set; } = [];
}

public class ProductionMaterialForecastBatchDto
{
    /// <summary>The schedule batch (production schedule product) ID.</summary>
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    /// <summary>Matches <see cref="ProductionMaterialForecastProductDto.Key"/>.</summary>
    public string ProductKey { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public string BatchNumber { get; set; }
    public BatchSize BatchSize { get; set; }
    public decimal Quantity { get; set; }
    public Guid? PackingStyleId { get; set; }
    public string PackingStyle { get; set; }
    public bool Started { get; set; }
    /// <summary>Raw materials were reserved for this batch when it started.</summary>
    public bool RawReserved { get; set; }
    public bool PackagingReserved { get; set; }
    public bool RawReady { get; set; }
    public bool PackagingReady { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public class ProductionMaterialForecastProductDto
{
    /// <summary>Product, batch size and packing style.</summary>
    public string Key { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public BatchSize BatchSize { get; set; }
    public Guid? PackingStyleId { get; set; }
    public string PackingStyle { get; set; }
    public int BatchCount { get; set; }
    public List<Guid> BatchIds { get; set; } = [];
    public int RawBatchesSatisfiable { get; set; }
    public int PackagingBatchesSatisfiable { get; set; }
    /// <summary>Batches whose raw and packaging materials can both be covered.</summary>
    public int BatchesSatisfiable { get; set; }
    public List<ProductionMaterialForecastMaterialDto> RawMaterials { get; set; } = [];
    public List<ProductionMaterialForecastMaterialDto> PackagingMaterials { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

/// <summary>All quantities are in <see cref="Unit"/>, the material's stock unit in this department.</summary>
public class ProductionMaterialForecastMaterialDto
{
    public Guid MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }
    public MaterialKind Kind { get; set; }
    public Guid UnitId { get; set; }
    public string Unit { get; set; }
    /// <summary>Quantity for one batch of this product.</summary>
    public decimal PerBatchQuantity { get; set; }
    /// <summary>True when batches of this product need different quantities (for example after a repair).</summary>
    public bool PerBatchQuantityVaries { get; set; }
    /// <summary>Planned batches of this product that need the material.</summary>
    public int BatchCount { get; set; }
    public decimal TotalQuantity { get; set; }
    /// <summary>Already reserved for, or issued to, these batches.</summary>
    public decimal ReservedQuantity { get; set; }
    /// <summary>Reserved plus the free stock shared out to these batches.</summary>
    public decimal CanSatisfyQuantity { get; set; }
    public decimal ShortfallQuantity { get; set; }
    /// <summary>Whole batches whose need for this material is fully covered.</summary>
    public int BatchesSatisfiable { get; set; }
    /// <summary>Usable, unreserved stock of the material in the department's storage warehouse, before sharing out.</summary>
    public decimal FreeInWarehouse { get; set; }
    /// <summary>Other products in this month's plan that also need the material.</summary>
    public int SharedWithProducts { get; set; }
}
