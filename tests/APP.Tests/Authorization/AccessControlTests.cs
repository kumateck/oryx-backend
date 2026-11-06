using System.Reflection;
using System.Security.Claims;
using API.Controllers;
using APP.Claims;
using APP.Repository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Requests;
using SHARED.Services.Identity;
using IDatabase = StackExchange.Redis.IDatabase;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Phase 0 of the permissions audit: the endpoints that let anyone escalate or tamper are closed.</summary>
public sealed class AccessControlTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    /// <summary>Redis is optional for these paths; every call fails and the repository carries on.</summary>
    public class NoRedis : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args) => throw new InvalidOperationException("No Redis in tests.");
    }

    private static ApplicationDbContext Context(Guid actor) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options, new Actor(actor));

    private static int _claimId = 1000;

    private static Role AddRole(ApplicationDbContext db, string name, params string[] keys)
    {
        var role = new Role { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), DisplayName = name };
        db.Roles.Add(role);
        foreach (var key in keys)
        {
            var claim = new IdentityRoleClaim<Guid> { Id = ++_claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        return role;
    }

    private static User AddUser(ApplicationDbContext db, Role role = null, bool disabled = false)
    {
        var user = new User { Id = Guid.NewGuid(), UserName = $"user-{Guid.NewGuid():N}", Email = "user@example.com", FirstName = "Test", LastName = "User", IsDisabled = disabled };
        db.Users.Add(user);
        if (role is not null) db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
        return user;
    }

    private static string Policy(Type controller, string action, Type[] parameters = null)
    {
        var method = (parameters is null ? controller.GetMethod(action) : controller.GetMethod(action, parameters))
            ?? throw new InvalidOperationException($"{controller.Name}.{action} is missing");
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        return Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy;
    }

    [Fact]
    public void Access_management_and_quality_gates_name_their_permission_and_none_is_anonymous()
    {
        Assert.Equal(PermissionKeys.CanCreateUser, Policy(typeof(UserController), nameof(UserController.CreateUser)));
        Assert.Equal(PermissionKeys.CanCreateUser, Policy(typeof(UserController), nameof(UserController.CreateNewUser)));
        Assert.Equal(PermissionKeys.CanEditUser, Policy(typeof(UserController), nameof(UserController.UpdateUser)));
        Assert.Equal(PermissionKeys.CanAssignUserRole, Policy(typeof(UserController), nameof(UserController.UpdateUserRoles)));
        Assert.Equal(PermissionKeys.CanDeleteUser, Policy(typeof(UserController), nameof(UserController.DeleteUser)));
        Assert.Equal(PermissionKeys.CanCreateSignatureSettings, Policy(typeof(UserController), nameof(UserController.UploadSignature)));
        // Own avatar and block/unblock: signed in, with the block or unblock key checked by direction in the repository.
        Assert.Null(Policy(typeof(UserController), nameof(UserController.UploadAnImage)));
        Assert.Null(Policy(typeof(UserController), nameof(UserController.DisableUser)));
        Assert.Equal(PermissionKeys.CanUpdateExistingPermission, Policy(typeof(PermissionController), nameof(PermissionController.UpdateRolePermissions)));
        Assert.Equal(PermissionKeys.CanViewPermissions, Policy(typeof(PermissionController), nameof(PermissionController.GetPermissionsByRole)));
        Assert.Equal(PermissionKeys.CanCreateRole, Policy(typeof(RoleController), nameof(RoleController.CreateRole)));
        Assert.Equal(PermissionKeys.CanCreateNewApproval, Policy(typeof(ApprovalController), nameof(ApprovalController.CreateApproval)));
        Assert.Equal(PermissionKeys.CanTransferApprovalRights, Policy(typeof(ApprovalController), nameof(ApprovalController.TransferApprovalRights)));
        Assert.Equal(PermissionKeys.CanDeleteApprovals, Policy(typeof(ApprovalController), nameof(ApprovalController.DeleteApproval)));
        Assert.Equal(PermissionKeys.CanApproveMaterialBatch, Policy(typeof(MaterialController), nameof(MaterialController.ApproveMaterialBatch)));
        Assert.Equal(PermissionKeys.CanRequestRetestProductTest, Policy(typeof(AnalyticalTestRequestController), nameof(AnalyticalTestRequestController.RequestRetest)));
        Assert.Equal(PermissionKeys.CanCompleteRetestProductTest, Policy(typeof(AnalyticalTestRequestController), nameof(AnalyticalTestRequestController.CompleteRetest)));
        foreach (var stock in typeof(MaterialController).GetMethods().Where(method => method.Name == nameof(MaterialController.GetApprovedRawMaterialsByDepartment)))
            Assert.Empty(stock.GetCustomAttributes<AllowAnonymousAttribute>());
        foreach (var upload in typeof(FileController).GetMethods().Where(method => method.Name == nameof(FileController.UploadFile)))
            Assert.Empty(upload.GetCustomAttributes<AllowAnonymousAttribute>());

        var catalog = PermissionUtils.GeneratePermissions().ToList();
        foreach (var key in new[] { PermissionKeys.CanAssignUserRole, PermissionKeys.CanDeleteUser, PermissionKeys.CanApproveMaterialBatch,
                     PermissionKeys.CanRequestRetestProductTest, PermissionKeys.CanCompleteRetestProductTest })
            Assert.Single(catalog, permission => permission.Key == key);
    }

    [Fact]
    public void Self_sign_up_is_refused()
    {
        var result = new UserController(null!).CreateNewUser(new());
        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    private static async Task<bool> Granted(ApplicationDbContext db, Guid user, string key)
    {
        var handler = new PermissionAuthorizationHandler(db);
        var context = new AuthorizationHandlerContext([new PermissionRequirement(key)],
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], "test")), null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Policies_follow_current_data_disabled_accounts_and_deleted_roles_grant_nothing()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var role = AddRole(db, "editors", PermissionKeys.CanEditUser);
        var active = AddUser(db, role);
        var disabled = AddUser(db, role, disabled: true);
        await db.SaveChangesAsync();

        Assert.True(await Granted(db, active.Id, PermissionKeys.CanEditUser));
        Assert.False(await Granted(db, active.Id, PermissionKeys.CanDeleteUser));
        Assert.False(await Granted(db, disabled.Id, PermissionKeys.CanEditUser));

        // A revoked permission stops working on the next request; nothing is cached for hours.
        db.PermissionTypes.RemoveRange(db.PermissionTypes);
        await db.SaveChangesAsync();
        Assert.False(await Granted(db, active.Id, PermissionKeys.CanEditUser));

        var other = AddRole(db, "deleted", PermissionKeys.CanBlockUser);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = active.Id, RoleId = other.Id });
        await db.SaveChangesAsync();
        Assert.True(await Granted(db, active.Id, PermissionKeys.CanBlockUser));
        other.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.False(await Granted(db, active.Id, PermissionKeys.CanBlockUser));
    }

    private static UserRepository Users(ApplicationDbContext db) => new(db, null!, null!, null!, null!, null!);

    [Fact]
    public async Task Nobody_changes_their_own_role_or_hands_out_permissions_they_do_not_hold()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var admins = AddRole(db, "user-admins", PermissionKeys.CanAssignUserRole, PermissionKeys.CanEditUser);
        var super = AddRole(db, "super", PermissionKeys.CanAssignUserRole, PermissionKeys.CanDeleteUser, PermissionKeys.CanUpdateExistingPermission);
        var actor = AddUser(db, admins);
        actor.Id = actorId;
        var target = AddUser(db);
        await db.SaveChangesAsync();
        var repository = Users(db);

        Assert.Equal("Access.Self", (await repository.UpdateRolesOfUser(new() { RoleNames = ["super"] }, actorId, actorId)).Error.Code);
        var escalation = await repository.UpdateRolesOfUser(new() { RoleNames = ["super"] }, target.Id, actorId);
        Assert.Equal("Access.Escalation", escalation.Error.Code);
        Assert.Contains(PermissionKeys.CanDeleteUser, escalation.Error.Description);
        Assert.Empty(await db.UserRoles.Where(role => role.UserId == target.Id).ToListAsync());

        // Editing a user with a role change follows the same rules.
        Assert.Equal("Access.Escalation", (await repository.UpdateUser(new() { RoleName = "super" }, target.Id, actorId)).Error.Code);
        Assert.Equal("Access.Self", (await repository.UpdateUser(new() { RoleName = "user-admins" }, actorId, actorId)).Error.Code);
        Assert.Equal("Access.Permission", (await repository.CreateUser(new() { RoleNames = { "super" } }, actorId)).Error.Code);
    }

    [Fact]
    public async Task Delete_removes_the_named_account_never_the_caller_and_block_and_unblock_are_separate()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var role = AddRole(db, "it", PermissionKeys.CanDeleteUser, PermissionKeys.CanBlockUser);
        var actor = AddUser(db, role);
        actor.Id = actorId;
        var target = AddUser(db);
        var blocked = AddUser(db, disabled: true);
        await db.SaveChangesAsync();
        var repository = Users(db);

        Assert.Equal("Access.Self", (await repository.DeleteUser(actorId, actorId)).Error.Code);
        Assert.True((await repository.DeleteUser(target.Id, actorId)).IsSuccess);
        db.ChangeTracker.Clear();
        Assert.NotNull((await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == target.Id)).DeletedAt);
        Assert.Null((await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == actorId)).DeletedAt);

        Assert.Equal("Access.Self", (await repository.ToggleDisableUser(actorId, actorId)).Error.Code);
        Assert.Equal("Access.Permission", (await repository.ToggleDisableUser(blocked.Id, actorId)).Error.Code);
        var other = AddUser(db);
        await db.SaveChangesAsync();
        Assert.True((await repository.ToggleDisableUser(other.Id, actorId)).IsSuccess);
        Assert.True((await db.Users.AsNoTracking().SingleAsync(user => user.Id == other.Id)).IsDisabled);
    }

    private static PermissionRepository Permissions(ApplicationDbContext db) =>
        new(db, null!, DispatchProxy.Create<IDatabase, NoRedis>(), NullLogger<PermissionRepository>.Instance);

    private static List<DOMAIN.Entities.Permissions.PermissionModuleDto> Request(params string[] keys) =>
        [new() { Module = "Test", Children = keys.Select(key => new PermissionDetailDto { Key = key, Types = ["Access"] }).ToList() }];

    [Fact]
    public async Task Role_permissions_accept_only_catalog_keys_the_editor_holds()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var editors = AddRole(db, "permission-editors", PermissionKeys.CanUpdateExistingPermission, PermissionKeys.CanViewRoles);
        var actor = AddUser(db, editors);
        actor.Id = actorId;
        var clerks = AddRole(db, "clerks", PermissionKeys.CanDeleteUser);
        var clerk = AddUser(db, clerks);
        await db.SaveChangesAsync();
        var repository = Permissions(db);

        Assert.Equal("Permission.Unknown", (await repository.UpdateRolePermissions(Request("CanDoAnything"), clerks.Id, actorId)).Error.Code);
        Assert.Equal("Access.Escalation", (await repository.UpdateRolePermissions(Request(PermissionKeys.CanAssignUserRole), clerks.Id, actorId)).Error.Code);
        // A permission the role already has may stay, and permissions the editor holds may be added.
        Assert.True((await repository.UpdateRolePermissions(Request(PermissionKeys.CanDeleteUser, PermissionKeys.CanViewRoles), clerks.Id, actorId)).IsSuccess);
        Assert.Equal(new[] { PermissionKeys.CanDeleteUser, PermissionKeys.CanViewRoles }.Order(), (await CurrentPermissionSet.Load(db, clerk.Id)).Order());

        var plain = AddUser(db);
        await db.SaveChangesAsync();
        Assert.Equal("Access.Permission", (await repository.UpdateRolePermissions(Request(), clerks.Id, plain.Id)).Error.Code);
    }

    [Fact]
    public async Task The_system_administrator_may_grant_permissions_nobody_holds_yet()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var editors = AddRole(db, "permission-editors", PermissionKeys.CanUpdateExistingPermission, PermissionKeys.CanAssignUserRole);
        var admin = AddUser(db, editors);
        admin.Id = actorId;
        admin.Email = APP.Repository.UserPasswordAdminRepository.PasswordAdminEmail;
        var other = AddUser(db, editors);
        var clerks = AddRole(db, "clerks", PermissionKeys.CanDeleteUser);
        var target = AddUser(db, clerks);
        var forecasters = AddRole(db, "forecasters", PermissionKeys.CanViewProductionScheduleMaterialForecast);
        await db.SaveChangesAsync();
        var permissions = Permissions(db);

        // A new feature's key that no role holds: the administrator can give it out, other editors still cannot.
        var key = PermissionKeys.CanViewProductionScheduleMaterialForecast;
        Assert.Equal("Access.Escalation", (await permissions.UpdateRolePermissions(Request(key), clerks.Id, other.Id)).Error.Code);
        Assert.True((await permissions.UpdateRolePermissions(Request(PermissionKeys.CanDeleteUser, key), clerks.Id, actorId)).IsSuccess);
        Assert.Contains(key, await CurrentPermissionSet.Load(db, target.Id));
        // Assigning a role carrying keys the administrator lacks is allowed for them too, and still refused for others.
        Assert.Equal("Access.Escalation", (await Users(db).UpdateRolesOfUser(new() { RoleNames = ["forecasters"] }, target.Id, other.Id)).Error.Code);
        Assert.Null(await AccessGuard.RoleAssignment(db, await CurrentPermissionSet.Load(db, actorId), [forecasters.Id], actorId));

        // A disabled administrator account is no administrator.
        admin.IsDisabled = true;
        await db.SaveChangesAsync();
        Assert.False(await AccessGuard.IsSystemAdministrator(db, actorId));
    }

    [Fact]
    public async Task A_users_permission_list_follows_role_changes_at_once()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var viewers = AddRole(db, "viewers", PermissionKeys.CanViewRoles);
        var forecasters = AddRole(db, "forecasters", PermissionKeys.CanViewProductionScheduleMaterialForecast);
        var user = AddUser(db, viewers);
        await db.SaveChangesAsync();
        var repository = Permissions(db);
        static IEnumerable<string> Granted(List<DOMAIN.Entities.Permissions.PermissionModuleDto> modules) =>
            modules.SelectMany(module => module.Children).Where(child => child.Types.Count > 0).Select(child => child.Key);

        Assert.DoesNotContain(PermissionKeys.CanViewProductionScheduleMaterialForecast, Granted((await repository.GetAllPermissionForUser(user.Id)).Value));
        // A role given after the first read (or by a grant migration) shows on the very next read.
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = forecasters.Id });
        await db.SaveChangesAsync();
        Assert.Contains(PermissionKeys.CanViewProductionScheduleMaterialForecast, Granted((await repository.GetAllPermissionForUser(user.Id)).Value));
        // A deleted role and a disabled account grant nothing, as on the server.
        forecasters.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.DoesNotContain(PermissionKeys.CanViewProductionScheduleMaterialForecast, Granted((await repository.GetAllPermissionForUser(user.Id)).Value));
        user.IsDisabled = true;
        await db.SaveChangesAsync();
        Assert.Empty(Granted((await repository.GetAllPermissionForUser(user.Id)).Value));
    }

    [Fact]
    public async Task Verification_needs_the_key_for_that_kind_of_document()
    {
        var actorId = Guid.NewGuid();
        await using var db = Context(actorId);
        var actor = AddUser(db, AddRole(db, "qc", PermissionKeys.CanVerifyProductArd));
        actor.Id = actorId;
        await db.SaveChangesAsync();
        var result = await new VerificationRepository(db).VerifyEntity(new VerifyRequest { ModelType = VerifiableEntity.Product, ModelId = Guid.NewGuid() }, actorId);
        Assert.Equal("Access.Permission", result.Error.Code);
        Assert.Contains(PermissionKeys.CanVerifyProduct, result.Error.Description);
    }
}
