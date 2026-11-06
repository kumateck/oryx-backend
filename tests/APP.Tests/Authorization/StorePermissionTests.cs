using System.Reflection;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.ItemGrns;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Stores: item GRNs need the store's receiving key; item logistics name their keys.</summary>
public sealed class StorePermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task An_item_grn_needs_the_receiving_key_of_the_items_store()
    {
        var user = Guid.NewGuid();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(user));
        var role = new Role { Id = Guid.NewGuid(), Name = "role", NormalizedName = "ROLE", DisplayName = "role" };
        db.Roles.Add(role);
        db.Users.Add(new User { Id = user, UserName = "u", Email = "u@example.com", FirstName = "U", LastName = "U" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        var claim = new IdentityRoleClaim<Guid> { Id = 15001, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = "CanApproveItStoreReceivingAreaEntry" };
        db.RoleClaims.Add(claim);
        db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = claim.ClaimValue, RoleClaimId = claim.Id, Type = "Access" });
        var itItem = new Item { Id = Guid.NewGuid(), Name = "Laptop", Store = Store.ItStore };
        var reagent = new Item { Id = Guid.NewGuid(), Name = "Buffer", Store = Store.ReagentStore };
        db.Items.AddRange(itItem, reagent);
        await db.SaveChangesAsync();
        var controller = new ItemGrnController(null!, db) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.HttpContext.Items["Sub"] = user.ToString();

        Assert.IsType<ForbidHttpResult>(await controller.CreateItemGrn(new CreateItemGrnRequest { ItemId = reagent.Id }));
        // The IT store key lets the request through to the repository (absent in this test), so it fails there instead.
        await Assert.ThrowsAsync<NullReferenceException>(() => controller.CreateItemGrn(new CreateItemGrnRequest { ItemId = itItem.Id }));
    }

    [Fact]
    public void Item_logistics_writes_name_their_key()
    {
        var expected = new Dictionary<string, string>
        {
            [nameof(ItemController.CreateItemShipmentInvoice)] = PermissionKeys.CanCreateShipmentInvoice,
            [nameof(ItemController.MarkItemShipmentInvoiceAsPaid)] = PermissionKeys.CanRecordPayment,
            [nameof(ItemController.DeleteItemShipmentDocument)] = PermissionKeys.CanDeleteShipmentDocument,
            [nameof(ItemController.CreateItemWaybill)] = PermissionKeys.CanCreateWaybill,
            [nameof(ItemController.DeleteItemBillingSheet)] = PermissionKeys.CanDeleteBillingSheet,
        };
        foreach (var (name, key) in expected)
            Assert.Equal(key, Assert.Single(Assert.Single(typeof(ItemController).GetMethods().Where(method => method.Name == name))
                .GetCustomAttributes<AuthorizeAttribute>()).Policy);
    }
}
