using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;

namespace APP.Repository;

internal sealed class ProductionReadinessSource
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public Guid? MarketId { get; set; }
    public string MarketName { get; set; }
    public Guid? PackingId { get; set; }
    public Guid? PackingProductId { get; set; }
    public string PackingName { get; set; }
    public BatchSize BatchSize { get; set; }
    public DateTime ScheduleStart { get; set; }
    public DateTime ScheduleEnd { get; set; }
    public decimal? ExpectedYield { get; set; }
    public int? PacksPerShipper { get; set; }
    public decimal? UnitPrice { get; set; }
    public string PriceUnit { get; set; }
    public bool Specification { get; set; }
    public bool Stp { get; set; }
    public bool Ard { get; set; }
    public bool Procedure { get; set; }
    public bool Bom { get; set; }
    public bool Packaging { get; set; }
    public bool PackingStyle { get; set; }
}

internal static class ProductionReadinessReportBuilder
{
    public static ProductionReadinessReportDto Build(Guid departmentId, string departmentName,
        DateOnly start, DateOnly end, List<ProductionReadinessSource> source)
    {
        var products = source.GroupBy(item => item.ProductId).Select(group =>
        {
            var first = group.First();
            return new PlannedProductReadinessDto
            {
                ProductId = group.Key, ProductCode = first.ProductCode, ProductName = first.ProductName,
                Batches = group.Sum(item => Share(item.BatchSize)), Configuration = Configuration(first),
                Markets = group.GroupBy(item => item.MarketId).Select(market => Market(market.First()))
                    .OrderBy(market => market.Name, StringComparer.Ordinal).ThenBy(market => market.Id).ToList()
            };
        }).OrderBy(item => item.ProductName, StringComparer.Ordinal).ThenBy(item => item.ProductId).ToList();
        var configuration = products.ToDictionary(product => product.ProductId, product => product.Configuration);
        var markets = source.GroupBy(item => item.MarketId).Select(market =>
        {
            var rows = market.GroupBy(item => (item.ProductId, item.PackingId)).Select(group =>
            {
                var first = group.First();
                var batches = group.Sum(item => Share(item.BatchSize));
                var yield = first.ExpectedYield is >= 0
                    ? decimal.Round(batches * first.ExpectedYield.Value, 0, MidpointRounding.AwayFromZero) : (decimal?)null;
                var perShipper = first.PacksPerShipper is > 0 ? first.PacksPerShipper : null;
                var shippers = yield.HasValue && perShipper.HasValue
                    ? decimal.Floor(yield.Value / perShipper.Value) : (decimal?)null;
                return new ProductionReadinessRowDto
                {
                    ProductId = first.ProductId, ProductName = first.ProductName, ProductCode = first.ProductCode,
                    ProductPackingId = first.PackingId, PackingStyle = first.PackingName ?? "No packing style",
                    ScheduleProductIds = group.Select(item => item.Id).Order().ToList(), Batches = batches,
                    UnitPrice = first.UnitPrice, PriceUnit = first.PriceUnit, ExpectedYield = yield,
                    PacksPerShipper = perShipper, Shippers = shippers,
                    LoosePacks = shippers.HasValue ? yield - shippers * perShipper : null,
                    Configuration = configuration[first.ProductId]
                };
            }).OrderBy(row => row.ProductName, StringComparer.Ordinal)
                .ThenBy(row => row.PackingStyle, StringComparer.Ordinal).ThenBy(row => row.ProductId)
                .ThenBy(row => row.ProductPackingId).ToList();
            return new ProductionReadinessGroupDto { Market = Market(market.First()), Rows = rows, Totals = Totals(rows) };
        }).OrderBy(group => group.Market.Name, StringComparer.Ordinal).ThenBy(group => group.Market.Id).ToList();
        var allRows = markets.SelectMany(market => market.Rows).ToList();
        var warnings = new List<string>();
        if (allRows.Any(row => row.ExpectedYield is null || row.Shippers is null))
            warnings.Add("Some selected packing definitions have no usable yield or packs per shipper. Affected values and totals are unavailable, not zero.");
        if (allRows.Any(row => row.UnitPrice is null))
            warnings.Add("Some products have no recorded unit price. Missing prices are not zero.");
        return new ProductionReadinessReportDto
        {
            DepartmentId = departmentId, DepartmentName = departmentName, StartDate = start, EndDate = end,
            GeneratedAt = DateTime.UtcNow, Markets = markets, Products = products, ProductCount = products.Count,
            ReadyProductCount = products.Count(product => product.Configuration.Readiness == ProductReadiness.Ready),
            ActionNeededProductCount = products.Count(product => product.Configuration.Readiness == ProductReadiness.ActionNeeded),
            UnavailableProductCount = products.Count(product => product.Configuration.Readiness == ProductReadiness.CouldNotVerify),
            Totals = Totals(allRows), Warnings = warnings
        };
    }

    private static decimal Share(BatchSize size) => size == BatchSize.Half ? 0.5m : 1m;
    private static ReadinessMarketDto Market(ProductionReadinessSource item) =>
        new() { Id = item.MarketId, Name = item.MarketName ?? "No market recorded" };
    private static DefinitionReadiness Check(bool configured) =>
        configured ? DefinitionReadiness.Configured : DefinitionReadiness.Missing;
    private static ProductConfigurationDto Configuration(ProductionReadinessSource item) => new()
    {
        Specification = Check(item.Specification), Stp = Check(item.Stp), Ard = Check(item.Ard),
        Procedure = Check(item.Procedure), Bom = Check(item.Bom), Packaging = Check(item.Packaging),
        PackingStyle = Check(item.PackingStyle)
    };
    private static ProductionReadinessTotalsDto Totals(List<ProductionReadinessRowDto> rows) => new()
    {
        Batches = rows.Sum(row => row.Batches),
        ExpectedYield = rows.All(row => row.ExpectedYield.HasValue) ? rows.Sum(row => row.ExpectedYield) : null,
        Shippers = rows.All(row => row.Shippers.HasValue) ? rows.Sum(row => row.Shippers) : null,
        LoosePacks = rows.All(row => row.LoosePacks.HasValue) ? rows.Sum(row => row.LoosePacks) : null
    };
}
