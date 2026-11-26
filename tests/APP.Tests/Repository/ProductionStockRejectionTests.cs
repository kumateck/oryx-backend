using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using Xunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Requisitions.Request;
namespace APP.Tests.Repository;
public sealed class ProductionStockRejectionTests
{
    [RepairPostgresFact]
    public async Task Deployment_copies_existing_rejection_access_types_once_for_both_material_kinds()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        await SharedDensityTransferTests.Grant(db, fixture.Actor.UserId!.Value, PermissionKeys.CanRejectPendingApproval);
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"))");
        var migration = new INFRASTRUCTURE.Migrations.GrantProductionStockRequisitionRejection();
        var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
        // Repeating the deployment SQL must not duplicate a claim or access type.
        for (var pass = 0; pass < 2; pass++)
            foreach (var command in generator.Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.ChangeTracker.Clear();
        var grants = await APP.Services.Authorization.CurrentPermissionSet.Load(db, fixture.Actor.UserId);
        foreach (var key in new[] { PermissionKeys.CanRejectRawMaterialStockRequisition, PermissionKeys.CanRejectPackagingMaterialStockRequisition })
        {
            Assert.Contains(key, grants);
            var claim = await db.RoleClaims.SingleAsync(c => c.ClaimValue == key);
            Assert.Equal("Action", (await db.PermissionTypes.SingleAsync(p => p.RoleClaimId == claim.Id && p.Key == key)).Type);
        }
    }
    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Reject_restores_unrequested_material_state_without_releasing_running_batch_reservations(MaterialKind kind)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = kind;
        var req = new Requisition { Id = Guid.NewGuid(), Code = kind == MaterialKind.Raw ? "RM/TEST/1" : "PM/TEST/1", RequisitionType = RequisitionType.Stock,
            ProductionScheduleProductId = f.Batch.Id, DepartmentId = f.Department.Id, RequestedById = f.Actor.Id,
            Items = [new RequisitionItem { Id = Guid.NewGuid(), MaterialId = f.Lot.MaterialId, Material = f.Lot.Material, Quantity = 20 }],
            Approvals = [new RequisitionApproval { Id = Guid.NewGuid(), Required = true, UserId = f.Actor.Id, ActivatedAt = DateTime.UtcNow }] };
        var reservation = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, Quantity = 20,
            UoMId = f.Lot.UoMId, WarehouseId = f.Production.Id, ProductionScheduleProductId = f.Batch.Id };
        db.MaterialBatchReservedQuantities.Add(reservation);
        db.Requisitions.Add(req); await db.SaveChangesAsync();
        Assert.True((await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Correct the material request")).IsFailure);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, StockRequisitionRejection.Key(kind == MaterialKind.Raw ? MaterialKind.Package : MaterialKind.Raw));
        Assert.True((await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Correct the material request")).IsFailure);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, StockRequisitionRejection.Key(kind));
        Assert.True((await StockRequisitionRejection.Validate(db, req, f.Actor.Id, " ")).IsFailure);
        Assert.True((await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Correct the material request")).IsSuccess);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var services = new ServiceCollection().BuildServiceProvider();
        var repository = new ApprovalRepository(db, f.Mapper, f.Harness.UserManager, cache, NullLogger<ApprovalRepository>.Instance, services, new NoOpProductionActivityStepEventPublisher());
        var result = await repository.RejectItem("StockRequisition", req.Id, f.Actor.Id, [], "Correct the material request", contentRevision: 0);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(ApprovalStatus.Rejected, req.Approvals[0].Status);
        Assert.Equal("Correct the material request", (await db.ApprovalActionLogs.SingleAsync()).Comments);
        Assert.Equal(RequestStatus.Rejected, req.Status); Assert.False(req.Approved);
        Assert.Equal(RequestStatus.Rejected, req.Items[0].Status);
        Assert.True((await StockRequisitionRules.CanRaise(db, f.Batch.Id, true, false)).IsSuccess);
        Assert.Null(req.Approvals[0].ActivatedAt);
        Assert.Equal(1_000_000, f.Shelf.Quantity);
        Assert.Null(reservation.ReleasedAt); Assert.Null(reservation.DeletedAt); Assert.Equal(20, reservation.Quantity);
        Assert.True((await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Duplicate")).IsFailure);
    }
    [Fact]
    public async Task Received_or_issued_quantities_are_not_reversed_by_rejection()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanRejectRawMaterialStockRequisition);
        var req = new Requisition { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id,
            Items = [new RequisitionItem { Id = Guid.NewGuid(), Material = f.Lot.Material, QuantityReceived = 1 }] };
        Assert.Equal("StockRequisition.AlreadyIssued", (await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Must use return")).Error.Code);
        req.Items[0].QuantityReceived = 0; req.Status = RequestStatus.Completed;
        Assert.Equal("StockRequisition.AlreadyIssued", (await StockRequisitionRejection.Validate(db, req, f.Actor.Id, "Must use return")).Error.Code);
    }
    [Fact]
    public async Task Legacy_rejection_is_not_an_outstanding_request_but_issued_evidence_prevents_rerequest()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        var req = new Requisition { Id = Guid.NewGuid(), Code = "RM/OLD/1", ProductionScheduleProductId = f.Batch.Id, Status = RequestStatus.New,
            Items = [new RequisitionItem { Id = Guid.NewGuid(), Material = f.Lot.Material, Quantity = 20 }],
            Approvals = [new RequisitionApproval { Id = Guid.NewGuid(), Status = ApprovalStatus.Rejected, ContentRevision = 0 }] };
        db.Requisitions.Add(req); await db.SaveChangesAsync();
        Assert.True((await StockRequisitionRules.CanRaise(db, f.Batch.Id, true, false)).IsSuccess);
        Assert.Equal(RequestStatus.New, req.Status); // Historical header is not silently rewritten.
        db.StockRequisitionIssues.Add(new StockRequisitionIssue { Id = Guid.NewGuid(), RequisitionId = req.Id }); await db.SaveChangesAsync();
        Assert.Equal("StockRequisition.RawAlreadyRaised", (await StockRequisitionRules.CanRaise(db, f.Batch.Id, true, false)).Error.Code);
    }
    [Fact]
    public async Task Late_packaging_is_visible_without_read_writes_and_adopted_only_on_the_first_authorized_request()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = MaterialKind.Package;
        foreach (var store in await db.Warehouses.IgnoreQueryFilters().IgnoreAutoIncludes().Where(w => w.DepartmentId == f.Department.Id && w.Type == WarehouseType.PackagedStorage).ToListAsync()) store.DeletedAt = DateTime.UtcNow;
        f.Storage.Type = WarehouseType.PackagedStorage;
        var style = new ProductPacking { Id = Guid.NewGuid(), ProductId = f.Batch.ProductId, Name = "60 x 100ml" };
        f.Batch.ProductPackingId = style.Id;
        var activity = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch.Id };
        var step = new ProductionActivityStep { Id = Guid.NewGuid(), ProductionActivity = activity,
            Operation = new DOMAIN.Entities.Base.Operation { Id = Guid.NewGuid(), Name = "Request stock" }, Status = ProductionStatus.InProgress };
        db.AddRange(style, step); await db.SaveChangesAsync();
        var before = await f.Repository.CheckPackageMaterialStockLevelsForProductionSchedule(f.Batch.Id, null);
        Assert.True(before.IsSuccess); Assert.Empty(before.Value);
        db.ProductPackages.Add(new ProductPackage { Id = Guid.NewGuid(), ProductId = f.Batch.ProductId, ProductPackingId = style.Id,
            MaterialId = f.Lot.MaterialId, PrescribedQuantity = 30, Loose = 2 }); await db.SaveChangesAsync();
        var after = await f.Repository.CheckPackageMaterialStockLevelsForProductionSchedule(f.Batch.Id, null);
        Assert.True(after.IsSuccess, after.Error.Description); Assert.Equal(32, Assert.Single(after.Value).QuantityNeeded);
        Assert.Empty(await db.ProductionMaterialConfigurations.ToListAsync()); Assert.Empty(await db.MaterialBatchReservedQuantities.ToListAsync());
        var items = new List<CreateRequisitionItemRequest> { new() { MaterialId = f.Lot.MaterialId, UomId = f.Lot.UoMId, Quantity = 32 } };
        Assert.True((await ProductionPackagingFirstRequest.Prepare(db, f.Batch.Id, step.Id, f.Actor.Id, items)).IsFailure);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanUpdateProductionStep);
        var storeEvidence = await db.Warehouses.IgnoreQueryFilters().IgnoreAutoIncludes().Where(w => w.DepartmentId == f.Department.Id)
            .Select(w => new { w.Name, w.Type, w.DeletedAt }).ToListAsync();
        Assert.Equal(1, storeEvidence.Count(w => w.DeletedAt == null && w.Type == WarehouseType.PackagedStorage));
        Assert.Equal(1, storeEvidence.Count(w => w.DeletedAt == null && w.Type == WarehouseType.Production));
        var prepared = await ProductionPackagingFirstRequest.Prepare(db, f.Batch.Id, step.Id, f.Actor.Id, items);
        Assert.True(prepared.IsSuccess, prepared.Error.Description);
        await db.SaveChangesAsync();
        var adopted = await db.ProductionMaterialConfigurations.SingleAsync(); Assert.Equal(MaterialKind.Package, adopted.Kind);
        Assert.Equal(32, (await db.MaterialBatchReservedQuantities.SingleAsync()).Quantity);
        (await db.ProductPackages.IgnoreQueryFilters().IgnoreAutoIncludes().SingleAsync()).PrescribedQuantity = 99; await db.SaveChangesAsync();
        var pinned = await f.Repository.CheckPackageMaterialStockLevelsForProductionSchedule(f.Batch.Id, null);
        Assert.Equal(32, Assert.Single(pinned.Value).QuantityNeeded);
    }
    [Theory]
    [InlineData("Raw", "CanRejectRawMaterialStockRequisition")]
    [InlineData("Packaging", "CanRejectPackagingMaterialStockRequisition")]
    public void New_variant_endpoints_name_one_specific_policy(string action, string key)
    {
        var method = typeof(ProductionStockRequisitionController).GetMethod(action);
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }
    [Fact]
    public async Task Stock_level_save_requires_the_counting_unit_action_to_change_the_saved_unit()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        var litre = new DOMAIN.Entities.Base.UnitOfMeasure { Id = Guid.NewGuid(), Name = "Litre", Symbol = "l" };
        db.UnitOfMeasures.Add(litre); await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanLinkRawMaterials);
        var result = await new MaterialRepository(db, f.Mapper).CreateMaterialDepartment([new() { MaterialId = f.Lot.MaterialId, UoMId = litre.Id }], f.Actor.Id);
        Assert.Equal("MaterialDepartment.CountingUnit", result.Error.Code);
        Assert.Equal(f.Lot.UoMId, (await db.MaterialDepartments.SingleAsync()).UoMId);
    }
}
