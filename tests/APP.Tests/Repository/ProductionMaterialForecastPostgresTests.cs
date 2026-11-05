using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Runs the forecast's queries on PostgreSQL, where the in-memory provider cannot prove SQL translation.</summary>
public sealed class ProductionMaterialForecastPostgresTests
{
    private static string? Connection => Environment.GetEnvironmentVariable("ORYX_FORECAST_TEST_CONNECTION")
        ?? Environment.GetEnvironmentVariable("ORYX_STOCK_TOPUP_TEST_CONNECTION");

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Connection))
                Skip = "Set ORYX_FORECAST_TEST_CONNECTION (or ORYX_STOCK_TOPUP_TEST_CONNECTION) to an isolated PostgreSQL test server (requires CREATE DATABASE).";
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [PostgresFact]
    public async Task Forecast_runs_on_postgres_with_reservations_retest_dates_and_month_scope()
    {
        var database = "oryx_forecast_test_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Connection);
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            var actor = new Actor();
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
            await db.Database.EnsureCreatedAsync();
            var november = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var department = new Department { Id = Guid.NewGuid(), Name = "Tablet", Type = DepartmentType.Production };
            db.Departments.Add(department);
            db.Users.Add(new User { Id = actor.UserId!.Value, UserName = "forecast-test", DepartmentId = department.Id });
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg", Name = "Milligram", Category = UnitOfMeasureCategory.Weight };
            db.UnitOfMeasures.Add(unit);
            var material = new Material { Id = Guid.NewGuid(), Code = "RM-1", Name = "Starch", Kind = MaterialKind.Raw };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id });
            var storage = new Warehouse { Id = Guid.NewGuid(), Name = "Raw", Type = WarehouseType.RawMaterialStorage, DepartmentId = department.Id };
            var production = new Warehouse { Id = Guid.NewGuid(), Name = "Production", Type = WarehouseType.Production, DepartmentId = department.Id };
            db.Warehouses.AddRange(storage, production,
                new Warehouse { Id = Guid.NewGuid(), Name = "Packing", Type = WarehouseType.PackagedStorage, DepartmentId = department.Id });
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "A", Code = "RAW/A", WarehouseLocationRack = new WarehouseLocationRack
                { Name = "Rack", WarehouseLocation = new WarehouseLocation { Name = "Room", Warehouse = storage } } };
            db.WarehouseLocationShelves.Add(shelf);
            // Past expiry but within its retest date: usable.
            var lot = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = unit.Id, BatchNumber = "LOT-1",
                Status = BatchStatus.Available, TotalQuantity = 1_000, QuantityAssigned = 1_000,
                ExpiryDate = DateTime.UtcNow.AddDays(-10), RetestDate = DateTime.UtcNow.AddDays(60) };
            db.MaterialBatches.Add(lot);
            db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, WarehouseLocationShelfId = shelf.Id, UoMId = unit.Id, Quantity = 1_000 });
            var product = new Product { Id = Guid.NewGuid(), Code = "TAB-1", Name = "Tablet", DepartmentId = department.Id, BaseUomId = unit.Id, FullBatchSize = 10 };
            db.Products.Add(product);
            var bom = new BillOfMaterial { Id = Guid.NewGuid(), ProductId = product.Id, IsActive = true, Items =
                [new BillOfMaterialItem { Id = Guid.NewGuid(), MaterialId = material.Id, BaseUoMId = unit.Id, BaseQuantity = 10, PrescribedQuantity = 300 }] };
            db.ProductBillOfMaterials.Add(new ProductBillOfMaterial { ProductId = product.Id, BillOfMaterial = bom, IsActive = true, EffectiveDate = DateTime.UtcNow.AddDays(-1) });
            var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-NOV", DepartmentId = department.Id,
                ScheduledStartTime = november.AddDays(2), ScheduledEndTime = november.AddDays(9) };
            var december = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-DEC", DepartmentId = department.Id,
                ScheduledStartTime = november.AddMonths(1), ScheduledEndTime = november.AddMonths(1).AddDays(5) };
            db.ProductionSchedules.AddRange(schedule, december);
            foreach (var number in new[] { "B1", "B2", "B3" })
                db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = product.Id,
                    ProductionScheduleId = schedule.Id, BatchSize = BatchSize.Full, BatchNumber = number });
            var holder = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = product.Id,
                ProductionScheduleId = december.Id, BatchSize = BatchSize.Full, BatchNumber = "DEC-1" };
            db.ProductionScheduleProducts.Add(holder);
            db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatchId = lot.Id,
                ProductionScheduleProductId = holder.Id, WarehouseId = production.Id, WarehouseLocationShelfId = shelf.Id, UoMId = unit.Id, Quantity = 300 });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var result = await ProductionMaterialForecasts.Build(db, department.Id, 2026, 11);

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
            var report = result.Value;
            Assert.Equal(3, report.TotalBatches);
            var line = Assert.Single(Assert.Single(report.Products).RawMaterials);
            Assert.Equal(300, line.PerBatchQuantity);
            Assert.Equal(900, line.TotalQuantity);
            Assert.Equal(700, line.FreeInWarehouse);
            Assert.Equal(700, line.CanSatisfyQuantity);
            Assert.Equal(200, line.ShortfallQuantity);
            Assert.Equal(2, line.BatchesSatisfiable);
            Assert.Equal(2, report.RawReadyBatches);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
