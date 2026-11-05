using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Procurement: every write names its key, and purchase orders use the supplier's foreign or local type.</summary>
public sealed class ProcurementPermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static int _claimId = 13000;

    [Fact]
    public async Task Purchase_orders_use_the_stored_suppliers_type()
    {
        var user = Guid.NewGuid();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(user));
        var role = new Role { Id = Guid.NewGuid(), Name = "role", NormalizedName = "ROLE", DisplayName = "role" };
        db.Roles.Add(role);
        db.Users.Add(new User { Id = user, UserName = "u", Email = "u@example.com", FirstName = "U", LastName = "U" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        foreach (var key in new[] { PermissionKeys.CanCancelLocalPurchaseOrder, PermissionKeys.CanCreateForeignPurchaseOrder })
        {
            var claim = new IdentityRoleClaim<Guid> { Id = ++_claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        var foreign = new Supplier { Id = Guid.NewGuid(), Name = "Overseas", Type = SupplierType.Foreign };
        var local = new Supplier { Id = Guid.NewGuid(), Name = "Accra", Type = SupplierType.Local };
        var localOrder = new PurchaseOrder { Id = Guid.NewGuid(), Code = "PO-L", SupplierId = local.Id };
        var foreignOrder = new PurchaseOrder { Id = Guid.NewGuid(), Code = "PO-F", SupplierId = foreign.Id };
        db.Suppliers.AddRange(foreign, local);
        db.PurchaseOrders.AddRange(localOrder, foreignOrder);
        await db.SaveChangesAsync();

        async Task<bool> Allowed(PurchaseOrderPermissionAttribute attribute, Dictionary<string, object> arguments)
        {
            var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
            http.Items["Sub"] = user.ToString();
            var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), arguments!, controller: null!);
            var ran = false;
            await attribute.OnActionExecutionAsync(executing, () =>
            {
                ran = true;
                return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
            });
            if (!ran) Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
            return ran;
        }

        var cancel = new PurchaseOrderPermissionAttribute(PermissionKeys.CanCancelForeignPurchaseOrder, PermissionKeys.CanCancelLocalPurchaseOrder);
        Assert.True(await Allowed(cancel, new() { ["purchaseOrderId"] = localOrder.Id }));
        Assert.False(await Allowed(cancel, new() { ["purchaseOrderId"] = foreignOrder.Id }));
        var create = new PurchaseOrderPermissionAttribute(PermissionKeys.CanCreateForeignPurchaseOrder, PermissionKeys.CanCreateLocalPurchaseOrder);
        Assert.True(await Allowed(create, new() { ["request"] = new CreatePurchaseOrderRequest { SupplierId = foreign.Id } }));
        Assert.False(await Allowed(create, new() { ["request"] = new CreatePurchaseOrderRequest { SupplierId = local.Id } }));
    }

    [Fact]
    public void Every_procurement_write_names_its_key_and_cancel_is_granted_to_revisers()
    {
        var authorize = new Dictionary<string, string>
        {
            [nameof(ProcurementController.CreateManufacturer)] = PermissionKeys.CanCreateManufacturer,
            [nameof(ProcurementController.DeleteSupplier)] = PermissionKeys.CanDeleteVendor,
            [nameof(ProcurementController.DeleteBillingSheet)] = PermissionKeys.CanDeleteBillingSheet,
            [nameof(ProcurementController.MarkShipmentAsArrived)] = PermissionKeys.CanChangeShipmentDocumentStatus,
            [nameof(ProcurementController.MarkMultipleShipmentInvoicesAsPaid)] = PermissionKeys.CanRecordPayment,
            [nameof(ProcurementController.DeleteWaybillDocument)] = PermissionKeys.CanDeleteWaybill,
            [nameof(ProcurementController.DistributeMaterialToWarehouse)] = PermissionKeys.CanDistributeMaterial,
        };
        foreach (var (name, key) in authorize)
            Assert.Equal(key, Assert.Single(Assert.Single(typeof(ProcurementController).GetMethods().Where(method => method.Name == name))
                .GetCustomAttributes<AuthorizeAttribute>()).Policy);
        var orders = new Dictionary<string, (string, string)>
        {
            [nameof(ProcurementController.CancelPurchaseOrder)] = (PermissionKeys.CanCancelForeignPurchaseOrder, PermissionKeys.CanCancelLocalPurchaseOrder),
            [nameof(ProcurementController.DeletePurchaseOrder)] = (PermissionKeys.CanDeleteForeignPurchaseOrder, PermissionKeys.CanDeleteLocalPurchaseOrder),
            [nameof(ProcurementController.CheckPurchaseOrder)] = (PermissionKeys.CanCheckForeignPurchaseOrder, PermissionKeys.CanCheckLocalPurchaseOrder),
            [nameof(ProcurementController.SendPurchaseOrderToSupplier)] = (PermissionKeys.CanSendApprovedForeignPurchaseOrder, PermissionKeys.CanSendApprovedLocalPurchaseOrder),
        };
        foreach (var (name, keys) in orders)
        {
            var attribute = Assert.Single(typeof(ProcurementController).GetMethods().Where(method => method.Name == name)
                .SelectMany(method => method.GetCustomAttributes<PurchaseOrderPermissionAttribute>()));
            Assert.Equal(keys, (attribute.ForeignKey, attribute.LocalKey));
        }
        Assert.Contains(GrantPurchaseOrderCancel.Grants, grant => grant == (PermissionKeys.CanCancelForeignPurchaseOrder, PermissionKeys.CanReviseForeignPurchaseOrder));
        Assert.DoesNotContain(GrantPurchaseOrderCancel.Grants, grant => grant.Key == PermissionKeys.CanDeleteForeignPurchaseOrder);
    }
}
