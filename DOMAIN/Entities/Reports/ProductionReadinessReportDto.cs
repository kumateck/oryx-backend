#nullable enable
using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Reports;

public sealed class ProductionReadinessFilter
{
    [Required] public Guid? DepartmentId { get; set; }
    /// <summary>Inclusive UTC calendar date; selects schedules overlapping the range.</summary>
    [Required] public DateOnly? StartDate { get; set; }
    [Required] public DateOnly? EndDate { get; set; }
}

public enum DefinitionReadiness { Missing = 0, Configured = 1, Unavailable = 2 }
public enum ProductReadiness { ActionNeeded = 0, Ready = 1, CouldNotVerify = 2 }

/// <summary>Configuration presence only, not stock availability, approval or release authorization.</summary>
public sealed class ProductConfigurationDto
{
    public DefinitionReadiness Specification { get; set; }
    public DefinitionReadiness Stp { get; set; }
    public DefinitionReadiness Ard { get; set; }
    public DefinitionReadiness Procedure { get; set; }
    public DefinitionReadiness Bom { get; set; }
    public DefinitionReadiness Packaging { get; set; }
    public DefinitionReadiness PackingStyle { get; set; }
    public ProductReadiness Readiness
    {
        get
        {
            DefinitionReadiness[] checks = [Specification, Stp, Ard, Procedure, Bom, Packaging, PackingStyle];
            return checks.Contains(DefinitionReadiness.Unavailable) ? ProductReadiness.CouldNotVerify
                : checks.All(check => check == DefinitionReadiness.Configured) ? ProductReadiness.Ready
                : ProductReadiness.ActionNeeded;
        }
    }
}

public sealed class ReadinessMarketDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class PlannedProductReadinessDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Batches { get; set; }
    public List<ReadinessMarketDto> Markets { get; set; } = [];
    public ProductConfigurationDto Configuration { get; set; } = new();
}

public sealed class ProductionReadinessRowDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public Guid? ProductPackingId { get; set; }
    public string PackingStyle { get; set; } = "";
    public List<Guid> ScheduleProductIds { get; set; } = [];
    public decimal Batches { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? PriceUnit { get; set; }
    public decimal? ExpectedYield { get; set; }
    public int? PacksPerShipper { get; set; }
    public decimal? Shippers { get; set; }
    public decimal? LoosePacks { get; set; }
    public ProductConfigurationDto Configuration { get; set; } = new();
}

public sealed class ProductionReadinessTotalsDto
{
    public decimal Batches { get; set; }
    public decimal? ExpectedYield { get; set; }
    public decimal? Shippers { get; set; }
    public decimal? LoosePacks { get; set; }
}

public sealed class ProductionReadinessGroupDto
{
    public ReadinessMarketDto Market { get; set; } = new();
    public List<ProductionReadinessRowDto> Rows { get; set; } = [];
    public ProductionReadinessTotalsDto Totals { get; set; } = new();
}

public sealed class ProductionReadinessReportDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int ProductCount { get; set; }
    public int ReadyProductCount { get; set; }
    public int ActionNeededProductCount { get; set; }
    public int UnavailableProductCount { get; set; }
    public List<ProductionReadinessGroupDto> Markets { get; set; } = [];
    public List<PlannedProductReadinessDto> Products { get; set; } = [];
    public ProductionReadinessTotalsDto Totals { get; set; } = new();
    public List<string> Warnings { get; set; } = [];
}
