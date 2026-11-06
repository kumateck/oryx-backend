using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Lists of people to assign hold only users the server would let do the task, searched or not.</summary>
public sealed class PermissionHoldersTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private const string Key = "CanStartRawMaterialTest";
    private static int _claimId = 9800;

    private sealed class World
    {
        public readonly ApplicationDbContext Db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

        public Role Role(string name, bool deleted = false)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant(), DisplayName = name, DeletedAt = deleted ? DateTime.UtcNow : null };
            Db.Roles.Add(role);
            return role;
        }

        public void Grant(Role role, string key, string? type = "Access")
        {
            var claim = new IdentityRoleClaim<Guid> { Id = Interlocked.Increment(ref _claimId), RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            Db.RoleClaims.Add(claim);
            Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = type });
        }

        public User User(string first, Role role, bool disabled = false, bool deleted = false)
        {
            var user = new User { Id = Guid.NewGuid(), UserName = first, Email = $"{first.ToLowerInvariant()}@example.com", FirstName = first, LastName = "Test",
                IsDisabled = disabled, DeletedAt = deleted ? DateTime.UtcNow : null };
            Db.Users.Add(user);
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
            return user;
        }
    }

    [Fact]
    public async Task Only_active_users_whose_role_still_grants_the_key_are_listed()
    {
        var world = new World();
        var analysts = world.Role("analysts");
        world.Grant(analysts, Key);
        var unticked = world.Role("unticked");
        world.Grant(unticked, Key, type: "");
        var removed = world.Role("removed", deleted: true);
        world.Grant(removed, Key);
        var others = world.Role("others");
        world.Grant(others, "CanViewOrder");

        var ama = world.User("Ama", analysts);
        var users = new[]
        {
            ama, world.User("Kofi", analysts, disabled: true), world.User("Esi", analysts, deleted: true),
            world.User("Yaw", unticked), world.User("Akua", removed), world.User("Kwame", others),
        };
        await world.Db.SaveChangesAsync();

        var listed = await PermissionHolders.Query(world.Db, [Key]).Select(user => user.Id).ToListAsync();
        Assert.Equal([ama.Id], listed);
        // The list and the server's own check agree for every user.
        foreach (var user in users)
            Assert.Equal((await CurrentPermissionSet.Load(world.Db, user.Id)).Contains(Key), listed.Contains(user.Id));
    }

    [Fact]
    public async Task Searching_narrows_the_list_and_never_adds_anyone()
    {
        var world = new World();
        var analysts = world.Role("analysts");
        world.Grant(analysts, Key);
        var others = world.Role("others");
        world.Grant(others, "CanViewOrder");
        var ama = world.User("Ama", analysts);
        world.User("Abena", analysts);
        world.User("Amanda", others);
        await world.Db.SaveChangesAsync();

        var found = await PermissionHolders.Query(world.Db, [Key]).Where(user => user.FirstName.StartsWith("Ama")).Select(user => user.Id).ToListAsync();
        Assert.Equal([ama.Id], found);
        Assert.Empty(await PermissionHolders.Query(world.Db, []).ToListAsync());
    }
}
