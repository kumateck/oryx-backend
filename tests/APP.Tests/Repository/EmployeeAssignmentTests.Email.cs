using APP.Utils;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed partial class EmployeeAssignmentTests
{
    [Fact]
    public async Task Resaving_employee_email_repairs_the_linked_account()
    {
        await using var db = InMemory();
        var actor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeEmail);
        var (employee, account) = await SeedLinkedAccount(db);
        var unrelated = new User { Id = Guid.NewGuid(), Email = "other@example.test", UserName = "other@example.test" };
        db.Users.Add(unrelated);
        await db.SaveChangesAsync();

        var result = await Repository(db).UpdateEmployeeEmail(employee.Id, " " + employee.Email + " ", actor);

        Assert.True(result.IsSuccess, result.Error.Description);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync(row => row.Id == account.Id);
        Assert.Equal(employee.Email, saved.Email);
        Assert.Equal(employee.Email, saved.UserName);
        Assert.Equal(employee.Email.ToUpperInvariant(), saved.NormalizedEmail);
        Assert.Equal(employee.Email.ToUpperInvariant(), saved.NormalizedUserName);
        Assert.Equal("existing password hash", saved.PasswordHash);
        Assert.NotEqual("existing security stamp", saved.SecurityStamp);
        Assert.False(saved.EmailConfirmed);
        Assert.Equal(unrelated.Email, (await db.Users.FindAsync(unrelated.Id))!.Email);
        Assert.Empty(db.PasswordResets);
    }

    [Fact]
    public async Task Email_permission_is_required_to_update_a_linked_account_through_the_profile()
    {
        await using var db = InMemory();
        var actor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeInfo);
        var (employee, account) = await SeedLinkedAccount(db);
        var repository = Repository(db);

        var denied = await repository.UpdateEmployeeEmail(employee.Id, "new@example.test", actor);
        Assert.Equal("Employee.Permission", denied.Error.Code);
        var profile = new UpdateEmployeeRequest { Email = "new@example.test", FirstName = "Updated" };
        var deniedProfile = await repository.UpdateEmployee(employee.Id, profile, actor);
        Assert.Equal("Employee.EmailPermission", deniedProfile.Error.Code);
        Assert.Equal("Fixture", employee.FirstName);
        Assert.Equal("old@example.test", account.Email);

        profile.Email = employee.Email;
        var unrelatedEdit = await repository.UpdateEmployee(employee.Id, profile, actor);
        Assert.True(unrelatedEdit.IsSuccess, unrelatedEdit.Error.Description);
        Assert.Equal("Updated", account.FirstName);
        Assert.Equal("old@example.test", account.Email);

        var emailEditor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeInfo, PermissionKeys.CanUpdateEmployeeEmail);
        profile.Email = "new@example.test";
        var updated = await repository.UpdateEmployee(employee.Id, profile, emailEditor);
        Assert.True(updated.IsSuccess, updated.Error.Description);
        Assert.Equal("new@example.test", employee.Email);
        Assert.Equal(employee.Email, account.Email);
    }

    [Fact]
    public async Task Email_update_never_claims_an_account_for_an_unlinked_employee()
    {
        await using var db = InMemory();
        var actor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeEmail);
        var (employee, account) = await SeedLinkedAccount(db);
        employee.UserId = null;
        employee.Email = account.Email;
        await db.SaveChangesAsync();

        var result = await Repository(db).UpdateEmployeeEmail(employee.Id, "new@example.test", actor);

        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal("new@example.test", employee.Email);
        Assert.Equal("old@example.test", account.Email);
        Assert.Null(employee.UserId);
    }

    [Fact]
    public async Task Email_update_rejects_a_case_insensitive_collision_with_another_account()
    {
        await using var db = InMemory();
        var actor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeEmail);
        var (employee, account) = await SeedLinkedAccount(db);
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "USED@example.test", UserName = "someone-else" });
        await db.SaveChangesAsync();

        var result = await Repository(db).UpdateEmployeeEmail(employee.Id, "used@example.test", actor);

        Assert.Equal("Employee.Email", result.Error.Code);
        Assert.Equal("current@example.test", employee.Email);
        Assert.Equal("old@example.test", account.Email);
    }

    [RepairPostgresFact]
    public async Task Account_write_conflict_rolls_back_employee_email_on_postgres()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(issueStock: false);
        await using var db = fixture.Open();
        var actor = await EmailEditor(db, PermissionKeys.CanUpdateEmployeeEmail);
        var (employee, account) = await SeedLinkedAccount(db);
        db.Users.Add(new User { Id = Guid.NewGuid(), UserName = "new@example.test", NormalizedUserName = "NEW@EXAMPLE.TEST" });
        await db.SaveChangesAsync();
        var repository = Repository(db);

        var conflict = await repository.UpdateEmployeeEmail(employee.Id, "new@example.test", actor);
        Assert.Equal("Employee.UpdateConflict", conflict.Error.Code);
        Assert.Equal("current@example.test", (await db.Employees.AsNoTracking().SingleAsync(row => row.Id == employee.Id)).Email);
        Assert.Equal("old@example.test", (await db.Users.AsNoTracking().SingleAsync(row => row.Id == account.Id)).Email);

        var retry = await repository.UpdateEmployeeEmail(employee.Id, "available@example.test", actor);
        Assert.True(retry.IsSuccess, retry.Error.Description);
        Assert.Equal("available@example.test", (await db.Employees.AsNoTracking().SingleAsync(row => row.Id == employee.Id)).Email);
        Assert.Equal("available@example.test", (await db.Users.AsNoTracking().SingleAsync(row => row.Id == account.Id)).Email);
    }

    private static async Task<Guid> EmailEditor(ApplicationDbContext db, params string[] keys)
    {
        var actor = new User { Id = Guid.NewGuid(), UserName = "editor" };
        var role = new Role { Id = Guid.NewGuid(), Name = "Email editor" };
        db.AddRange(actor, role, new IdentityUserRole<Guid> { UserId = actor.Id, RoleId = role.Id });
        var claimId = (await db.RoleClaims.MaxAsync(claim => (int?)claim.Id) ?? 0) + 1;
        foreach (var key in keys)
        {
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { RoleClaimId = claimId, Key = key, Type = "Action" });
            claimId++;
        }
        await db.SaveChangesAsync();
        return actor.Id;
    }

    private static async Task<(Employee Employee, User Account)> SeedLinkedAccount(ApplicationDbContext db)
    {
        var account = new User
        {
            Id = Guid.NewGuid(), Email = "old@example.test", UserName = "old@example.test", EmailConfirmed = true,
            PasswordHash = "existing password hash", SecurityStamp = "existing security stamp"
        };
        var employee = new Employee { Id = Guid.NewGuid(), UserId = account.Id, Email = "current@example.test", FirstName = "Fixture" };
        db.AddRange(employee, account);
        await db.SaveChangesAsync();
        return (employee, account);
    }
}
