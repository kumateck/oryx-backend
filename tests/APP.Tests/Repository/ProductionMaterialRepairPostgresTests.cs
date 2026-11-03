using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Real transactions, filters, retained approval cycles and independent-session contention.</summary>
public class ProductionMaterialRepairPostgresTests
{
    internal const string Variable = "ORYX_MATERIAL_REPAIR_TEST_CONNECTION";
    private static readonly Guid Actor = Guid.Parse("f1000000-0000-0000-0000-000000000001");
    private static readonly Guid DepartmentId = Guid.Parse("f1000000-0000-0000-0000-000000000002");
    private static readonly Guid BatchId = Guid.Parse("f1000000-0000-0000-0000-000000000003");
    private static readonly Guid ActivityId = Guid.Parse("f1000000-0000-0000-0000-000000000004");
    private static readonly Guid MaterialId = Guid.Parse("f1000000-0000-0000-0000-000000000005");
    private static readonly Guid RawBatch = Guid.Parse("f1000000-0000-0000-0000-000000000006");
    private static readonly Guid RequisitionId = Guid.Parse("f1000000-0000-0000-0000-000000000007");
    private static readonly Guid UnitId = Guid.Parse("f1000000-0000-0000-0000-000000000008");

    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => Actor; public Guid? DepartmentId => ProductionMaterialRepairPostgresTests.DepartmentId; public string DepartmentType => "Production"; }

    private static ApplicationDbContext Context()
    {
        var connection = Environment.GetEnvironmentVariable(Variable) ?? throw new InvalidOperationException("A disposable test connection is required.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost" or "/tmp") || parsed.Database != "oryx_material_repair_test")
            throw new InvalidOperationException("Only the dedicated local material-repair fixture is permitted.");
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new CurrentUser());
    }

    private static ApprovalRepository Approvals(ApplicationDbContext context) => new(context, null!, null!, null!,
        NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());

    private static async Task Seed()
    {
        await using var db = Context();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await db.Database.OpenConnectionAsync();
        // Omitted, unrelated ERP graph edges are tolerated only while seeding this disposable slice.
        // Constraints are ON for every operation and assertion under test.
        await db.Database.ExecuteSqlRawAsync("SET session_replication_role = replica");
        var role = Guid.NewGuid();
        db.Departments.Add(new Department { Id = DepartmentId, Name = "Production", Type = DepartmentType.Production });
        db.Users.Add(new User { Id = Actor, UserName = "repair-operator", FirstName = "Repair", LastName = "Operator", DepartmentId = DepartmentId });
        db.Roles.Add(new Role { Id = role, Name = "Repair operator" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor, RoleId = role });
        foreach (var (key, id) in new[] { (PermissionKeys.CanRepairBom, 701), (PermissionKeys.CanRepairPackaging, 702) })
        {
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = id, Type = "assigned" });
        }
        db.UnitOfMeasures.Add(new UnitOfMeasure { Id = UnitId, Name = "Milligram", Symbol = "mg" });
        var product = new Product { Id = Guid.NewGuid(), Name = "Repair product", DepartmentId = DepartmentId, FullBatchSize = 100, BaseUomId = UnitId };
        db.Products.Add(product);
        var style = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Selected style" };
        db.ProductPackings.Add(style);
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "TEST", DepartmentId = DepartmentId, Status = ProductionStatus.InProgress };
        db.ProductionSchedules.Add(schedule);
        db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = BatchId, ProductId = product.Id,
            ProductionScheduleId = schedule.Id, ProductPackingId = style.Id, BatchSize = BatchSize.Full, Quantity = 100 });
        db.ProductionActivities.Add(new ProductionActivity { Id = ActivityId, ProductionScheduleProductId = BatchId, Code = "TEST-RUN", Status = ProductionStatus.InProgress });
        var raw = new Material { Id = MaterialId, Name = "Raw", Code = "RM-TEST", Kind = MaterialKind.Raw };
        var pack = new Material { Id = Guid.NewGuid(), Name = "Packaging", Code = "PM-TEST", Kind = MaterialKind.Package };
        db.Materials.AddRange(raw, pack);
        foreach (var material in new[] { raw, pack })
            db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = DepartmentId, UoMId = UnitId });
        var bom = new BillOfMaterial { Id = Guid.NewGuid(), ProductId = product.Id, IsActive = true,
            Items = [new BillOfMaterialItem { MaterialId = MaterialId, BaseUoMId = UnitId, PrescribedQuantity = 20, BaseQuantity = 1, Order = 1 }] };
        db.BillOfMaterials.Add(bom);
        db.ProductBillOfMaterials.Add(new ProductBillOfMaterial { ProductId = product.Id, BillOfMaterialId = bom.Id, IsActive = true, EffectiveDate = DateTime.UtcNow.Date });
        db.ProductPackages.Add(new ProductPackage { ProductId = product.Id, ProductPackingId = style.Id, MaterialId = pack.Id, PrescribedQuantity = 30, Loose = 2, PackingExcessMargin = 2 });
        var floor = new Warehouse { Id = Guid.NewGuid(), Name = "Production", DepartmentId = DepartmentId, Type = WarehouseType.Production };
        db.Warehouses.Add(floor);
        foreach (var material in new[] { raw, pack })
        {
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = material.Name, DepartmentId = DepartmentId,
                Type = material.Kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage };
            var location = new WarehouseLocation { Id = Guid.NewGuid(), WarehouseId = warehouse.Id, Name = "Location" };
            var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocationId = location.Id, Name = "Rack" };
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRackId = rack.Id, Name = "Shelf" };
            var physical = new MaterialBatch { Id = material.Kind == MaterialKind.Raw ? RawBatch : Guid.NewGuid(), MaterialId = material.Id,
                BatchNumber = "LOT", UoMId = UnitId, TotalQuantity = 100, Status = BatchStatus.Available, ExpiryDate = DateTime.UtcNow.AddYears(1) };
            db.Warehouses.Add(warehouse); db.WarehouseLocations.Add(location); db.WarehouseLocationRacks.Add(rack); db.WarehouseLocationShelves.Add(shelf);
            db.MaterialBatches.Add(physical);
            db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { MaterialBatchId = physical.Id, WarehouseLocationShelfId = shelf.Id, UoMId = UnitId, Quantity = 100 });
            db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { MaterialBatchId = physical.Id, WarehouseLocationShelfId = shelf.Id,
                ProductionScheduleProductId = BatchId, WarehouseId = floor.Id, UoMId = UnitId, Quantity = material.Kind == MaterialKind.Raw ? 10 : 5 });
        }
        var approval = new Approval { Id = Guid.NewGuid(), ItemType = "StockRequisition", ApprovalStages = [] };
        db.Approvals.Add(approval);
        db.ApprovalStages.Add(new ApprovalStage { Id = Guid.NewGuid(), ApprovalId = approval.Id, UserId = Actor, Required = true, Order = 1 });
        db.Requisitions.Add(new Requisition { Id = RequisitionId, Code = "RM/N/26/001", DepartmentId = DepartmentId,
            RequestedById = Actor, ProductionScheduleProductId = BatchId, RequisitionType = RequisitionType.Stock, Status = RequestStatus.Pending, Approved = true,
            Items = [new RequisitionItem { MaterialId = MaterialId, UoMId = UnitId, Quantity = 10, Status = RequestStatus.Pending }],
            Approvals = [new RequisitionApproval { Id = Guid.NewGuid(), ApprovalId = approval.Id, UserId = Actor, Required = true, Order = 1,
                Status = ApprovalStatus.Approved, ApprovedById = Actor, ApprovalTime = DateTime.UtcNow }] });
        await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT");
    }

    private static async Task<ProductionMaterialRepairPreview> Preview(ApplicationDbContext db, MaterialKind kind = MaterialKind.Raw)
    {
        var result = await ProductionMaterialRepairs.Preview(db, ActivityId, Actor, kind);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        return result.Value;
    }
    private static ApplyProductionMaterialRepair Request(ProductionMaterialRepairPreview preview) => new()
    { RequestId = Guid.NewGuid(), PreviewHash = preview.PreviewHash, Reason = "Correct saved product material configuration" };

    [MaterialRepairFact]
    public async Task Preview_is_read_only_and_apply_reconciles_stock_document_and_approval_atomically()
    {
        await Seed();
        ApplyProductionMaterialRepair request;
        await using (var db = Context())
        {
            var preview = await Preview(db);
            Assert.Empty(preview.Blockers);
            Assert.Equal(20, Assert.Single(preview.Lines).After);
            Assert.Empty(await db.ProductionMaterialRepairs.ToListAsync());
            Assert.Equal(10, await db.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
            request = Request(preview) with { Reason = new string('R', 2000) };
            var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, request);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        }
        await using var check = Context();
        Assert.Equal(20, await check.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
        Assert.Equal(100, await check.ShelfMaterialBatches.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
        var document = await check.Requisitions.Include(item => item.Items).Include(item => item.Approvals).SingleAsync();
        Assert.False(document.Approved); Assert.Equal(1, document.ContentRevision);
        Assert.Equal(20, Assert.Single(document.Items).Quantity);
        Assert.Equal(ApprovalStatus.Pending, Assert.Single(document.Approvals).Status);
        var history = await check.RequisitionApprovals.IgnoreQueryFilters().ToListAsync();
        Assert.Equal(2, history.Count); Assert.Contains(history, item => item.ContentRevision == 0 && item.Status == ApprovalStatus.Approved);
        Assert.Single(await check.ProductionMaterialRepairs.ToListAsync());
        Assert.Single(await check.ProductionActivityLogs.ToListAsync());
        Assert.Equal(2000, (await check.ProductionMaterialRepairs.SingleAsync()).Reason.Length);
        Assert.True((await check.ProductionActivityLogs.SingleAsync()).Message.Length <= 1000);
        Assert.Empty(await check.BinCardInformation.ToListAsync());
        var again = await ProductionMaterialRepairs.Apply(check, Approvals(check), ActivityId, Actor, MaterialKind.Raw, request);
        Assert.True(again.IsSuccess);
        Assert.Single(await check.ProductionMaterialRepairs.ToListAsync());
        var staleApproval = await Approvals(check).ApproveItem("StockRequisition", RequisitionId, Actor, [], contentRevision: 0);
        Assert.Equal("Approval.StaleRevision", staleApproval.Error.Code);
        await using var fresh = Context();
        var unchanged = await Preview(fresh);
        Assert.False(unchanged.HasChanges);
        var noOp = await ProductionMaterialRepairs.Apply(fresh, Approvals(fresh), ActivityId, Actor, MaterialKind.Raw, Request(unchanged));
        Assert.True(noOp.IsSuccess); Assert.False(noOp.Value.Changed);
        Assert.Single(await fresh.ProductionMaterialRepairs.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Product_edit_after_preview_is_refused_without_changing_operational_data()
    {
        await Seed();
        ProductionMaterialRepairPreview preview;
        await using (var read = Context()) preview = await Preview(read);
        await using (var edit = Context()) { var line = await edit.BillOfMaterialItems.SingleAsync(); line.PrescribedQuantity = 25; await edit.SaveChangesAsync(); }
        await using var apply = Context();
        var result = await ProductionMaterialRepairs.Apply(apply, Approvals(apply), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.Equal("Repair.StalePreview", result.Error.Code);
        Assert.Empty(await apply.ProductionMaterialRepairs.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Shortage_excludes_other_batches_reservations_and_rolls_back_all_changes()
    {
        await Seed();
        await using (var seed = Context())
        {
            var row = await seed.MaterialBatchReservedQuantities.SingleAsync(item => item.MaterialBatchId == RawBatch);
            await seed.Database.OpenConnectionAsync(); await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = replica");
            var otherBatch = Guid.NewGuid();
            seed.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { MaterialBatchId = RawBatch, ProductionScheduleProductId = otherBatch,
                WarehouseId = row.WarehouseId, WarehouseLocationShelfId = row.WarehouseLocationShelfId, UoMId = UnitId, Quantity = 90 });
            // Receiving a purchase requisition is not a warehouse issue of this batch's hold.
            seed.Requisitions.Add(new Requisition { Code = "PURCHASE-TEST", DepartmentId = DepartmentId, RequestedById = Actor,
                ProductionScheduleProductId = otherBatch, RequisitionType = RequisitionType.Purchase, Status = RequestStatus.Completed,
                Items = [new RequisitionItem { MaterialId = MaterialId, UoMId = UnitId, Quantity = 90, Status = RequestStatus.Completed }] });
            await seed.SaveChangesAsync(); await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT");
        }
        await using var db = Context(); var preview = await Preview(db);
        Assert.Equal(10, Assert.Single(preview.Lines).Shortage);
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.Equal("Repair.Blocked", result.Error.Code);
        Assert.Empty(await db.ProductionMaterialConfigurations.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Issued_and_consumed_material_is_refused_even_with_a_pending_document_header()
    {
        await Seed();
        await using (var edit = Context())
        {
            var item = await edit.RequisitionItems.SingleAsync(); item.Status = RequestStatus.Completed;
            await edit.SaveChangesAsync();
        }
        await using (var db = Context()) Assert.Contains((await Preview(db)).Blockers, text => text.Contains("already been issued"));
        await using (var edit = Context())
        {
            var row = await edit.MaterialBatchReservedQuantities.SingleAsync(item => item.MaterialBatchId == RawBatch);
            edit.MaterialBatchReservedQuantities.Remove(row); await edit.SaveChangesAsync();
        }
        await using var consumed = Context();
        Assert.Contains((await Preview(consumed)).Blockers, text => text.Contains("consumed or returned"));
    }

    [MaterialRepairFact]
    public async Task Released_allocations_are_not_consumed_and_applied_requirements_survive_later_master_edits()
    {
        await Seed();
        await using (var edit = Context())
        {
            var line = await edit.BillOfMaterialItems.SingleAsync(); line.PrescribedQuantity = 5; await edit.SaveChangesAsync();
        }
        await using (var db = Context())
        {
            var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(await Preview(db)));
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        }
        await using (var edit = Context()) { var line = await edit.BillOfMaterialItems.SingleAsync(); line.PrescribedQuantity = 99; await edit.SaveChangesAsync(); }
        await using var check = Context();
        var configuration = await check.ProductionMaterialConfigurations.SingleAsync();
        Assert.Equal(5, Assert.Single(ProductionMaterialSources.Requirements(configuration.RequirementsJson)).Quantity);
        Assert.Empty(await check.MaterialBatchReservedQuantities.IgnoreQueryFilters().Where(item => item.DeletedAt != null && item.ReleasedAt == null).ToListAsync());
        Assert.Equal(5, await check.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
    }

    [MaterialRepairFact]
    public async Task Packaging_repair_is_independent_of_issued_raw_material_and_counts_allowance_once()
    {
        await Seed();
        await using (var edit = Context()) { var req = await edit.Requisitions.SingleAsync(); req.Status = RequestStatus.Completed; await edit.SaveChangesAsync(); }
        await using var db = Context(); var preview = await Preview(db, MaterialKind.Package);
        Assert.Empty(preview.Blockers); Assert.Equal(32, Assert.Single(preview.Lines).After);
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Package, Request(preview));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(10, await db.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
    }

    [MaterialRepairFact]
    public async Task A_revoked_grant_between_preview_and_apply_denies_the_write()
    {
        await Seed(); ProductionMaterialRepairPreview preview;
        await using (var read = Context()) preview = await Preview(read);
        await using (var revoke = Context()) { revoke.PermissionTypes.RemoveRange(await revoke.PermissionTypes.ToListAsync()); await revoke.SaveChangesAsync(); }
        await using var db = Context();
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.Equal("Repair.Permission", result.Error.Code); Assert.Empty(await db.ProductionMaterialRepairs.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Removing_a_material_releases_its_allocation_without_recording_consumption()
    {
        await Seed();
        var replacementId = Guid.NewGuid();
        await using (var edit = Context())
        {
            var line = await edit.BillOfMaterialItems.SingleAsync(); line.MaterialId = replacementId;
            var shelf = await edit.ShelfMaterialBatches.SingleAsync(item => item.MaterialBatchId == RawBatch);
            var replacement = new Material { Id = replacementId, Name = "Correct replacement", Code = "RM-NEW", Kind = MaterialKind.Raw };
            edit.Materials.Add(replacement);
            edit.MaterialDepartments.Add(new MaterialDepartment { MaterialId = replacementId, DepartmentId = DepartmentId, UoMId = UnitId });
            var physical = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = replacementId, UoMId = UnitId, Status = BatchStatus.Available,
                TotalQuantity = 100, BatchNumber = "NEW", ExpiryDate = DateTime.UtcNow.AddYears(1) };
            edit.MaterialBatches.Add(physical);
            edit.ShelfMaterialBatches.Add(new ShelfMaterialBatch { MaterialBatchId = physical.Id, WarehouseLocationShelfId = shelf.WarehouseLocationShelfId, UoMId = UnitId, Quantity = 100 });
            await edit.SaveChangesAsync();
        }
        await using (var db = Context())
        {
            var preview = await Preview(db); Assert.Empty(preview.Blockers);
            var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        }
        await using var check = Context();
        var released = await check.MaterialBatchReservedQuantities.IgnoreQueryFilters().SingleAsync(item => item.MaterialBatchId == RawBatch);
        Assert.NotNull(released.ReleasedAt); Assert.Null(released.DeletedAt);
        Assert.Empty(await check.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).ToListAsync());
        Assert.Empty(await check.MaterialBatchEvents.Where(item => item.Type == EventType.Consumed).ToListAsync());
        Assert.Equal(replacementId, (await check.RequisitionItems.SingleAsync()).MaterialId);
        Assert.NotNull((await check.RequisitionItems.IgnoreQueryFilters().SingleAsync(item => item.MaterialId == MaterialId)).RetiredByRepairId);
        var materialRepository = new MaterialRepository(check, null!);
        var currentReservations = await materialRepository.GetReservedQuantitiesForMaterial(MaterialId, DepartmentId);
        Assert.True(currentReservations.IsSuccess); Assert.Empty(currentReservations.Value);
        var reports = new ReportRepository(check, null!, materialRepository, NullLogger<ReportRepository>.Instance);
        var trace = await reports.GetBatchTraceability(RawBatch, new ReportFilter(), DepartmentId);
        Assert.True(trace.IsSuccess);
        Assert.Contains(trace.Value, item => item.EventType == "Reservation released" && item.Quantity == 10);
        Assert.DoesNotContain(trace.Value, item => item.EventType == "Consumed");
        Assert.Empty((await Preview(check)).Blockers);
    }

    [MaterialRepairFact]
    public async Task Half_batch_packaging_keeps_the_full_allowance_and_ignores_other_packing_styles()
    {
        await Seed();
        await using (var edit = Context())
        {
            var batch = await edit.ProductionScheduleProducts.SingleAsync(); batch.BatchSize = BatchSize.Half;
            var current = await edit.ProductPackages.SingleAsync();
            var other = new ProductPacking { Id = Guid.NewGuid(), ProductId = batch.ProductId, Name = "Other style" };
            edit.ProductPackings.Add(other);
            edit.ProductPackages.Add(new ProductPackage { ProductId = batch.ProductId, ProductPackingId = other.Id, MaterialId = current.MaterialId, PrescribedQuantity = 9999 });
            await edit.SaveChangesAsync();
        }
        await using var db = Context();
        var preview = await Preview(db, MaterialKind.Package);
        Assert.Empty(preview.Blockers); Assert.Equal(17, Assert.Single(preview.Lines).After);
    }

    [MaterialRepairFact]
    public async Task A_valid_requisition_substitute_is_preserved_when_its_reservation_is_missing()
    {
        await Seed();
        var substituteId = Guid.NewGuid();
        await using (var edit = Context())
        {
            var line = await edit.BillOfMaterialItems.SingleAsync(); line.IsSubstitutable = true;
            var shelf = await edit.ShelfMaterialBatches.SingleAsync(item => item.MaterialBatchId == RawBatch);
            edit.Materials.Add(new Material { Id = substituteId, Kind = MaterialKind.Raw, Name = "Selected substitute", Code = "RM-SUB" });
            edit.MaterialDepartments.Add(new MaterialDepartment { MaterialId = substituteId, DepartmentId = DepartmentId, UoMId = UnitId });
            edit.BillOfMaterialItemSubstitutes.Add(new BillOfMaterialItemSubstitute { BillOfMaterialItemId = line.Id, SubstituteMaterialId = substituteId });
            var physical = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = substituteId, UoMId = UnitId, BatchNumber = "SUB-LOT",
                Status = BatchStatus.Available, TotalQuantity = 100, ExpiryDate = DateTime.UtcNow.AddYears(1) };
            edit.MaterialBatches.Add(physical);
            edit.ShelfMaterialBatches.Add(new ShelfMaterialBatch { MaterialBatchId = physical.Id, WarehouseLocationShelfId = shelf.WarehouseLocationShelfId, UoMId = UnitId, Quantity = 100 });
            (await edit.RequisitionItems.SingleAsync()).MaterialId = substituteId;
            await edit.SaveChangesAsync();
            // Model the legacy missing-allocation defect, without creating consumed history.
            await edit.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).ExecuteDeleteAsync();
        }
        await using var db = Context(); var preview = await Preview(db);
        Assert.Empty(preview.Blockers); Assert.Equal(substituteId, Assert.Single(preview.Lines).MaterialId);
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var configuration = await db.ProductionMaterialConfigurations.SingleAsync();
        var requirement = Assert.Single(ProductionMaterialSources.Requirements(configuration.RequirementsJson));
        Assert.Equal(MaterialId, requirement.SourceMaterialId); Assert.Equal(substituteId, requirement.MaterialId);
        Assert.Equal(20, requirement.Quantity);
    }

    [MaterialRepairFact]
    public async Task Repair_is_denied_after_the_operator_moves_to_another_department()
    {
        await Seed();
        ProductionMaterialRepairPreview preview;
        await using (var read = Context()) preview = await Preview(read);
        await using (var edit = Context())
        {
            var other = new Department { Id = Guid.NewGuid(), Name = "Other production", Type = DepartmentType.Production };
            edit.Departments.Add(other);
            (await edit.Users.SingleAsync(item => item.Id == Actor)).DepartmentId = other.Id;
            await edit.SaveChangesAsync();
        }
        await using var db = Context();
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.Equal("Repair.Department", result.Error.Code);
        Assert.Empty(await db.ProductionMaterialRepairs.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Production_notifications_are_deferred_until_commit_and_discarded_on_rollback()
    {
        await Seed();
        await using var db = Context();
        var published = false;
        await using (var scope = await ProductionMaterialTransaction.Begin(db))
        {
            Assert.True(ProductionMaterialTransaction.DeferUntilCommit(db, () => { published = true; return Task.CompletedTask; }));
            Assert.False(published);
            await scope.CommitAsync();
            Assert.True(published);
        }
        published = false;
        await using (var rollback = await ProductionMaterialTransaction.Begin(db))
            Assert.True(ProductionMaterialTransaction.DeferUntilCommit(db, () => { published = true; return Task.CompletedTask; }));
        Assert.False(published);
    }

    [MaterialRepairFact]
    public async Task A_failed_audit_insert_rolls_back_allocations_configuration_and_reapproval()
    {
        await Seed();
        await using var db = Context(); var preview = await Preview(db);
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"ProductionMaterialRepairs\" ADD CONSTRAINT \"FailRepairAudit\" CHECK (false)");
        var result = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.True(result.IsFailure);
        await using var check = Context();
        Assert.Equal(10, await check.MaterialBatchReservedQuantities.Where(item => item.MaterialBatchId == RawBatch).Select(item => item.Quantity).SingleAsync());
        Assert.Empty(await check.ProductionMaterialConfigurations.ToListAsync());
        Assert.True((await check.Requisitions.SingleAsync()).Approved);
        Assert.Single(await check.RequisitionApprovals.IgnoreQueryFilters().ToListAsync());
    }

    [MaterialRepairFact]
    public async Task Open_purchase_work_blocks_repair_and_new_supply_requests_respect_the_adopted_quantity()
    {
        await Seed();
        var purchaseId = Guid.NewGuid();
        await using (var edit = Context())
        {
            edit.Requisitions.Add(new Requisition { Id = purchaseId, Code = "PR-OPEN", RequestedById = Actor, DepartmentId = DepartmentId,
                RequisitionType = RequisitionType.Purchase, ProductionScheduleProductId = BatchId, Status = RequestStatus.New,
                Items = [new RequisitionItem { MaterialId = MaterialId, UoMId = UnitId, Quantity = 20, Status = RequestStatus.Pending }] });
            await edit.SaveChangesAsync();
        }
        await using (var read = Context()) Assert.Contains((await Preview(read)).Blockers, item => item.Contains("purchase requisition"));
        await using (var close = Context())
        {
            var purchase = await close.Requisitions.Include(item => item.Items).SingleAsync(item => item.Id == purchaseId);
            purchase.Status = RequestStatus.Completed; foreach (var item in purchase.Items) item.Status = RequestStatus.Completed;
            await close.SaveChangesAsync();
        }
        await using var db = Context(); var preview = await Preview(db); Assert.Empty(preview.Blockers);
        var repaired = await ProductionMaterialRepairs.Apply(db, Approvals(db), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        Assert.True(repaired.IsSuccess, repaired.IsFailure ? repaired.Error.Description : null);
        var stale = await ProductionMaterialRequisitionRules.ValidateProcurement(db, BatchId,
            [new CreateRequisitionItemRequest { MaterialId = MaterialId, UomId = UnitId, Quantity = 21 }]);
        Assert.Equal("Procurement.StaleConfiguration", stale.Error.Code);
        var current = await ProductionMaterialRequisitionRules.ValidateProcurement(db, BatchId,
            [new CreateRequisitionItemRequest { MaterialId = MaterialId, UomId = UnitId, Quantity = 20 }]);
        Assert.True(current.IsSuccess);
    }

    [MaterialRepairFact]
    public async Task Stock_swaps_require_the_complete_current_allocation_and_cannot_retarget_issued_stock()
    {
        await Seed();
        await using (var apply = Context())
        {
            var result = await ProductionMaterialRepairs.Apply(apply, Approvals(apply), ActivityId, Actor, MaterialKind.Raw, Request(await Preview(apply)));
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        }
        await using var db = Context();
        var first = await db.ShelfMaterialBatches.IgnoreQueryFilters().Include(item => item.WarehouseLocationShelf)
            .ThenInclude(item => item.WarehouseLocationRack).ThenInclude(item => item.WarehouseLocation)
            .SingleAsync(item => item.MaterialBatchId == RawBatch);
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Other production", Type = DepartmentType.Production };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), DepartmentId = otherDepartment.Id, Type = WarehouseType.RawMaterialStorage, Name = "Replacement" };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), WarehouseId = warehouse.Id, Name = "Location" };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocationId = location.Id, Name = "Rack" };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRackId = rack.Id, Name = "Shelf" };
        var physical = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = MaterialId, UoMId = UnitId, BatchNumber = "REPLACEMENT",
            Status = BatchStatus.Available, TotalQuantity = 100, ExpiryDate = DateTime.UtcNow.AddYears(1) };
        var second = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = physical.Id, UoMId = UnitId, WarehouseLocationShelfId = shelf.Id, Quantity = 100 };
        db.Departments.Add(otherDepartment); db.Warehouses.Add(warehouse); db.WarehouseLocations.Add(location);
        db.WarehouseLocationRacks.Add(rack); db.WarehouseLocationShelves.Add(shelf); db.MaterialBatches.Add(physical); db.ShelfMaterialBatches.Add(second);
        await db.SaveChangesAsync();
        var swap = new CreateSwapRequest { StockRequisitionId = RequisitionId,
            FirstWarehouseId = first.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId, SecondWarehouseId = warehouse.Id,
            FirstSwapShelfMaterialBatches = [new CreateSwapShelfMaterialBatch { ShelfMaterialBatchId = first.Id, MaterialBatchId = RawBatch, UoMId = UnitId, Quantity = 10 }],
            SecondSwapShelfMaterialBatches = [new CreateSwapShelfMaterialBatch { ShelfMaterialBatchId = second.Id, MaterialBatchId = physical.Id, UoMId = UnitId, Quantity = 10 }] };
        var stale = await ProductionMaterialSwapRules.Validate(db, swap);
        Assert.Equal("Swap.StaleAllocation", stale.Error.Code);
        swap.FirstSwapShelfMaterialBatches[0].Quantity = 20; swap.SecondSwapShelfMaterialBatches[0].Quantity = 20;
        var current = await ProductionMaterialSwapRules.Validate(db, swap); Assert.True(current.IsSuccess, current.IsFailure ? current.Error.Description : null);
        (await db.Requisitions.SingleAsync()).Status = RequestStatus.Completed; await db.SaveChangesAsync();
        var issued = await ProductionMaterialSwapRules.Validate(db, swap); Assert.Equal("Swap.Issued", issued.Error.Code);
        Assert.Equal(100, first.Quantity); Assert.Equal(100, second.Quantity);
        Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync());
    }

    [MaterialRepairFact]
    public async Task An_issue_committed_while_repair_waits_invalidates_the_preview()
    {
        await Seed(); ProductionMaterialRepairPreview preview;
        await using (var read = Context()) preview = await Preview(read);
        await using var issuing = Context();
        await using var issue = await ProductionMaterialTransaction.Begin(issuing);
        await using var repair = Context();
        var task = ProductionMaterialRepairs.Apply(repair, Approvals(repair), ActivityId, Actor, MaterialKind.Raw, Request(preview));
        var request = await issuing.Requisitions.SingleAsync(); request.Status = RequestStatus.Completed; await issuing.SaveChangesAsync();
        await issue.CommitAsync();
        var result = await task;
        Assert.Equal("Repair.StalePreview", result.Error.Code);
        Assert.Empty(await repair.ProductionMaterialRepairs.ToListAsync());
    }
}

public sealed class MaterialRepairFactAttribute : FactAttribute
{
    public MaterialRepairFactAttribute()
    { if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ProductionMaterialRepairPostgresTests.Variable))) Skip = "Set the dedicated local disposable material-repair fixture connection."; }
}
