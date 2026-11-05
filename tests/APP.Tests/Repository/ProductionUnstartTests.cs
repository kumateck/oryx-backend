using APP.Mapper;
using APP.Repository;
using APP.Services.Production;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductionUnstartTests
{
    private sealed class NoOpBackgroundWorker : APP.Services.Background.IBackgroundWorkerService
    {
        public void EnqueueLog(DOMAIN.Entities.ActivityLogs.CreateActivityLog log) { }
        public void EnqueuePrevStateCapture(DOMAIN.Entities.ActivityLogs.PrevStateCaptureRequest request) { }
        public void EnqueueNotification(string message, DOMAIN.Entities.Notifications.NotificationType type, Guid? departmentId = null,
            List<DOMAIN.Entities.Users.User> users = null!, Guid? shipmentDocumentId = null, string relatedEntityType = null!, Guid? relatedEntityId = null,
            string relatedEntityCode = null!, string relatedEntityName = null!, string relatedEntityStatus = null!, string relatedEntityPriority = null!) { }
        public Task DoWork(CancellationToken stoppingToken) => Task.CompletedTask;
    }
    private static async Task<Guid> Setup(IssuedMaterialRepairFixture fixture)
    {
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        var run = await db.ProductionActivities.Include(item => item.Steps).ThenInclude(item => item.Operation).SingleAsync();
        foreach (var step in run.Steps) { step.Status = ProductionStatus.New; step.StartedAt = null; }
        var first = run.Steps.OrderBy(item => item.Order).First();
        first.Operation.Name = "BMR requisition and issue";
        db.BatchManufacturingRecords.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = fixture.BatchId, ProductionActivityStepId = first.Id, BatchQuantity = 311_250_000 });
        db.BatchPackagingRecords.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = fixture.BatchId, ProductionActivityStepId = first.Id, BatchQuantity = 311_250_000 });
        db.Routes.Add(new() { ProductId = fixture.ProductId, OperationId = first.OperationId, Order = 1,
            ResponsibleUsers = [new() { UserId = fixture.Actor.UserId.GetValueOrDefault(), Action = DOMAIN.Entities.Routes.OperationAction.BmrAndBprRequisitionOrIssue }] });
        db.RoleClaims.Add(new() { Id = 4, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanUnstartProduction });
        db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), RoleClaimId = 4, Key = PermissionKeys.CanUnstartProduction, Type = "Action" });
        await db.SaveChangesAsync();
        // Exercise the complete deployment migration from its pre-repair schema baseline.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ProductionUnstartRepairs\"; ALTER TABLE \"MaterialBatchReservedQuantities\" DROP COLUMN \"ProductionUnstartRepairId\"; DROP INDEX \"IX_ProductionActivities_ProductionScheduleProductId\"; CREATE UNIQUE INDEX \"IX_ProductionActivities_ProductionScheduleProductId\" ON \"ProductionActivities\" (\"ProductionScheduleProductId\")");
        var migration = new INFRASTRUCTURE.Migrations.AddProductionUnstartRepair();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        return run.Id;
    }

    private static ProductionUnstartService Service(ApplicationDbContext db, IssuedMaterialRepairFixture fixture) => new(db, fixture.Actor);
    private static async Task<ProductionUnstartRequest> Request(IssuedMaterialRepairFixture fixture, Guid activity)
    {
        await using var db = fixture.Open(); var preview = await Service(db, fixture).Preview(activity);
        Assert.True(preview.IsSuccess, preview.Error.Description); Assert.True(preview.Value.CanApply, string.Join(" ", preview.Value.Blockers));
        return new() { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken, Reason = "Accidental start before BMR issue" };
    }

    private static ServiceProvider Mapping(ApplicationDbContext db)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(db);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        services.AddAutoMapper(config => { }, typeof(OryxMapper)); return services.BuildServiceProvider();
    }
    private static ProductionScheduleRepository Repository(ApplicationDbContext db, IMapper mapper) =>
        new(db, mapper, null!, new MaterialRepository(db, mapper), new NoOpBackgroundWorker(), null!, new NoOpProductionActivityStepEventPublisher());

    [RepairPostgresFact]
    public async Task Unstart_archives_drafts_releases_reservations_preserves_stock_and_can_restart_once()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        await using (var db = fixture.Open())
        {
            var productionId = await db.Warehouses.Where(item => item.Type == DOMAIN.Entities.Warehouses.WarehouseType.Production).Select(item => item.Id).SingleAsync();
            // A restart validates both saved material definitions, including a real packing style.
            var style = new DOMAIN.Entities.Products.ProductPacking { ProductId = fixture.ProductId, Name = "Restart fixture" };
            db.ProductPackings.Add(style);
            (await db.ProductionScheduleProducts.SingleAsync()).ProductPackingId = style.Id;
            var material = new Material { Name = "Restart foil", Code = "PM-RESTART", Kind = MaterialKind.Package };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new() { MaterialId = material.Id, DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), UoMId = fixture.UnitId });
            db.ProductPackages.Add(new() { ProductId = fixture.ProductId, ProductPackingId = style.Id, MaterialId = material.Id, PrescribedQuantity = 1 });
            var warehouse = await db.Warehouses.SingleAsync(item => item.Type == DOMAIN.Entities.Warehouses.WarehouseType.PackagedStorage);
            var shelf = new DOMAIN.Entities.Warehouses.WarehouseLocationShelf { Name = "Restart packing", Code = "RESTART/PM", WarehouseLocationRack = new()
                { Name = "Rack", WarehouseLocation = new() { Name = "Room", WarehouseId = warehouse.Id } } };
            db.WarehouseLocationShelves.Add(shelf);
            var lot = new DOMAIN.Entities.Materials.Batch.MaterialBatch { MaterialId = material.Id, UoMId = fixture.UnitId,
                BatchNumber = "RESTART-PM", Status = DOMAIN.Entities.Materials.Batch.BatchStatus.Available, TotalQuantity = 1, QuantityAssigned = 1,
                ExpiryDate = DateTime.UtcNow.AddYears(1) };
            db.MaterialBatches.Add(lot);
            db.ShelfMaterialBatches.Add(new() { MaterialBatchId = lot.Id, WarehouseLocationShelfId = shelf.Id, UoMId = fixture.UnitId, Quantity = 1 });
            db.ProductionMaterialConfigurations.Add(new ProductionMaterialConfiguration
            {
                ProductionScheduleProductId = fixture.BatchId, Kind = MaterialKind.Raw, DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(),
                StorageWarehouseId = fixture.SourceId, ProductionWarehouseId = productionId, Revision = 1,
                SourceHash = "prior-run-configuration", RequirementsJson = "[]"
            });
            await db.SaveChangesAsync();
        }
        var request = await Request(fixture, run);
        await using (var db = fixture.Open())
        {
            var result = await Service(db, fixture).Apply(run, request); Assert.True(result.IsSuccess, result.Error.Description);
            Assert.Equal(fixture.BatchId, result.Value.BatchId);
        }
        await using (var db = fixture.Open())
        {
            Assert.Empty(await db.ProductionActivities.ToListAsync()); Assert.Empty(await db.ProductionActivitySteps.ToListAsync());
            Assert.Empty(await db.BatchManufacturingRecords.ToListAsync()); Assert.Empty(await db.BatchPackagingRecords.ToListAsync());
            Assert.Empty(await db.MaterialBatchReservedQuantities.ToListAsync());
            Assert.Empty(await db.ProductionMaterialConfigurations.ToListAsync());
            Assert.Equal(400_000_001, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync()); Assert.Empty(await db.MaterialBatchEvents.ToListAsync());
            Assert.Equal(ProductionStatus.New, (await db.ProductionSchedules.SingleAsync()).Status);
            Assert.Equal(2, await db.ProductionActivitySteps.IgnoreQueryFilters().CountAsync());
            var audit = await db.ProductionUnstartRepairs.SingleAsync(); Assert.Contains("Accidental", audit.Reason);
            Assert.All(await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().ToListAsync(), item => Assert.Equal(audit.Id, item.ProductionUnstartRepairId));
            Assert.All(await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().ToListAsync(), item => Assert.Equal(audit.AppliedAt, item.ReleasedAt));
            Assert.Contains("prior-run-configuration", audit.BeforeJson);
            using var mapping = Mapping(db);
            var material = await db.MaterialBatches.FirstAsync();
            var productionId = await db.Warehouses.Where(item => item.Type == DOMAIN.Entities.Warehouses.WarehouseType.Production).Select(item => item.Id).SingleAsync();
            Assert.Empty(await new MaterialRepository(db, mapping.GetRequiredService<IMapper>()).GetConsumedBatchesAndQuantityForProductionWarehouse(material.MaterialId, productionId, fixture.BatchId));
            Assert.Contains("311250000", audit.BeforeJson); Assert.NotEqual(audit.BeforeJson, audit.AfterJson);
            Assert.True((await Service(db, fixture).Apply(run, request)).Value.AlreadyApplied);
            request.Reason = "Conflicting reuse of the same request";
            Assert.Equal("Unstart.RequestReused", (await Service(db, fixture).Apply(run, request)).Error.Code);
            request.Reason = "Accidental start before BMR issue";
        }
        Guid restarted;
        await using (var db = fixture.Open())
        {
            using var provider = Mapping(db);
            var result = await Repository(db, provider.GetRequiredService<IMapper>()).StartProductionActivity(fixture.BatchId, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(result.IsSuccess, result.Error.Description); restarted = result.Value; Assert.NotEqual(run, restarted);
        }
        await using (var db = fixture.Open())
        {
            Assert.Single(await db.ProductionActivities.ToListAsync()); Assert.Single(await db.BatchManufacturingRecords.ToListAsync());
            Assert.Equal(3, await db.MaterialBatchReservedQuantities.CountAsync());
            Assert.Equal(2, await db.ProductionMaterialConfigurations.CountAsync());
            Assert.Equal(2, (await db.ProductionMaterialConfigurations.SingleAsync(item => item.Kind == MaterialKind.Raw)).Revision);
            Assert.True((await Service(db, fixture).Apply(run, request)).Value.AlreadyApplied);
            request.RequestId = Guid.NewGuid(); Assert.True((await Service(db, fixture).Apply(run, request)).Value.AlreadyApplied);
            Assert.Equal(restarted, (await db.ProductionActivities.SingleAsync()).Id);
            using var provider = Mapping(db);
            Assert.True((await Repository(db, provider.GetRequiredService<IMapper>()).StartProductionActivity(fixture.BatchId, fixture.Actor.UserId.GetValueOrDefault())).IsFailure);
        }
        // A fresh repair of the new run can release its reservations without reusing old ones.
        var next = await Request(fixture, restarted);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(restarted, next)).IsSuccess);
        await using (var db = fixture.Open())
        {
            Assert.Empty(await db.MaterialBatchReservedQuantities.ToListAsync());
            Assert.Equal(5, await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().CountAsync());
            Assert.Equal(2, await db.ProductionUnstartRepairs.CountAsync());
            Assert.Equal(400_000_001, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        }
    }

    [RepairPostgresFact]
    public async Task Production_starts_when_raw_materials_are_ready_even_if_packaging_is_not_yet_in_stock()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        var request = await Request(fixture, run);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(run, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            foreach (var shelf in await db.ShelfMaterialBatches.Where(item => item.MaterialBatch.Material.Kind == MaterialKind.Package).ToListAsync())
                shelf.Quantity = 0;
            await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            using var provider = Mapping(db);
            var result = await Repository(db, provider.GetRequiredService<IMapper>()).StartProductionActivity(fixture.BatchId, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(result.IsSuccess, result.Error.Description);
        }
        await using (var db = fixture.Open())
        {
            // Raw materials are reserved and configured; packaging is left for the later packaging requisition.
            Assert.Equal(MaterialKind.Raw, (await db.ProductionMaterialConfigurations.SingleAsync()).Kind);
            var held = await db.MaterialBatchReservedQuantities.Select(item => item.MaterialBatch.Material.Kind).ToListAsync();
            Assert.NotEmpty(held); Assert.All(held, kind => Assert.Equal(MaterialKind.Raw, kind));
        }
    }

    [RepairPostgresFact]
    public async Task Production_start_is_still_refused_when_a_raw_material_is_short()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        var request = await Request(fixture, run);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(run, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            foreach (var shelf in await db.ShelfMaterialBatches.Where(item => item.MaterialBatch.Material.Kind == MaterialKind.Raw).ToListAsync())
                shelf.Quantity = 0;
            await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            using var provider = Mapping(db);
            var result = await Repository(db, provider.GetRequiredService<IMapper>()).StartProductionActivity(fixture.BatchId, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(result.IsFailure);
            Assert.Contains("All raw materials must have valid configuration", result.Error.Description);
        }
        await using (var db = fixture.Open()) Assert.Empty(await db.ProductionActivities.ToListAsync());
    }

    [RepairPostgresFact]
    public async Task Unissued_holds_are_released_without_recreating_shelf_stock_even_when_the_balance_changed()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        await using (var db = fixture.Open())
        {
            foreach (var shelf in await db.ShelfMaterialBatches.ToListAsync()) shelf.Quantity = 0;
            await db.SaveChangesAsync();
        }
        var request = await Request(fixture, run);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(run, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            Assert.Empty(await db.MaterialBatchReservedQuantities.ToListAsync());
            Assert.Equal(0, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync());
            Assert.Empty(await db.StockRequisitionIssues.ToListAsync());
            Assert.All(await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().ToListAsync(), item => Assert.NotNull(item.ReleasedAt));
        }
    }

    [RepairPostgresFact]
    public async Task Issued_or_progressed_documents_and_downstream_records_block_unstart()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture); var request = await Request(fixture, run);
        await using (var db = fixture.Open())
        {
            var bmr = await db.BatchManufacturingRecords.SingleAsync(); bmr.IssuedDate = DateTime.UtcNow; await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var preview = await Service(db, fixture).Preview(run); Assert.False(preview.Value.CanApply);
            Assert.Contains(preview.Value.Blockers, text => text.Contains("unissued"));
            Assert.True((await Service(db, fixture).Apply(run, request)).IsFailure);
            Assert.Single(await db.ProductionActivities.ToListAsync()); Assert.Equal(2, await db.MaterialBatchReservedQuantities.CountAsync());
        }
    }

    [RepairPostgresFact]
    public async Task Access_revocation_stale_preview_and_new_dependencies_do_not_change_stock()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture); var request = await Request(fixture, run);
        await using (var db = fixture.Open())
        {
            var claim = await db.RoleClaims.SingleAsync(item => item.ClaimValue == PermissionKeys.CanUnstartProduction);
            db.RoleClaims.Remove(claim); await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var denied = await Service(db, fixture).Apply(run, request); Assert.True(denied.IsFailure); Assert.Equal("Unstart.Permission", denied.Error.Code);
            db.RoleClaims.Add(new() { Id = 4, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanUnstartProduction }); await db.SaveChangesAsync();
            var bmr = await db.BatchManufacturingRecords.SingleAsync(); bmr.BatchNumber = "Changed after preview"; await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var stale = await Service(db, fixture).Apply(run, request); Assert.True(stale.IsFailure); Assert.Equal("Unstart.StalePreview", stale.Error.Code);
            db.Requisitions.Add(new() { ProductionScheduleProductId = fixture.BatchId, DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), RequestedById = fixture.Actor.UserId.GetValueOrDefault(), Code = "SYNTHETIC/DOWNSTREAM", Items = [] });
            await db.SaveChangesAsync();
            Assert.Contains((await Service(db, fixture).Preview(run)).Value.Blockers, text => text.Contains("Requisitions"));
            Assert.Empty(await db.ProductionUnstartRepairs.ToListAsync()); Assert.Equal(400_000_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            (await db.Products.SingleAsync(item => item.Id == fixture.ProductId)).DepartmentId = null;
            await db.SaveChangesAsync();
            Assert.Equal("Unstart.Activity", (await Service(db, fixture).Preview(run)).Error.Code);
        }
    }

    [RepairPostgresFact]
    public async Task Concurrent_unstarts_and_rollback_preserve_all_evidence()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture); var request = await Request(fixture, run);
        await using (var db = fixture.Open()) await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductionUnstartRepairs\" ADD CONSTRAINT reject_unstart_test CHECK (false) NOT VALID");
        await using (var db = fixture.Open())
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => Service(db, fixture).Apply(run, request)); Assert.Empty(db.ChangeTracker.Entries()); await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            Assert.Single(await db.ProductionActivities.ToListAsync()); Assert.Equal(2, await db.MaterialBatchReservedQuantities.CountAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductionUnstartRepairs\" DROP CONSTRAINT reject_unstart_test");
        }
        async Task<bool> Apply()
        {
            await using var db = fixture.Open();
            try { var result = await Service(db, fixture).Apply(run, request); return result.IsSuccess && !result.Value.AlreadyApplied; }
            catch (PostgresException error) when (error.SqlState is "40001" or "40P01") { return false; }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "23505" }) { return false; }
        }
        Assert.Single(await Task.WhenAll(Apply(), Apply()), applied => applied);
        await using (var db = fixture.Open())
        {
            Assert.Single(await db.ProductionUnstartRepairs.ToListAsync());
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"ProductionUnstartRepairs\""));
        }
    }

    [RepairPostgresFact]
    public async Task Stale_BMR_issue_cannot_issue_an_archived_document()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        await using (var db = fixture.Open())
        {
            (await db.ProductionScheduleProducts.SingleAsync()).BatchNumber = null!;
            (await db.BatchManufacturingRecords.SingleAsync()).BatchNumber = "PRESERVE-DRAFT-NUMBER";
            (await db.BatchPackagingRecords.SingleAsync()).BatchNumber = "PRESERVE-DRAFT-NUMBER";
            await db.SaveChangesAsync();
        }
        var request = await Request(fixture, run);
        await using var stale = fixture.Open(); var document = await stale.BatchManufacturingRecords.Include(item => item.ProductionActivityStep).SingleAsync();
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(run, request)).IsSuccess);
        document.IssuedDate = DateTime.UtcNow;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        await using (var db = fixture.Open())
        {
            using var mapping = Mapping(db);
            var result = await Repository(db, mapping.GetRequiredService<IMapper>()).IssueBatchManufacturingRecord(document.Id, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(result.IsFailure);
            Assert.Null((await db.BatchManufacturingRecords.IgnoreQueryFilters().SingleAsync()).IssuedDate);
            Assert.Equal("PRESERVE-DRAFT-NUMBER", (await db.ProductionScheduleProducts.SingleAsync()).BatchNumber);
        }
    }

    [RepairPostgresFact]
    public async Task Unstarting_one_batch_preserves_other_active_runs_and_late_writers_are_refused()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture);
        Guid firstStep;
        await using (var db = fixture.Open())
        {
            var first = await db.ProductionActivitySteps.OrderBy(item => item.Order).FirstAsync(); firstStep = first.Id;
            first.Status = ProductionStatus.InProgress; first.StartedAt = DateTime.UtcNow;
            var batch = await db.ProductionScheduleProducts.SingleAsync();
            var otherBatch = new DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.ProductId,
                ProductionScheduleId = batch.ProductionScheduleId, BatchNumber = "OTHER-RUN" };
            db.ProductionScheduleProducts.Add(otherBatch); db.ProductionActivities.Add(new() { ProductionScheduleProductId = otherBatch.Id });
            await db.SaveChangesAsync();
        }
        var request = await Request(fixture, run);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(run, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            Assert.Single(await db.ProductionActivities.ToListAsync()); Assert.Equal(ProductionStatus.InProgress, (await db.ProductionSchedules.SingleAsync()).Status);
            db.Requisitions.Add(new() { ProductionActivityStepId = firstStep, ProductionScheduleProductId = fixture.BatchId,
                RequestedById = fixture.Actor.UserId.GetValueOrDefault(), DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), Code = "STALE-REQUEST" });
            var blocked = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal("23514", Assert.IsType<PostgresException>(blocked.InnerException).SqlState);
        }
        await using (var db = fixture.Open())
        {
            var old = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().FirstAsync();
            db.MaterialBatchReservedQuantities.Add(new() { MaterialBatchId = old.MaterialBatchId, UoMId = old.UoMId,
                WarehouseId = old.WarehouseId, WarehouseLocationShelfId = old.WarehouseLocationShelfId, ProductionScheduleProductId = fixture.BatchId, Quantity = 1 });
            var blocked = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal("23514", Assert.IsType<PostgresException>(blocked.InnerException).SqlState);
        }
    }

    [RepairPostgresFact]
    public async Task Issuance_racing_unstart_cannot_leave_an_issued_archived_document()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var run = await Setup(fixture); var request = await Request(fixture, run);
        Guid bmr;
        await using (var db = fixture.Open()) bmr = await db.BatchManufacturingRecords.Select(item => item.Id).SingleAsync();
        async Task Issue()
        {
            await using var db = fixture.Open(); using var mapping = Mapping(db);
            await Repository(db, mapping.GetRequiredService<IMapper>()).IssueBatchManufacturingRecord(bmr, fixture.Actor.UserId.GetValueOrDefault());
        }
        async Task Unstart()
        {
            await using var db = fixture.Open();
            try { await Service(db, fixture).Apply(run, request); }
            catch (PostgresException error) when (error.SqlState is "40001" or "40P01") { }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "23505" }) { }
        }
        await Task.WhenAll(Issue(), Unstart());
        await using (var db = fixture.Open())
        {
            var record = await db.BatchManufacturingRecords.IgnoreQueryFilters().SingleAsync();
            Assert.False(record.DeletedAt.HasValue && record.IssuedDate.HasValue);
            Assert.True(record.DeletedAt.HasValue || record.IssuedDate.HasValue);
            Assert.Equal(record.DeletedAt.HasValue, await db.ProductionUnstartRepairs.AnyAsync());
        }
    }
}
