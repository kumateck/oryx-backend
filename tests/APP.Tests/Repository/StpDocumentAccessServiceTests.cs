using System.Security.Claims;
using APP.Services.StpDocuments;
using APP.Utils;
using DOMAIN.Entities.StpDocuments;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StpDocumentAccessServiceTests
{
    [Fact]
    public async Task Product_document_write_requires_product_edit_permission()
    {
        await using var context = CreateContext();
        var document = new StpDocument
        {
            Id = Guid.NewGuid(), OwnerType = "ProductStandardTestProcedure", OwnerId = Guid.NewGuid()
        };
        context.StpDocuments.Add(document);
        await context.SaveChangesAsync();

        var denied = new StpDocumentAccessService(context, new TestAuthorizationService([]));
        var allowed = new StpDocumentAccessService(
            context,
            new TestAuthorizationService([PermissionKeys.CanEditProductStp]));

        Assert.False(await denied.CanAccessDocument(new ClaimsPrincipal(), document.Id, true));
        Assert.True(await allowed.CanAccessDocument(new ClaimsPrincipal(), document.Id, true));
    }

    [Fact]
    public async Task Draft_editor_requires_write_but_approved_editor_requires_read()
    {
        await using var context = CreateContext();
        var document = new StpDocument
        {
            Id = Guid.NewGuid(), OwnerType = "ProductStandardTestProcedure", OwnerId = Guid.NewGuid()
        };
        context.StpDocuments.Add(document);
        await context.SaveChangesAsync();
        var service = new StpDocumentAccessService(
            context,
            new TestAuthorizationService([PermissionKeys.CanViewProductStps]));

        Assert.False(await service.CanOpenEditor(new ClaimsPrincipal(), document.Id));
        document.Status = StpDocumentStatus.Approved;
        await context.SaveChangesAsync();
        Assert.True(await service.CanOpenEditor(new ClaimsPrincipal(), document.Id));
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new AccessTestUser());
}

file sealed class TestAuthorizationService(HashSet<string> permissions) : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
        Task.FromResult(AuthorizationResult.Failed());

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user, object? resource, string policyName) =>
        Task.FromResult(permissions.Contains(policyName)
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed());
}

file sealed class AccessTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}
