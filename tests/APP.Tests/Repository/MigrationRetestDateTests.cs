using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Materials;
using APP.Mapper.Resolvers.MaterialBatch;
using AutoMapper;
using APP.Tests.QcWorksheets;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using INFRASTRUCTURE.Context;
using Xunit;

namespace APP.Tests.Repository;

public class MigrationRetestDateTests
{
    private sealed class ArReference { public string ArNumber { get; set; } = string.Empty; }

    [Fact]
    public void Batch_details_correction_has_a_specific_registered_permission()
    {
        var method = typeof(MaterialController).GetMethod(nameof(MaterialController.UpdateMigrationBatchDetails))!;
        Assert.Equal(PermissionKeys.CanUpdateMigrationBatchDetails, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == PermissionKeys.CanUpdateMigrationBatchDetails);
    }

    [RepairPostgresFact]
    public async Task Migrated_details_are_audited_and_used_in_the_stock_report()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        var actor = fixture.Actor.UserId.GetValueOrDefault();
        var batch = await db.MaterialBatches.OrderBy(row => row.Id).FirstAsync();
        var previousExpiry = batch.ExpiryDate;
        var source = new StockMigrationLine
        {
            StockMigration = new StockMigration { DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), Status = StockMigrationStatus.Approved },
            ImportedBatchId = batch.Id, ArNumber = "AR-IMPORT"
        };
        db.StockMigrationLines.Add(source);
        await db.SaveChangesAsync();
        var request = new UpdateMigrationBatchDetails
        {
            ExpectedExpiryDate = previousExpiry, ManufacturingDate = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            ExpiryDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc), GrnNumber = " GRN-MIG-42 ", Reason = "Checked source ledger"
        };
        var repository = new MaterialRepository(db, null!);
        Assert.Equal("MigrationBatch.Permission", (await repository.UpdateMigrationBatchDetails(batch.Id, request, actor)).Error.Code);

        const string key = PermissionKeys.CanUpdateMigrationRetestDate;
        db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 804, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = key });
        db.PermissionTypes.Add(new PermissionType { RoleClaimId = 804, Key = key, Type = "Action" });
        await db.SaveChangesAsync();
        var migration = new INFRASTRUCTURE.Migrations.AddMigrationBatchDetailsCorrections();
        var sql = db.GetService<IMigrationsSqlGenerator>();
        foreach (var command in sql.Generate(migration.DownOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), MAX(\"Id\")) FROM \"roleclaims\"");
        foreach (var command in sql.Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var updated = await repository.UpdateMigrationBatchDetails(batch.Id, request, actor);
        Assert.True(updated.IsSuccess, updated.Error.Description);
        Assert.Equal("MigrationBatch.Changed", (await repository.UpdateMigrationBatchDetails(batch.Id, request, actor)).Error.Code);
        var audit = await db.MigrationRetestDateChanges.AsNoTracking().SingleAsync();
        Assert.Equal(previousExpiry, audit.ExpiryDate);
        Assert.Equal(new DateTime(2020, 5, 1, 0, 0, 0, DateTimeKind.Utc), audit.NewManufacturingDate);
        Assert.Equal(new DateTime(2025, 2, 28, 0, 0, 0, DateTimeKind.Utc), audit.NewExpiryDate);
        Assert.Equal("GRN-MIG-42", audit.NewGrnNumber);
        Assert.Equal("AR-IMPORT", (await db.StockMigrationLines.AsNoTracking().SingleAsync()).ArNumber);
        var report = await repository.GetMaterialStockReport(new() { ExpiredOnly = true }, actor);
        var row = Assert.Single(report.Value.Rows);
        Assert.Equal("GRN-MIG-42", row.GrnNumber);
        Assert.Equal("AR-IMPORT", row.ArNumber);
        Assert.Equal(audit.NewExpiryDate, row.ExpiryDate);
    }

    [Fact]
    public void Correction_lookup_translates_with_later_QC_sampling_exclusion()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata_only").Options, new NoCurrentUser());
        Assert.Contains("NOT EXISTS", MigrationBatchReferences.Corrections(db, Guid.NewGuid()).ToQueryString());
    }

    [Fact]
    public void Correction_has_its_own_backend_policy_and_does_not_grant_roles()
    {
        var method = typeof(MaterialController).GetMethod(nameof(MaterialController.UpdateMigrationRetestDate))
            ?? throw new InvalidOperationException("Missing correction endpoint");
        Assert.Equal(PermissionKeys.CanUpdateMigrationRetestDate, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Single(PermissionUtils.GeneratePermissions(), p => p.Key == PermissionKeys.CanUpdateMigrationRetestDate);
    }

    [Fact]
    public async Task Correction_requires_live_grant_scope_and_migration_and_saves_independent_audit()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await harness.SeedMaterialBatch("MIGRATION-CORRECTION", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        var department = new Department { Id = Guid.NewGuid(), Name = "Syrup" };
        var actor = new User { Id = Guid.NewGuid(), UserName = "Migration operator", Department = department };
        var role = new Role { Id = Guid.NewGuid(), Name = "Correction role" };
        harness.Db.Users.Add(actor);
        harness.Db.Roles.Add(role);
        harness.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id });
        await harness.Db.SaveChangesAsync();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var mapper = new MapperConfiguration(config => config.CreateMap<MaterialBatch, ArReference>()
            .ForMember(dest => dest.ArNumber, opt => opt.MapFrom(new MaterialBatchArNumberResolver(harness.Db)))).CreateMapper();
        var request = new UpdateMigrationRetestDate { RetestDate = DateTime.UtcNow.AddDays(60),
            ExpectedExpiryDate = batch.ExpiryDate, Reason = "Supplier migration extension evidence" };
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        var claim = new IdentityRoleClaim<Guid> { Id = 987, RoleId = role.Id, ClaimType = AppConstants.Permission,
            ClaimValue = PermissionKeys.CanUpdateMigrationRetestDate };
        harness.Db.RoleClaims.Add(claim);
        harness.Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id,
            Key = claim.ClaimValue, Type = "Action" });
        await harness.Db.SaveChangesAsync();
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        var migration = new StockMigration { Id = Guid.NewGuid(), Department = department, Status = StockMigrationStatus.Approved };
        var source = new StockMigrationLine { Id = Guid.NewGuid(), StockMigration = migration, ImportedBatchId = batch.Id, ArNumber = "AR-ORIGINAL" };
        harness.Db.StockMigrationLines.Add(source);
        await harness.Db.SaveChangesAsync();
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal(request.RetestDate, batch.RetestDate);
        var audit = await harness.Db.MigrationRetestDateChanges.SingleAsync();
        Assert.Null(audit.PreviousDate);
        Assert.Equal(request.RetestDate, audit.NewDate);
        Assert.Equal(actor.Id, audit.ActorId);
        Assert.Equal(request.Reason, audit.Reason);
        Assert.Null(audit.NewArNumber);
        Assert.Equal("AR-ORIGINAL", await MigrationBatchReferences.Current(harness.Db, batch.Id));
        Assert.Equal("AR-ORIGINAL", mapper.Map<ArReference>(batch).ArNumber);
        Assert.Empty(await harness.Db.MaterialRetestRequests.ToListAsync());
        Assert.Equal(BatchStatus.Available, batch.Status);
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Single(await harness.Db.MigrationRetestDateChanges.ToListAsync());
        request.ExpectedRetestDate = batch.RetestDate;
        request.ArNumber = " AR-CORRECTED ";
        request.ExpectedArNumber = "AR-STALE";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Single(await harness.Db.MigrationRetestDateChanges.ToListAsync());
        request.ExpectedArNumber = "AR-ORIGINAL";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal("AR-CORRECTED", await MigrationBatchReferences.Current(harness.Db, batch.Id));
        Assert.Equal("AR-CORRECTED", mapper.Map<ArReference>(batch).ArNumber);
        var arAudit = await harness.Db.MigrationRetestDateChanges.SingleAsync(c => c.NewArNumber != null);
        Assert.Equal("AR-ORIGINAL", arAudit.PreviousArNumber);
        Assert.Equal("AR-CORRECTED", arAudit.NewArNumber);
        Assert.Equal("AR-ORIGINAL", source.ArNumber);
        request.ExpectedArNumber = "AR-CORRECTED";
        request.ArNumber = "";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal("", await MigrationBatchReferences.Current(harness.Db, batch.Id));
        Assert.Equal("", mapper.Map<ArReference>(batch).ArNumber);
        Assert.Equal("AR-ORIGINAL", source.ArNumber);
        request.ArNumber = new string('x', 501);
        request.ExpectedArNumber = "";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        request.ArNumber = null;
        request.RetestDate = null;
        batch.Status = BatchStatus.Frozen;
        await harness.Db.SaveChangesAsync();
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Equal(3, await harness.Db.MigrationRetestDateChanges.CountAsync());
        harness.Db.MaterialSamplings.Add(new DOMAIN.Entities.MaterialSampling.MaterialSampling
        {
            Id = Guid.NewGuid(), MaterialBatchId = batch.Id, ArNumber = "AR-NEW-QC", CreatedAt = DateTime.UtcNow.AddDays(1),
        });
        await harness.Db.SaveChangesAsync();
        Assert.Equal("AR-NEW-QC", await MigrationBatchReferences.Current(harness.Db, batch.Id));
        Assert.Equal("AR-NEW-QC", mapper.Map<ArReference>(batch).ArNumber);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-60)]
    [InlineData(60)]
    public async Task Ar_only_correction_preserves_absent_or_legacy_retest_dates_and_still_checks_stale_dates(int? retestOffset)
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await harness.SeedMaterialBatch("AR-ONLY", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        batch.RetestDate = retestOffset.HasValue ? DateTime.UtcNow.AddDays(retestOffset.Value) : null;
        var originalRetest = batch.RetestDate;
        var department = new Department { Id = Guid.NewGuid(), Name = "Syrup" };
        var actor = new User { Id = Guid.NewGuid(), UserName = "AR operator", Department = department };
        var role = new Role { Id = Guid.NewGuid(), Name = "AR correction role" };
        harness.Db.Users.Add(actor);
        harness.Db.Roles.Add(role);
        harness.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id });
        var claim = new IdentityRoleClaim<Guid> { Id = 988, RoleId = role.Id, ClaimType = AppConstants.Permission,
            ClaimValue = PermissionKeys.CanUpdateMigrationRetestDate };
        harness.Db.RoleClaims.Add(claim);
        harness.Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id,
            Key = claim.ClaimValue, Type = "Action" });
        var migration = new StockMigration { Id = Guid.NewGuid(), Department = department, Status = StockMigrationStatus.Approved };
        harness.Db.StockMigrationLines.Add(new StockMigrationLine { Id = Guid.NewGuid(), StockMigration = migration,
            ImportedBatchId = batch.Id, ArNumber = "AR-ORIGINAL" });
        await harness.Db.SaveChangesAsync();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var request = new UpdateMigrationRetestDate { RetestDate = originalRetest, ExpectedRetestDate = originalRetest,
            ExpectedExpiryDate = batch.ExpiryDate, ArNumber = "AR-ONLY", ExpectedArNumber = "AR-ORIGINAL",
            Reason = "Correct migrated opening reference only" };
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal(originalRetest, batch.RetestDate);
        var audit = await harness.Db.MigrationRetestDateChanges.SingleAsync();
        Assert.Equal(originalRetest, audit.PreviousDate);
        Assert.Equal(originalRetest, audit.NewDate);
        Assert.Equal("AR-ORIGINAL", audit.PreviousArNumber);
        Assert.Equal("AR-ONLY", audit.NewArNumber);
        request.ExpectedArNumber = "AR-ONLY";
        request.ArNumber = "AR-LATER";
        request.ExpectedRetestDate = DateTime.UtcNow.AddDays(90);
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        request.ExpectedRetestDate = originalRetest;
        request.RetestDate = batch.ExpiryDate.Value.AddDays(-1);
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Single(await harness.Db.MigrationRetestDateChanges.ToListAsync());
        Assert.Equal(originalRetest, batch.RetestDate);
    }

    [Fact]
    public async Task Batch_number_correction_keeps_dates_and_the_migration_evidence_and_checks_stale_values()
    {
        using var harness = new QcWorksheetTestContext();
        // Excel turned the supplier batch "RMA016" into a date on import.
        var batch = await harness.SeedMaterialBatch("01/12/2017", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        batch.RetestDate = null;
        var department = new Department { Id = Guid.NewGuid(), Name = "Tablet" };
        var actor = new User { Id = Guid.NewGuid(), UserName = "Batch operator", Department = department };
        var role = new Role { Id = Guid.NewGuid(), Name = "Batch correction role" };
        harness.Db.Users.Add(actor);
        harness.Db.Roles.Add(role);
        harness.Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id });
        var claim = new IdentityRoleClaim<Guid> { Id = 989, RoleId = role.Id, ClaimType = AppConstants.Permission,
            ClaimValue = PermissionKeys.CanUpdateMigrationRetestDate };
        harness.Db.RoleClaims.Add(claim);
        harness.Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id,
            Key = claim.ClaimValue, Type = "Action" });
        var migration = new StockMigration { Id = Guid.NewGuid(), Department = department, Status = StockMigrationStatus.Approved };
        var source = new StockMigrationLine { Id = Guid.NewGuid(), StockMigration = migration, ImportedBatchId = batch.Id,
            BatchNumber = "01/12/2017", ArNumber = "AR-1" };
        harness.Db.StockMigrationLines.Add(source);
        await harness.Db.SaveChangesAsync();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var request = new UpdateMigrationRetestDate { RetestDate = null, ExpectedRetestDate = null,
            ExpectedExpiryDate = batch.ExpiryDate, BatchNumber = " RMA016 ", ExpectedBatchNumber = "STALE",
            Reason = "Excel converted the supplier batch number into a date" };

        Assert.Equal("MaterialRetest.Changed", (await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).Error.Code);
        request.ExpectedBatchNumber = "01/12/2017";
        request.BatchNumber = "   ";
        Assert.Equal("MaterialRetest.BatchNumber", (await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).Error.Code);
        request.BatchNumber = new string('x', 501);
        Assert.Equal("MaterialRetest.BatchNumber", (await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).Error.Code);
        Assert.Empty(await harness.Db.MigrationRetestDateChanges.ToListAsync());

        request.BatchNumber = " RMA016 ";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal("RMA016", batch.BatchNumber);
        Assert.Null(batch.RetestDate);
        Assert.Equal("01/12/2017", source.BatchNumber);
        var audit = await harness.Db.MigrationRetestDateChanges.SingleAsync();
        Assert.Equal("01/12/2017", audit.PreviousBatchNumber);
        Assert.Equal("RMA016", audit.NewBatchNumber);
        Assert.Null(audit.NewArNumber);

        // Omitting the batch number preserves it and records no batch change.
        request.BatchNumber = null;
        request.ExpectedBatchNumber = null;
        request.Reason = "Separate date-only correction";
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal("RMA016", batch.BatchNumber);
        var later = await harness.Db.MigrationRetestDateChanges.SingleAsync(item => item.Reason == "Separate date-only correction");
        Assert.Null(later.PreviousBatchNumber);
        Assert.Null(later.NewBatchNumber);
    }
}
