using DOMAIN.Entities.Products;
using SHARED;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionScheduleReportDto
{
    public ProductListDto Product { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BatchSize { get; set; }
    public decimal Batches { get; set; }
    public decimal ExpectedQuantity => BatchSize * Batches;
    public decimal ExpectedAmount => ExpectedQuantity * UnitPrice;
    public decimal ActualQuantity{ get; set; }
    public decimal ActualAmount => ActualQuantity * UnitPrice;
    public CollectionItemDto MarketType { get; set; }
}

public class ProductionScheduleDetailedReportDto
{
    public decimal UnitPrice { get; set; }
    public string BatchNumber { get; set; }
    public string PackageStyle { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ExpectedAmount => ExpectedQuantity * UnitPrice;
    public decimal ActualQuantity{ get; set; }
    public decimal ActualAmount => ActualQuantity * UnitPrice;
}

public class ProductionScheduleReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid ProductId { get; set; }
    public Guid? MarketTypeId { get; set; }
}