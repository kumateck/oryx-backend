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
    [InlineData("hr/employee-master-list", PermissionKeys.CanViewEmployeeMasterListReport)]
    [InlineData("staff-report", PermissionKeys.CanViewStaffGradeReport)]
    [InlineData("employee-movement", PermissionKeys.CanViewNewHiresAndExitsReport)]
    [InlineData("staff-total-report", PermissionKeys.CanViewStaffTotalReport)]
    [InlineData("staff-gender-ratio-report", PermissionKeys.CanViewStaffGenderRatioReport)]
    [InlineData("staff-leave-report", PermissionKeys.CanViewLeaveOverviewReport)]
    [InlineData("staff-turnover-report", PermissionKeys.CanViewStaffTurnoverReport)]
    [InlineData("warehouse/materials-stock-summary", PermissionKeys.CanViewMaterialStockSummaryReport)]
    [InlineData("warehouse/materials-stock-batch-detail", PermissionKeys.CanViewMaterialStockDetailReport)]
    [InlineData("warehouse/inventory-valuation-detail", PermissionKeys.CanViewInventoryValuationReport)]
    [InlineData("purchase-orders", PermissionKeys.CanViewPurchaseOrdersReport)]
    [InlineData("finished-goods-transfer-summary", PermissionKeys.CanViewFinishedGoodsTransferSummaryReport)]
    [InlineData("finished-goods-transfer-detailed", PermissionKeys.CanViewFinishedGoodsTransferDetailReport)]
    [InlineData("product-stock-detailed", PermissionKeys.CanViewProductStockDetailReport)]
    [InlineData("invoiced-products-summary", PermissionKeys.CanViewInvoicedProductsReport)]
    [InlineData("stock-summary-per-store-type", PermissionKeys.CanViewStoreStockReport)]
    [InlineData("vendor-item", PermissionKeys.CanViewVendorItemMappingDetailReport)]
    [InlineData("vendor-item/summary", PermissionKeys.CanViewVendorItemMappingSummaryReport)]
    [InlineData("bmr-issued", PermissionKeys.CanViewBmrIssuedReport)]
    [InlineData("bmr-issued/by-product", PermissionKeys.CanViewBmrIssuedByProductReport)]
    // Register reports outside the reports hub keep their group key.
    [InlineData("expired-materials", PermissionKeys.CanViewWarehouseReport)]
    public void A_report_needs_its_own_key(string route, string key)
    {
        Assert.Equal(key, Policy(route));
        Assert.Empty(Action(route).GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData("production-readiness", "CanViewMonthlyProductionScheduleReport,CanViewDailyProductionScheduleReport,CanViewProductReadinessReport")]
    [InlineData("supplier-materials", "CanViewApprovedManufacturerListReport,CanViewSupplierMappingSummaryReport,CanViewSupplierMappingDetailReport")]
    [InlineData("items-per-store-type", "CanViewStoreItemsReport,CanViewStoreStockReport,CanViewVendorItemMappingSummaryReport,CanViewVendorItemMappingDetailReport")]
    [InlineData("purchased-po-report", "CanViewProcurementDashboard,CanViewInvoicedPurchaseOrdersReport")]
    public void Reports_built_from_the_same_data_list_each_report_key(string route, string keys)
    {
        var action = Action(route);
        Assert.Empty(action.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(keys.Split(','), Assert.Single(action.GetCustomAttributes<AnyPermissionAttribute>()).Keys);
    }

    private static MethodInfo Action(string route) => Assert.Single(typeof(ReportController).GetMethods(),
        method => method.GetCustomAttributes<HttpGetAttribute>().Any(get => get.Template == route));

    [Theory]
    [InlineData("human-resource", "CanViewHrDashboard")]
    [InlineData("hr-kpi/employee-gender-ratio", "CanViewHrDashboard,CanViewHrExecutiveSummary")]
    [InlineData("hr-kpi/employee-headcount", "CanViewHrExecutiveSummary")]
    [InlineData("kpi/fgtn-pending-approval", "CanViewProductionHealthDashboard,CanViewFgtnDashboard")]
    [InlineData("product-stock-summary", "CanViewFgtnDashboard,CanViewProductStockSummaryReport")]
    [InlineData("warehouse-kpi/expiry-risk-index", "CanViewWarehouseDashboard,CanViewInventoryCommandCenter")]
    [InlineData("warehouse/slow-moving-inventory", "CanViewInventoryCommandCenter")]
    [InlineData("shipment", "CanViewLogisticsDashboard,CanViewShipmentsReport")]
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

    [Theory]
    [InlineData("daily-summary", PermissionKeys.CanViewDailyAttendanceReport)]
    [InlineData("general-summary", PermissionKeys.CanViewDepartmentAttendanceReport)]
    public void Attendance_reports_open_with_their_own_key_or_the_attendance_summary_key(string route, string key)
    {
        var action = Assert.Single(typeof(AttendanceRecordController).GetMethods(),
            method => method.GetCustomAttributes<HttpGetAttribute>().Any(get => get.Template == route));
        Assert.Equal([key, PermissionKeys.CanViewAttendanceReportUpload], Assert.Single(action.GetCustomAttributes<AnyPermissionAttribute>()).Keys);
    }

    [Fact]
    public void Every_hub_report_key_is_registered_once_under_reports()
    {
        var catalog = PermissionUtils.GeneratePermissions().ToList();
        Assert.Equal(38, ReportPermissions.Hub.Count);
        Assert.Equal(ReportPermissions.Hub.Count, ReportPermissions.Hub.Distinct().Count());
        Assert.All(ReportPermissions.Hub, key =>
            Assert.Equal(PermissionModules.Reports, Assert.Single(catalog, permission => permission.Key == key).Module));
    }

    [Fact]
    public void The_migration_grants_every_hub_report_key_from_one_group_key()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        var pairs = System.Text.RegularExpressions.Regex.Matches(
                (string)typeof(GrantPerReportViewKeys).GetField("Pairs", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!,
                @"\('(\w+)', '(\w+)'\)")
            .Select(match => (Group: match.Groups[1].Value, Report: match.Groups[2].Value)).ToList();
        Assert.Equal(ReportPermissions.Hub.Order(), pairs.Select(pair => pair.Report).Order());
        Assert.All(pairs, pair => Assert.Contains(pair.Group, registered));
        Assert.Contains((PermissionKeys.CanViewProcurementReport, PermissionKeys.CanViewApprovedManufacturerListReport), pairs);
        Assert.Contains((PermissionKeys.CanViewQcAndQaReport, PermissionKeys.CanViewBmrIssuedReport), pairs);
    }

    [Fact]
    public async Task A_report_opens_with_its_own_key_and_not_with_a_neighbour_or_its_old_group_key()
    {
        var aml = Assert.Single(Action("supplier-materials").GetCustomAttributes<AnyPermissionAttribute>());
        Assert.True(await Allowed(aml, PermissionKeys.CanViewApprovedManufacturerListReport));
        Assert.False(await Allowed(aml, PermissionKeys.CanViewProcurementReport));
        Assert.False(await Allowed(aml, PermissionKeys.CanViewPurchaseOrdersReport));
        Assert.False(await Allowed(aml));
    }

    [Fact]
    public async Task Filter_lookups_open_for_any_one_report_key()
    {
        var materials = typeof(MaterialController).GetMethods()
            .Single(method => method.Name == nameof(MaterialController.GetMaterials) && method.GetParameters().Length > 0);
        var lookup = Assert.Single(materials.GetCustomAttributes<AnyPermissionAttribute>());
        Assert.True(lookup.OrAnyHubReport);
        Assert.True(await Allowed(lookup, PermissionKeys.CanViewApprovedManufacturerListReport));
        Assert.True(await Allowed(lookup, PermissionKeys.CanViewStaffTurnoverReport));
        Assert.False(await Allowed(lookup, PermissionKeys.CanViewHrDashboard));
    }
}
