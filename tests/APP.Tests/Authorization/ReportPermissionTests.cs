using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Report pages: every report the report pages use needs its family's key on the server (audit phase 4a).</summary>
public sealed class ReportPermissionTests
{
    private static string Policy(string route)
    {
        var action = Assert.Single(typeof(ReportController).GetMethods(),
            method => method.GetCustomAttributes<HttpGetAttribute>().Any(get => get.Template == route));
        return Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy;
    }

    [Theory]
    [InlineData("hr/employee-master-list", PermissionKeys.CanViewHumanResourcesReport)]
    [InlineData("staff-turnover-report", PermissionKeys.CanViewHumanResourcesReport)]
    [InlineData("warehouse/materials-stock-summary", PermissionKeys.CanViewWarehouseReport)]
    [InlineData("expired-materials", PermissionKeys.CanViewWarehouseReport)]
    [InlineData("purchase-orders", PermissionKeys.CanViewProcurementReport)]
    [InlineData("invoiced-products-summary", PermissionKeys.CanViewFgtnReport)]
    [InlineData("vendor-item/summary", PermissionKeys.CanViewStoreAndInventoryReport)]
    [InlineData("bmr-issued", PermissionKeys.CanViewQcAndQaReport)]
    [InlineData("production-readiness", PermissionKeys.CanViewQcAndQaReport)]
    public void A_report_needs_its_family_key(string route, string key) => Assert.Equal(key, Policy(route));

    private static MethodInfo Action(string route) => Assert.Single(typeof(ReportController).GetMethods(),
        method => method.GetCustomAttributes<HttpGetAttribute>().Any(get => get.Template == route));

    [Theory]
    [InlineData("human-resource", "CanViewHrDashboard")]
    [InlineData("hr-kpi/employee-gender-ratio", "CanViewHrDashboard,CanViewHrExecutiveSummary")]
    [InlineData("hr-kpi/employee-headcount", "CanViewHrExecutiveSummary")]
    [InlineData("kpi/fgtn-pending-approval", "CanViewProductionHealthDashboard,CanViewFgtnDashboard")]
    [InlineData("product-stock-summary", "CanViewFgtnDashboard,CanViewFgtnReport")]
    [InlineData("warehouse-kpi/expiry-risk-index", "CanViewWarehouseDashboard,CanViewInventoryCommandCenter")]
    [InlineData("warehouse/slow-moving-inventory", "CanViewInventoryCommandCenter")]
    [InlineData("shipment", "CanViewLogisticsDashboard,CanViewProcurementReport")]
    [InlineData("qa-dashboard", "CanViewQcDashboard,CanViewQaDashboard")]
    [InlineData("general-inventory-dashboard", "CanViewStoresDashboard")]
    [InlineData("services-dashboard", "CanViewServicesDashboard")]
    public void A_dashboard_figure_needs_the_key_of_a_page_that_shows_it(string route, string keys)
    {
        var action = Action(route);
        Assert.Empty(action.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(keys.Split(','), Assert.Single(action.GetCustomAttributes<AnyPermissionAttribute>()).Keys);
    }

    [Fact]
    public void Every_report_route_checks_a_key()
    {
        var open = typeof(ReportController).GetMethods().Where(method => method.GetCustomAttributes<HttpGetAttribute>().Any())
            .Where(method => !method.GetCustomAttributes<AuthorizeAttribute>().Any() && !method.GetCustomAttributes<AnyPermissionAttribute>().Any())
            .Select(method => method.Name).ToList();
        Assert.Empty(open);
    }

    [Fact]
    public void Every_dashboard_key_is_registered_and_granted_from_registered_keys()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        var used = typeof(ReportController).GetMethods().SelectMany(method => method.GetCustomAttributes<AnyPermissionAttribute>()).SelectMany(attribute => attribute.Keys).ToHashSet();
        Assert.All(used, key => Assert.Contains(key, registered));
        Assert.All(GrantDashboardViews.Grants, grant => Assert.Contains(grant.From, registered));
        var granted = GrantDashboardViews.Grants.Select(grant => grant.Key).ToHashSet();
        Assert.Equal(12, granted.Count);
        Assert.Subset(used, granted);
    }

    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static int _claimId = 9500;

    private static async Task<bool> Allowed(AnyPermissionAttribute attribute, params string[] keys)
    {
        var user = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(user));
        var role = new Role { Id = Guid.NewGuid(), Name = "role", NormalizedName = "ROLE", DisplayName = "role" };
        db.Roles.Add(role);
        db.Users.Add(new User { Id = user, UserName = "u", Email = "u@example.com", FirstName = "U", LastName = "U" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        foreach (var key in keys)
        {
            var claim = new IdentityRoleClaim<Guid> { Id = Interlocked.Increment(ref _claimId), RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        await db.SaveChangesAsync();
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
        http.Items["Sub"] = user.ToString();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
        var ran = false;
        await attribute.OnActionExecutionAsync(executing, () =>
        {
            ran = true;
            return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
        });
        if (!ran) Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
        return ran;
    }

    [Fact]
    public async Task Any_one_of_the_listed_keys_opens_a_shared_figure()
    {
        var shared = Assert.Single(Action("qa-dashboard").GetCustomAttributes<AnyPermissionAttribute>());
        Assert.True(await Allowed(shared, PermissionKeys.CanViewQaDashboard));
        Assert.True(await Allowed(shared, PermissionKeys.CanViewQcDashboard));
        // An old page key no longer opens the dashboard by itself.
        Assert.False(await Allowed(shared, PermissionKeys.CanViewAnalyticalTestRequests));
        Assert.False(await Allowed(shared));
    }
}
