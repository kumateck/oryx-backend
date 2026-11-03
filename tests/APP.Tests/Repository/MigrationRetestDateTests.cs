using System.Reflection;
using API.Controllers;
using APP.Repository;
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
using Xunit;

namespace APP.Tests.Repository;

public class MigrationRetestDateTests
{
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
        harness.Db.StockMigrationLines.Add(new StockMigrationLine { Id = Guid.NewGuid(), StockMigration = migration, ImportedBatchId = batch.Id });
        await harness.Db.SaveChangesAsync();
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsSuccess);
        Assert.Equal(request.RetestDate, batch.RetestDate);
        var audit = await harness.Db.MigrationRetestDateChanges.SingleAsync();
        Assert.Null(audit.PreviousDate);
        Assert.Equal(request.RetestDate, audit.NewDate);
        Assert.Equal(actor.Id, audit.ActorId);
        Assert.Equal(request.Reason, audit.Reason);
        Assert.Empty(await harness.Db.MaterialRetestRequests.ToListAsync());
        Assert.Equal(BatchStatus.Available, batch.Status);
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Single(await harness.Db.MigrationRetestDateChanges.ToListAsync());
        request.ExpectedRetestDate = batch.RetestDate;
        request.RetestDate = null;
        batch.Status = BatchStatus.Frozen;
        await harness.Db.SaveChangesAsync();
        Assert.True((await repository.UpdateMigrationRetestDate(batch.Id, request, actor.Id)).IsFailure);
        Assert.Single(await harness.Db.MigrationRetestDateChanges.ToListAsync());
    }
}
