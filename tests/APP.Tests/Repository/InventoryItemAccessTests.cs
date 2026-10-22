using APP.Services.Inventory;
using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class InventoryItemAccessTests
{
    private readonly Guid actor = Guid.NewGuid();
    private readonly Guid role = Guid.NewGuid();
    private ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private async Task Seed(ApplicationDbContext c, params string[] keys)
    {
        c.Users.Add(new User { Id = actor, UserName = "test" });
        c.Roles.Add(new Role { Id = role, Name = "Super account" });
        c.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor, RoleId = role });
        foreach (var key in keys) {
            var claim = new IdentityRoleClaim<Guid> { RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key };
            c.RoleClaims.Add(claim); await c.SaveChangesAsync();
            c.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Access" });
        }
        await c.SaveChangesAsync();
    }
    [Theory]
    [InlineData(Store.ItStore, "It")] [InlineData(Store.GeneralStore, "General")]
    [InlineData(Store.EquipmentStore, "Equipment")] [InlineData(Store.ReagentStore, "Reagent")]
    public async Task EachStoreRequiresBothReadAndItsExactWriteKey(Store store, string name)
    {
        await using var c = Context();
        await Seed(c, $"CanViewItemsIn{name}Store", $"CanCreateNewItemIn{name}Store");
        Assert.True(await InventoryItemAccess.Allows(c, actor, store, "CreateNewItemIn"));
        Assert.False(await InventoryItemAccess.Allows(c, actor, store, "EditItemIn"));
        Assert.False(await InventoryItemAccess.Allows(c, actor, store, "DeleteItemFrom"));
    }
    [Fact]
    public async Task RoleNameCannotBypassReadOrStoreRestrictions()
    {
        await using var c = Context(); await Seed(c, "CanCreateNewItemInItStore");
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "CreateNewItemIn"));
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.GeneralStore, "ViewItemsIn"));
        Assert.False(await InventoryItemAccess.Allows(c, null, Store.ItStore, "ViewItemsIn"));
    }
    [Fact]
    public async Task RevocationIsEffectiveOnTheNextCheckWithoutCachedGrant()
    {
        await using var c = Context(); await Seed(c, "CanViewItemsInItStore");
        Assert.True(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "ViewItemsIn"));
        c.PermissionTypes.RemoveRange(await c.PermissionTypes.ToListAsync()); await c.SaveChangesAsync();
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "ViewItemsIn"));
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DisabledAccountOrDeletedRoleCannotRetainAccess(bool deletedRole)
    {
        await using var c = Context(); await Seed(c, "CanViewItemsInItStore");
        if (deletedRole) (await c.Roles.SingleAsync()).DeletedAt = DateTime.UtcNow;
        else (await c.Users.SingleAsync()).IsDisabled = true;
        await c.SaveChangesAsync();
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "ViewItemsIn"));
    }
    [Fact]
    public async Task StockReadAllowsReadOnlyItemIdentityAndCannotCreate()
    {
        await using var c = Context(); await Seed(c, "CanViewItStoreAvailableStocks");
        Assert.True(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "ViewItemsIn", true));
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "ViewItemsIn"));
        Assert.False(await InventoryItemAccess.Allows(c, actor, Store.ItStore, "CreateNewItemIn", true));
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
