using API.Controllers;
using API.Filters;
using APP.Services.Authorization;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

public class ReportDepartmentScopeTests
{
    private static readonly Guid Own = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private sealed class Actor(Guid id) : ICurrentUserService
    { public Guid? UserId => id; public Guid? DepartmentId => null; public string DepartmentType => ""; }

    private sealed class Endpoint
    {
        [ReportDepartmentScope] public void ById(Guid? departmentId) { }
        [ReportDepartmentScope] public void ByFilter(MovementReportFilter filter) { }
        [ReportDepartmentScope] public void ByName(string departmentName) { }
        public void Unmarked(Guid? departmentId) { }
    }

    [Theory]
    [InlineData(DepartmentType.Production, true)]
    [InlineData(DepartmentType.NonProduction, false)]
    [InlineData(DepartmentType.RnD, false)]
    public void Only_production_departments_are_restricted(DepartmentType type, bool restricted)
    {
        var actor = new ReportDepartmentScope.Actor(Own, type, "Oral solids");
        Assert.Equal(restricted, ReportDepartmentScope.Resolve(actor, Other).IsFailure);
        Assert.Equal(restricted ? Own : null, ReportDepartmentScope.Resolve(actor, null).Value);
        Assert.Equal(Own, ReportDepartmentScope.Resolve(actor, Own).Value);
        Assert.Equal(restricted, ReportDepartmentScope.ResolveName(actor, "Liquids").IsFailure);
        Assert.True(ReportDepartmentScope.ResolveName(actor, " oral SOLIDS ").IsSuccess);
        Assert.Equal(Other, ReportDepartmentScope.Resolve(new ReportDepartmentScope.Actor(null, null, null), Other).Value);
    }

    [Theory]
    [InlineData(nameof(Endpoint.ById))]
    [InlineData(nameof(Endpoint.ByFilter))]
    [InlineData(nameof(Endpoint.ByName))]
    public async Task A_production_user_cannot_read_another_department(string action)
    {
        var (context, called) = await Run(DepartmentType.Production, action, action == nameof(Endpoint.ByName) ? "Liquids"
            : action == nameof(Endpoint.ByFilter) ? new MovementReportFilter { DepartmentId = Other } : Other);
        Assert.False(called);
        Assert.Equal(403, Assert.IsAssignableFrom<IStatusCodeActionResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task A_production_user_asking_for_all_departments_gets_their_own()
    {
        var (byId, calledById) = await Run(DepartmentType.Production, nameof(Endpoint.ById), null);
        Assert.True(calledById); Assert.Equal(Own, byId.ActionArguments["departmentId"]);
        var (byFilter, _) = await Run(DepartmentType.Production, nameof(Endpoint.ByFilter), new MovementReportFilter());
        Assert.Equal(Own, Assert.IsType<MovementReportFilter>(byFilter.ActionArguments["filter"]).DepartmentId);
        var (byName, _) = await Run(DepartmentType.Production, nameof(Endpoint.ByName), null);
        Assert.Equal("Oral solids", byName.ActionArguments["departmentName"]);
    }

    [Theory]
    [InlineData(DepartmentType.NonProduction)]
    [InlineData(DepartmentType.RnD)]
    public async Task A_non_production_user_can_pick_any_department_or_all(DepartmentType type)
    {
        var (picked, called) = await Run(type, nameof(Endpoint.ById), Other);
        Assert.True(called); Assert.Equal(Other, picked.ActionArguments["departmentId"]);
        var (all, _) = await Run(type, nameof(Endpoint.ById), null);
        Assert.False(all.ActionArguments.ContainsKey("departmentId"));
        var (filter, _) = await Run(type, nameof(Endpoint.ByFilter), new MovementReportFilter { DepartmentId = Other });
        Assert.Equal(Other, Assert.IsType<MovementReportFilter>(filter.ActionArguments["filter"]).DepartmentId);
    }

    [Fact]
    public async Task Unmarked_endpoints_are_left_alone()
    {
        var (context, called) = await Run(DepartmentType.Production, nameof(Endpoint.Unmarked), Other);
        Assert.True(called); Assert.Equal(Other, context.ActionArguments["departmentId"]);
    }

    [Theory]
    [InlineData(typeof(ReportController), nameof(ReportController.GetMaterialsStockSummary))]
    [InlineData(typeof(ReportController), nameof(ReportController.GetEmployeeMasterList))]
    [InlineData(typeof(ReportController), nameof(ReportController.GetIssuedBmrReport))]
    [InlineData(typeof(MaterialController), nameof(MaterialController.GetMaterialStockReport))]
    [InlineData(typeof(AttendanceRecordController), nameof(AttendanceRecordController.DepartmentDailySummaryAttendance))]
    public void Report_endpoints_with_a_department_use_the_shared_rule(Type controller, string action)
    {
        var method = Assert.Single(controller.GetMethods(), item => item.Name == action);
        Assert.NotNull(method.GetCustomAttributes(typeof(ReportDepartmentScopeAttribute), true).SingleOrDefault());
    }

    private static async Task<(ActionExecutingContext Context, bool Called)> Run(DepartmentType type, string action, object? argument)
    {
        var userId = Guid.NewGuid();
        await using var database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(userId));
        database.Users.Add(new User { Id = userId, UserName = "reader", Department = new Department { Id = Own, Name = "Oral solids", Type = type } });
        await database.SaveChangesAsync();
        var method = typeof(Endpoint).GetMethod(action) ?? throw new InvalidOperationException();
        var parameter = method.GetParameters().Single();
        var descriptor = new ControllerActionDescriptor
        {
            MethodInfo = method,
            Parameters = [new ParameterDescriptor { Name = parameter.Name!, ParameterType = parameter.ParameterType }],
        };
        var arguments = new Dictionary<string, object?>();
        if (argument is not null) arguments[parameter.Name!] = argument;
        var filters = new List<IFilterMetadata>(); var controller = new Endpoint();
        var context = new ActionExecutingContext(new ActionContext(new DefaultHttpContext(), new RouteData(), descriptor), filters, arguments, controller);
        var called = false;
        await new ReportDepartmentScopeFilter(database, new Actor(userId)).OnActionExecutionAsync(context,
            () => { called = true; return Task.FromResult(new ActionExecutedContext(context, filters, controller)); });
        return (context, called);
    }
}
