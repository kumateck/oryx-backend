using APP.Repository;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using Xunit;

namespace APP.Tests.Reporting;

public class ProductionReadinessBuilderTests
{
    [Fact]
    public void Same_product_in_two_markets_keeps_plan_rows_but_counts_readiness_once()
    {
        var first = Source();
        var second = Source();
        second.ProductId = first.ProductId;
        second.PackingId = first.PackingId;
        second.BatchSize = BatchSize.Half;
        var report = Build([first, second]);
        Assert.Equal(2, report.Markets.Count);
        Assert.Equal(1, report.ProductCount);
        Assert.Equal(1, report.ReadyProductCount);
        Assert.Equal(1.5m, Assert.Single(report.Products).Batches);
        Assert.Equal(1.5m, report.Totals.Batches);
        Assert.Equal(1500m, report.Totals.ExpectedYield);
        Assert.Equal(4m, report.Totals.Shippers);
        Assert.Equal(300m, report.Totals.LoosePacks);
        Assert.Equal(2, report.Products[0].Markets.Count);
        Assert.Equal(first.Id, report.Markets.SelectMany(group => group.Rows)
            .Single(row => row.ScheduleProductIds.Contains(first.Id)).ScheduleProductIds[0]);
    }

    [Fact]
    public void Identity_grouping_never_merges_same_named_markets_or_packing_styles()
    {
        var first = Source();
        var second = Source();
        second.ProductId = first.ProductId;
        second.MarketId = first.MarketId;
        second.PackingName = first.PackingName;
        var third = Source();
        third.MarketName = first.MarketName;
        var report = Build([first, second, third]);
        Assert.Equal(2, report.Markets.Count);
        Assert.Equal(2, report.Markets.Single(group => group.Market.Id == first.MarketId).Rows.Count);
    }

    [Fact]
    public void Half_yield_rounds_after_grouping_and_keeps_shipper_remainder()
    {
        var first = Source();
        first.BatchSize = BatchSize.Half;
        first.ExpectedYield = 1001;
        var row = Assert.Single(Assert.Single(Build([first]).Markets).Rows);
        Assert.Equal(501m, row.ExpectedYield);
        Assert.Equal(1m, row.Shippers);
        Assert.Equal(201m, row.LoosePacks);
        var second = Source();
        second.ProductId = first.ProductId;
        second.MarketId = first.MarketId;
        second.PackingId = first.PackingId;
        second.BatchSize = BatchSize.Half;
        second.ExpectedYield = 1001;
        Assert.Equal(1001m, Build([first, second]).Totals.ExpectedYield);
    }

    [Fact]
    public void Missing_values_poison_affected_totals_and_keep_real_zeroes()
    {
        var first = Source();
        first.ExpectedYield = null;
        first.PacksPerShipper = 0;
        first.UnitPrice = null;
        var report = Build([first, Source()]);
        Assert.Null(report.Totals.ExpectedYield);
        Assert.Null(report.Totals.Shippers);
        Assert.Null(report.Totals.LoosePacks);
        Assert.Equal(2, report.Warnings.Count);
        var zero = Source();
        zero.ExpectedYield = 0;
        zero.UnitPrice = 0;
        var row = Assert.Single(Assert.Single(Build([zero]).Markets).Rows);
        Assert.Equal(0m, row.ExpectedYield);
        Assert.Equal(0m, row.Shippers);
        Assert.Equal(0m, row.UnitPrice);
    }

    [Fact]
    public void Empty_plan_is_a_complete_zero_report_and_missing_checks_are_not_ready()
    {
        var empty = Build([]);
        Assert.Empty(empty.Markets);
        Assert.Equal(0m, empty.Totals.ExpectedYield);
        Assert.Equal(0, empty.ProductCount);
        var item = Source();
        item.Ard = false;
        var report = Build([item]);
        Assert.Equal(0, report.ReadyProductCount);
        Assert.Equal(1, report.ActionNeededProductCount);
        Assert.Equal(DefinitionReadiness.Missing, report.Products[0].Configuration.Ard);
    }

    [Fact]
    public void Unavailable_check_never_claims_ready_or_missing()
    {
        Assert.Equal(ProductReadiness.CouldNotVerify,
            new ProductConfigurationDto { Ard = DefinitionReadiness.Unavailable }.Readiness);
    }

    private static ProductionReadinessReportDto Build(List<ProductionReadinessSource> rows) =>
        ProductionReadinessReportBuilder.Build(Guid.NewGuid(), "Production", new(2026, 10, 28), new(2026, 10, 28), rows);
    [Fact]
    public void Expected_tablets_become_packs_before_filling_shippers()
    {
        var tablets = Source();
        tablets.ExpectedYield = 1_000_000;
        tablets.PacksPerShipper = 80;
        tablets.PackQuantities = [80m, 20m, 10m];
        var row = Build([tablets]).Markets.Single().Rows.Single();
        Assert.Equal(5000m, row.Packs);
        Assert.Equal(62m, row.Shippers);
        Assert.Equal(40m, row.LoosePacks);

        tablets.PacksPerShipper = 75;
        var unknown = Build([tablets]);
        Assert.Null(unknown.Markets.Single().Rows.Single().Shippers);
        Assert.Null(unknown.Totals.Packs);
        Assert.Contains(unknown.Warnings, warning => warning.Contains("units do not add up"));
    }

    private static ProductionReadinessSource Source() => new()
    {
        Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product", ProductCode = "P-1",
        MarketId = Guid.NewGuid(), MarketName = "Market", PackingId = Guid.NewGuid(), PackingName = "Packing",
        ExpectedYield = 1000, PacksPerShipper = 300, UnitPrice = 1, PriceUnit = "piece",
        Specification = true, Stp = true, Ard = true, Bom = true, Procedure = true, Packaging = true, PackingStyle = true
    };
}
