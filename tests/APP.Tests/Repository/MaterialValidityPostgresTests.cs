using APP.Mapper;
using APP.Repository;
using APP.Tests.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports.WarehouseDashboardKpi;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialValidityPostgresTests
{
    [RepairPostgresFact]
    public async Task Extended_raw_stock_can_be_issued_and_is_not_expired_in_forecasts_or_risk_reports()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        // Seed exercises the real issue mutation. This failed its final expiry-only guard before the fix.
        await fixture.Seed(IssuedMaterialRepairKind.Bom, validRetest: true);
        await using var db = fixture.Open();
        Assert.Single(await db.StockRequisitionIssues.ToListAsync());
        Assert.Equal(400_000_000 - 117.5m, await db.ShelfMaterialBatches.SumAsync(row => row.Quantity));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<IHttpContextAccessor, NullHttpContextAccessor>();
        services.AddAutoMapper(_ => { }, typeof(OryxMapper));
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();
        var repository = new ProductionScheduleRepository(db, mapper, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
        var forecast = await repository.ForecastProductionScheduleProduct(fixture.ProductId, 1, Guid.Empty, BatchSize.Full, fixture.Actor.UserId.GetValueOrDefault());
        Assert.True(forecast.IsSuccess, forecast.Error.Description);
        Assert.Equal(2, forecast.Value.Count);
        Assert.All(forecast.Value, row => Assert.Equal(0, row.ExpiredQuantity));
        var reports = new ReportRepository(db, mapper, null!, NullLogger<ReportRepository>.Instance);
        var global = await reports.GetExpiryRiskIndex(new WarehouseKpiFilterDto());
        var branch = await reports.GetExpiryRiskIndex(new WarehouseKpiFilterDto(), fixture.Actor.DepartmentId);
        Assert.Equal("> 90 days", Assert.Single(global.Value).ExpiryWindow);
        Assert.Equal("> 90 days", Assert.Single(branch.Value).ExpiryWindow);
        var expiryProjection = await reports.GetMaterialExpiryProjection();
        Assert.Equal(2, expiryProjection.Value.Count);
        Assert.All(expiryProjection.Value, row => Assert.True(row.EffectiveExpiryDate > DateTime.UtcNow));
        Assert.All(expiryProjection.Value, row => Assert.NotEqual("Expired", row.ExpiryWindow));
        var accountSchedules = await repository.GetProductionSchedules(fixture.Actor.UserId.GetValueOrDefault(), 1, 50, null);
        Assert.True(accountSchedules.IsSuccess, accountSchedules.Error.Description);
        Assert.Single(accountSchedules.Value.Data);
        // The same SQL projection must also work for aggregation and per-warehouse availability.
        var material = await db.Materials.IgnoreQueryFilters().FirstAsync();
        var stock = await new MaterialRepository(db, mapper).GetMaterialStockAcrossWarehouses(material.Id);
        Assert.True(stock.IsSuccess, stock.Error.Description);
        Assert.All(stock.Value, row => Assert.Equal(0, row.ExpiredQuantity));
        var reserved = await new MaterialRepository(db, mapper).GetReservedQuantitiesForMaterial(material.Id, fixture.Actor.DepartmentId);
        Assert.True(reserved.IsSuccess, reserved.Error.Description);
        Assert.True(Assert.Single(reserved.Value).EffectiveExpiryDate > DateTime.UtcNow);
    }
}
