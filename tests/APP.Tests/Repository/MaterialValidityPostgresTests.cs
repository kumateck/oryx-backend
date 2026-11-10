using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SHARED;
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
    public async Task Density_migration_reservation_requisition_and_issue_agree_in_Postgres()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();

        var migration = new INFRASTRUCTURE.Migrations.AddBomDensityConversion();
        async Task ExecuteMigration(IReadOnlyList<Microsoft.EntityFrameworkCore.Migrations.Operations.MigrationOperation> operations)
        {
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(operations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        }

        await ExecuteMigration(migration.DownOperations);
        await ExecuteMigration(migration.UpOperations);
        var bomItems = await db.BillOfMaterialItems.ToListAsync();
        Assert.All(bomItems, item => Assert.False(item.UseDensityConversion));

        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml" };
        var density = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        db.UnitOfMeasures.AddRange(ml, density);
        foreach (var item in bomItems)
        {
            item.UseDensityConversion = true;
            item.PrescribedQuantity = 1_000_000m;
        }
        foreach (var link in await db.MaterialDepartments.OrderBy(item => item.MaterialId).ToListAsync())
        {
            link.UoM = ml;
            link.DensityUoM = density;
            link.Density = link.MaterialId == bomItems[0].MaterialId ? 1.5m : 1.25m;
        }
        foreach (var row in await db.MaterialBatches.ToListAsync()) row.UoM = ml;
        foreach (var row in await db.ShelfMaterialBatches.ToListAsync()) row.UoMId = ml.Id;
        foreach (var row in await db.BinCardInformation.ToListAsync()) row.UoMId = ml.Id;
        await db.MaterialBatchReservedQuantities.IgnoreQueryFilters()
            .Where(row => row.ProductionScheduleProductId == fixture.BatchId).ExecuteDeleteAsync();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var actor = fixture.Actor.UserId.GetValueOrDefault();
        var production = new ProductionScheduleRepository(db, null!, null!, null!, null!, null!,
            new NoOpProductionActivityStepEventPublisher());
        await using (var blockingDb = fixture.Open())
        {
            await using var blocker = await blockingDb.Database.BeginTransactionAsync();
            await blockingDb.Database.ExecuteSqlRawAsync("LOCK TABLE \"ProductionActivities\" IN ACCESS EXCLUSIVE MODE");
            var failed = await production.StartProductionActivity(fixture.BatchId, actor);
            Assert.Equal("Production.Start", failed.Error.Code);
            Assert.Null(db.Database.CurrentTransaction);
        }
        await production.FreezeMaterialInProduction(fixture.BatchId);

        var configuration = await db.ProductionMaterialConfigurations.SingleAsync(row => row.Kind == MaterialKind.Raw);
        var requirements = ProductionMaterialSources.Requirements(configuration.RequirementsJson);
        Assert.Equal(2, requirements.Count);
        Assert.Contains(requirements, item => item.Quantity == 666.66666666666666666666666667m);
        Assert.Contains(requirements, item => item.Quantity == 800m);
        var lines = requirements.Select(item => new CreateRequisitionItemRequest
        {
            MaterialId = item.MaterialId, UomId = item.UnitId,
            Quantity = item.DensityGramsPerMilliliter == 1.5m ? 666.6666666666666m : 800m
        }).ToList();
        var checkedLines = await ProductionMaterialRequisitionRules.Validate(db, fixture.BatchId, lines);
        Assert.True(checkedLines.IsSuccess, checkedLines.Error.Description);
        var reservations = await db.MaterialBatchReservedQuantities.Where(row => row.ReleasedAt == null).ToListAsync();
        Assert.All(reservations, row => Assert.Equal(ml.Id, row.UoMId));
        Assert.Equal(lines.Sum(item => item.Quantity), reservations.Sum(item => item.Quantity));

        db.Requisitions.Add(new Requisition
        {
            Id = fixture.RequisitionId, Code = "RM/DENSITY/SYNTHETIC",
            DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), RequestedById = actor,
            ProductionScheduleProductId = fixture.BatchId, RequisitionType = RequisitionType.Stock,
            Approved = true, Status = RequestStatus.Pending,
            Items = lines.Select(line => new RequisitionItem
            {
                MaterialId = line.MaterialId, UoMId = line.UomId, Quantity = line.Quantity, Status = RequestStatus.Pending
            }).ToList()
        });
        await db.SaveChangesAsync();
        var stockBefore = await db.ShelfMaterialBatches.SumAsync(row => row.Quantity);
        var requisitions = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!,
            new NoOpProductionActivityStepEventPublisher());
        var issued = await requisitions.IssueStockRequisition(fixture.RequisitionId, actor);
        Assert.True(issued.IsSuccess, issued.Error.Description);
        Assert.Equal(stockBefore - lines.Sum(item => item.Quantity), await db.ShelfMaterialBatches.SumAsync(row => row.Quantity));

        await ExecuteMigration(migration.DownOperations);
        Assert.Equal(2L, await db.Database.SqlQueryRaw<long>("SELECT COUNT(*) AS \"Value\" FROM \"BillOfMaterialItems\"").SingleAsync());
    }

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
