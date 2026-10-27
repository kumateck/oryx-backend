using API.Filters;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;
namespace APP.Tests.Reporting;

public class ReportHistoryAuthorizationTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    { public Guid? UserId => id; public Guid? DepartmentId => null; public string DepartmentType => "NonProduction"; }
    private sealed class Endpoint
    { [ReportHistory(PermissionKeys.CanViewHumanResourcesReport, RequireForCurrent = true)] public void Read() { } }
    [Theory]
    [InlineData(false,false,403,false)]
    [InlineData(false,false,403,true)]
    [InlineData(true,true,403,false)]
    [InlineData(true,true,403,true)]
    [InlineData(true,false,409,false)]
    public async Task History_requires_live_enabled_grants_and_never_falls_back_to_current_data(bool granted,bool disabled,int expected,bool current)
    {
        var actor=new Actor(Guid.NewGuid());
        await using var database=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,actor);
        database.Users.Add(new User {Id=actor.UserId ?? throw new InvalidOperationException(),UserName="Administrator",IsDisabled=disabled});
        if(granted)
        {
            var role=new Role {Id=Guid.NewGuid(),Name="Report reader"}; database.Roles.Add(role);
            database.UserRoles.Add(new IdentityUserRole<Guid> {UserId=actor.UserId.Value,RoleId=role.Id});
            database.RoleClaims.Add(new IdentityRoleClaim<Guid> {Id=9001,RoleId=role.Id,ClaimType=AppConstants.Permission,ClaimValue=PermissionKeys.CanViewHumanResourcesReport});
            database.PermissionTypes.Add(new PermissionType {Id=Guid.NewGuid(),RoleClaimId=9001,Key=PermissionKeys.CanViewHumanResourcesReport,Type="View"});
        }
        await database.SaveChangesAsync();
        var http=new DefaultHttpContext(); http.Request.Method="GET"; http.Request.QueryString=new QueryString(current ? "" : "?asOfDate=2026-10-01T23:59:59Z");
        var descriptor=new ControllerActionDescriptor {MethodInfo=typeof(Endpoint).GetMethod(nameof(Endpoint.Read)) ?? throw new InvalidOperationException()};
        var controller=new Endpoint(); var filters=new List<IFilterMetadata>();
        var action=new ActionExecutingContext(new ActionContext(http,new RouteData(),descriptor),filters,new Dictionary<string,object?>(),controller);
        var called=false;
        await new ReportHistoryFilter(database,actor).OnActionExecutionAsync(action,()=>{called=true;return Task.FromResult(new ActionExecutedContext(action,filters,controller));});
        Assert.False(called);
        var result=Assert.IsAssignableFrom<IStatusCodeActionResult>(action.Result);
        Assert.Equal(expected,result.StatusCode);
    }
}
