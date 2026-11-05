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
using INFRASTRUCTURE.Context;
using Xunit;

namespace APP.Tests.Repository;

public class MigrationRetestDateTests
{
    private sealed class ArReference { public string ArNumber { get; set; } = string.Empty; }

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
}
