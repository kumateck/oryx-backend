using APP.Repository;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

internal sealed class RepairPostgresFactAttribute : FactAttribute
{
    public RepairPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION")))
            Skip = "Set ORYX_ISSUE_REPAIR_TEST_CONNECTION to an isolated local PostgreSQL server (CREATE DATABASE required).";
    }
}

internal sealed class RepairActor : ICurrentUserService
{
    public Guid? UserId { get; } = Guid.NewGuid();
    public Guid? DepartmentId { get; } = Guid.NewGuid();
    public string DepartmentType => nameof(SHARED.DepartmentType.Production);
}

internal sealed class IssuedMaterialRepairFixture : IAsyncDisposable
{
    public RepairActor Actor { get; } = new();
    public string Database { get; } = "oryx_issue_repair_test_" + Guid.NewGuid().ToString("N");
    public string Connection { get; private set; } = "";
    public Guid BatchId { get; } = Guid.NewGuid();
    public Guid ProductId { get; } = Guid.NewGuid();
    public Guid RequisitionId { get; } = Guid.NewGuid();
    public Guid RoleId { get; } = Guid.NewGuid();
    public Guid UnitId { get; } = Guid.NewGuid();
    public Guid SourceId { get; private set; }
    public IssuedMaterialRepairKind Kind { get; private set; }
    private NpgsqlConnection? admin;
    public ApplicationDbContext Open() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Connection).Options, Actor);
    public IssuedMaterialRepairService Service(ApplicationDbContext context) => new(context, Actor);

    public async Task Seed(IssuedMaterialRepairKind kind = IssuedMaterialRepairKind.Packaging, decimal stock = 200_000_000, bool issueStock = true)
    {
        Kind = kind;
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION"));
        // Never allow these fixtures to use a remote customer server.
        Assert.Contains(builder.Host, new[] { "localhost", "127.0.0.1", "::1", "/tmp" });
        admin = new NpgsqlConnection(builder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{Database}\"", admin)) await create.ExecuteNonQueryAsync();
        builder.Database = Database;
        Connection = builder.ConnectionString;
        await using var db = Open();
        await db.Database.EnsureCreatedAsync();
        // Exercise the exact deployment migration against the isolated baseline, including its
        // foreign keys, evidence columns and database append-only triggers.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"IssuedMaterialRepairAllocations\", \"IssuedMaterialRepairLines\", \"IssuedMaterialRepairs\"");
        var migration = new INFRASTRUCTURE.Migrations.AddIssuedMaterialRepairs();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.Departments.Add(new Department { Id = Actor.DepartmentId.GetValueOrDefault(), Name = "Synthetic Beta Lactam", Type = DepartmentType.Production });
        db.Users.Add(new User { Id = Actor.UserId.GetValueOrDefault(), DepartmentId = Actor.DepartmentId, UserName = "repair-test" });
        db.Roles.Add(new Role { Id = RoleId, Name = "Synthetic repair operator" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.GetValueOrDefault(), RoleId = RoleId });
        var permissions = new[] { PermissionKeys.CanViewPlannedProducts, PermissionKeys.CanRepairIssuedBom, PermissionKeys.CanRepairIssuedPackaging };
        for (var i = 0; i < permissions.Length; i++)
        {
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = i + 1, RoleId = RoleId, ClaimType = AppConstants.Permission, ClaimValue = permissions[i] });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = i + 1, Key = permissions[i], Type = "Action" });
        }
        db.UnitOfMeasures.Add(new UnitOfMeasure { Id = UnitId, Name = "Milligram", Symbol = "mg" });
        var raw = new Warehouse { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId, Name = "Synthetic raw store", Type = WarehouseType.RawMaterialStorage };
        var packing = new Warehouse { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId, Name = "Synthetic packing store", Type = WarehouseType.PackagedStorage };
        var production = new Warehouse { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId, Name = "Synthetic production", Type = WarehouseType.Production };
        db.Warehouses.AddRange(raw, packing, production);
        var source = kind == IssuedMaterialRepairKind.Packaging ? packing : raw;
        SourceId = source.Id;
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf A", Code = "SYNTHETIC/A", WarehouseLocationRack = new WarehouseLocationRack
        { Id = Guid.NewGuid(), Name = "Rack", WarehouseLocation = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Room", Warehouse = source } } };
        db.WarehouseLocationShelves.Add(shelf);
        var product = new Product { Id = ProductId, Code = "BFC-04", Name = "Amoxicillin 500mg Capsule-BF (synthetic fixture)",
            DepartmentId = Actor.DepartmentId, BaseUomId = UnitId, FullBatchSize = 311_250_000 };
        db.Products.Add(product);
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "SYNTHETIC/PS001", DepartmentId = Actor.DepartmentId, Status = ProductionStatus.InProgress };
        db.ProductionSchedules.Add(schedule);
        db.ProductionScheduleProducts.Add(new() { Id = BatchId, ProductId = ProductId, ProductionSchedule = schedule,
            BatchNumber = "BFC4K338-SYNTHETIC", Quantity = 311_250_000, BatchSize = BatchSize.Full });
        db.ProductionActivities.Add(new() { Id = Guid.NewGuid(), Code = "Synthetic run", ProductionScheduleProductId = BatchId,
            Status = ProductionStatus.InProgress, Steps = [new() { Id = Guid.NewGuid(), Order = 1, Operation = new Operation { Name = "Dispensing" }, Status = ProductionStatus.InProgress },
                new() { Id = Guid.NewGuid(), Order = 2, Operation = new Operation { Name = kind == IssuedMaterialRepairKind.Bom ? "Product Preparation" : "Final Packing" }, Status = ProductionStatus.New }] });
        var req = new Requisition { Id = RequisitionId, Code = kind == IssuedMaterialRepairKind.Bom ? "RM/SYNTHETIC/001" : "PM/SYNTHETIC/001",
            DepartmentId = Actor.DepartmentId.GetValueOrDefault(), ProductionScheduleProductId = BatchId,
            RequestedById = Actor.UserId.GetValueOrDefault(), RequisitionType = RequisitionType.Stock,
            Approved = true, Status = RequestStatus.Pending, Items = [] };
        var bom = new BillOfMaterial { Id = Guid.NewGuid(), ProductId = ProductId, Version = 1, IsActive = true };
        if (kind == IssuedMaterialRepairKind.Bom)
            db.ProductBillOfMaterials.Add(new() { ProductId = ProductId, BillOfMaterial = bom, Version = 1, IsActive = true });
        foreach (var (code, wrong) in new[] { ("PMF015", 22.5m), ("PMF093", 95m) })
        {
            var material = new Material { Id = Guid.NewGuid(), Code = code, Name = code + " synthetic material", Kind = kind == IssuedMaterialRepairKind.Bom ? MaterialKind.Raw : MaterialKind.Package };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new() { Id = Guid.NewGuid(), MaterialId = material.Id, DepartmentId = Actor.DepartmentId.GetValueOrDefault(), UoMId = UnitId });
            if (kind == IssuedMaterialRepairKind.Packaging) db.ProductPackages.Add(new() { Id = Guid.NewGuid(), ProductId = ProductId, MaterialId = material.Id, PrescribedQuantity = wrong });
            else bom.Items.Add(new() { Id = Guid.NewGuid(), MaterialId = material.Id, BaseUoMId = UnitId, PrescribedQuantity = wrong });
            var lot = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = UnitId, BatchNumber = code + "/SYNTHETIC/LOT",
                Status = BatchStatus.Available, TotalQuantity = stock, QuantityAssigned = stock, ExpiryDate = DateTime.UtcNow.AddYears(1) };
            db.MaterialBatches.Add(lot);
            db.ShelfMaterialBatches.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, WarehouseLocationShelfId = shelf.Id, UoMId = UnitId, Quantity = stock });
            db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, WarehouseId = production.Id,
                WarehouseLocationShelfId = shelf.Id, ProductionScheduleProductId = BatchId, UoMId = UnitId, Quantity = wrong });
            req.Items.Add(new() { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = UnitId, Quantity = wrong, Status = RequestStatus.Pending });
            db.BinCardInformation.Add(new() { MaterialBatchId = lot.Id, WarehouseId = source.Id, UoMId = UnitId, QuantityReceived = stock, BalanceQuantity = stock, Description = "Synthetic opening receipt" });
        }
        if (issueStock) db.Requisitions.Add(req);
        await db.SaveChangesAsync();
        if (!issueStock) return;
        // Exercise the real warehouse issue operation with the wrong mg quantities.
        var repository = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
        var issue = await repository.IssueStockRequisition(RequisitionId, Actor.UserId.GetValueOrDefault());
        Assert.True(issue.IsSuccess, issue.Error.Description);
    }

    public async Task CorrectMaster(decimal multiplier = 1_000_000)
    {
        await using var db = Open();
        if (Kind == IssuedMaterialRepairKind.Packaging)
            foreach (var row in await db.ProductPackages.Where(item => item.ProductId == ProductId).ToListAsync()) row.PrescribedQuantity *= multiplier;
        else foreach (var row in await db.BillOfMaterialItems.Where(item => item.BillOfMaterial.ProductId == ProductId).ToListAsync()) row.PrescribedQuantity *= multiplier;
        await db.SaveChangesAsync();
    }

    public async Task<ApplyIssuedMaterialRepairRequest> Request()
    {
        await using var db = Open();
        var preview = await Service(db).Preview(BatchId, Kind);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.True(preview.Value.CanApply, string.Join(" ", preview.Value.Blockers));
        return new() { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken, Reason = "Correct synthetic kg values stored as mg" };
    }

    public async ValueTask DisposeAsync()
    {
        if (admin is null) return;
        NpgsqlConnection.ClearAllPools();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Database}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
        await admin.DisposeAsync();
    }
}
