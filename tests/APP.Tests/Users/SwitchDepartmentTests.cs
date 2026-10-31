using System.Text.Json;
using API.Controllers;
using APP.Repository;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Users.Request;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Users;

public sealed class SwitchDepartmentTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task ClearSavesNullForTheCallerAndInvalidatesTheirTokenCache()
    {
        var caller = Guid.NewGuid();
        await using var context = Context(caller);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var department = Seed(context, caller);
        var other = User(Guid.NewGuid(), department.Id);
        context.Users.Add(other);
        await context.SaveChangesAsync();
        cache.Set("caller-token", "cached-caller");
        cache.Set("other-token", "cached-other");
        var controller = Controller(context, cache, caller, "caller-token");

        var result = await controller.SetDepartment(new SwitchDepartmentRequest { DepartmentId = null });

        Assert.Equal(StatusCodes.Status204NoContent, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        context.ChangeTracker.Clear();
        var saved = await context.Users.Include(user => user.Department).SingleAsync(user => user.Id == caller);
        Assert.Null(saved.DepartmentId);
        Assert.Null(saved.Department);
        Assert.Equal(department.Id, (await context.Users.SingleAsync(user => user.Id == other.Id)).DepartmentId);
        Assert.False(cache.TryGetValue("caller-token", out object? _));
        Assert.True(cache.TryGetValue("other-token", out object? _));
    }

    [Fact]
    public async Task AnUnassignedUserCanChooseADepartmentThroughTheLegacyUrl()
    {
        var caller = Guid.NewGuid();
        await using var context = Context(caller);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var department = Seed(context, caller, assigned: false);
        await context.SaveChangesAsync();
        var result = await Controller(context, cache, caller, "caller-token").SwitchDepartment(department.Id);
        Assert.Equal(StatusCodes.Status200OK, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
        context.ChangeTracker.Clear();
        Assert.Equal(department.Id, (await context.Users.SingleAsync(user => user.Id == caller)).DepartmentId);
    }

    [Fact]
    public async Task ARepeatedClearIsSafe()
    {
        var caller = Guid.NewGuid();
        await using var context = Context(caller);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        Seed(context, caller, assigned: false);
        await context.SaveChangesAsync();
        var result = await Repository(context, cache).SwitchDepartment(null, caller, "caller-token");
        Assert.True(result.IsSuccess);
        Assert.Null((await context.Users.SingleAsync(user => user.Id == caller)).DepartmentId);
    }

    [Fact]
    public async Task InvalidDepartmentKeepsTheAssignmentAndCachedSession()
    {
        var caller = Guid.NewGuid();
        await using var context = Context(caller);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var department = Seed(context, caller);
        await context.SaveChangesAsync();
        cache.Set("caller-token", "cached-caller");
        var result = await Repository(context, cache).SwitchDepartment(Guid.NewGuid(), caller, "caller-token");
        Assert.False(result.IsSuccess);
        Assert.Equal("Department.NotFound", result.Error.Code);
        Assert.Equal(department.Id, (await context.Users.SingleAsync(user => user.Id == caller)).DepartmentId);
        Assert.True(cache.TryGetValue("caller-token", out object? _));
    }

    [Fact]
    public async Task ClearWithoutASubjectOrBearerTokenDoesNotWrite()
    {
        var caller = Guid.NewGuid();
        await using var context = Context(caller);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var department = Seed(context, caller);
        await context.SaveChangesAsync();
        var controller = Controller(context, cache, caller, "");
        var noToken = await controller.SetDepartment(new SwitchDepartmentRequest { DepartmentId = null });
        Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsAssignableFrom<IStatusCodeHttpResult>(noToken).StatusCode);
        controller.HttpContext.Items.Remove("Sub");
        controller.HttpContext.Request.Headers.Authorization = "Bearer caller-token";
        var noSubject = await controller.SetDepartment(new SwitchDepartmentRequest { DepartmentId = null });
        Assert.Equal(StatusCodes.Status401Unauthorized, Assert.IsAssignableFrom<IStatusCodeHttpResult>(noSubject).StatusCode);
        Assert.Equal(department.Id, (await context.Users.SingleAsync(user => user.Id == caller)).DepartmentId);
    }

    [Fact]
    public void NullIsExplicitAndAnOmittedDepartmentIsNotAClearRequest()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var request = JsonSerializer.Deserialize<SwitchDepartmentRequest>("""{"departmentId":null}""", options);
        Assert.NotNull(request);
        Assert.Null(request.DepartmentId);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SwitchDepartmentRequest>("{}", options));
    }

    private static ApplicationDbContext Context(Guid caller) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new Actor(caller));

    private static User User(Guid id, Guid? departmentId) => new()
    {
        Id = id, UserName = id.ToString(), FirstName = "Test", LastName = "User", Title = "",
        Avatar = "", Signature = "", DepartmentId = departmentId
    };

    private static Department Seed(ApplicationDbContext context, Guid caller, bool assigned = true)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "Quality", Code = "QA", Description = "Quality" };
        context.Departments.Add(department);
        context.Users.Add(User(caller, assigned ? department.Id : null));
        return department;
    }

    private static UserRepository Repository(ApplicationDbContext context, IMemoryCache cache) =>
        new(context, null!, null!, null!, null!, cache);

    private static UserController Controller(ApplicationDbContext context, IMemoryCache cache, Guid caller, string token)
    {
        var http = new DefaultHttpContext();
        http.Items["Sub"] = caller.ToString();
        if (token.Length > 0) http.Request.Headers.Authorization = $"Bearer {token}";
        return new UserController(Repository(context, cache)) { ControllerContext = new ControllerContext { HttpContext = http } };
    }
}
