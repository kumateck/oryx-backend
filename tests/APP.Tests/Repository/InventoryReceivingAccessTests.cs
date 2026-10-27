using API.Controllers;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;
public class InventoryReceivingAccessTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public async Task ControllerRequiresCurrentExactStoreAccessAndADistinctAuditedCreator(int scenario)
    {
        var actor = Guid.NewGuid();
        Guid? creator = scenario == 1 ? actor : scenario == 2 ? null : Guid.NewGuid();
        var identity = new CurrentUser { Id = creator };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new ApplicationDbContext(options, identity);
        context.Users.Add(new User { Id = actor, UserName = "reviewer" });
        if (creator.HasValue && creator != actor) context.Users.Add(new User { Id = creator.Value, UserName = "creator" });
        var role = Guid.NewGuid(); context.Roles.Add(new Role { Id = role, Name = "Super account" });
        context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor, RoleId = role });
        var name = scenario == 5 ? "General" : "It";
        var keys = new List<string>();
        if (scenario != 3) keys.Add($"CanView{name}StoreReceivingArea");
        if (scenario != 4) keys.Add($"CanApprove{name}StoreReceivingAreaEntry");
        foreach (var key in keys) {
            var claim = new IdentityRoleClaim<Guid> { RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key };
            context.RoleClaims.Add(claim); await context.SaveChangesAsync();
            context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Access" });
        }
        var item = new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV", Store = Store.ItStore, AvailableQuantity = 10, IsActive = true };
        var entry = new StockEntry { Id = Guid.NewGuid(), Item = item, Status = ApprovalStatus.Pending, Quantity = 5 };
        context.StockEntries.Add(entry);
        context.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV", Credit = 10, TotalBalance = 10 });
        await context.SaveChangesAsync(); identity.Id = actor;
        var repo = new InventoryProcurementRepository(context, new MapperConfiguration(m => m.CreateMap<StockEntry, StockEntryDto>()).CreateMapper(), null, null);
        var controller = new InventoryProcurementController(repo, context) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.HttpContext.Items["Sub"] = actor.ToString();
        var result = await controller.ApproveItem(entry.Id);
        if (scenario == 0) {
            Assert.IsType<NoContent>(result); Assert.Equal(ApprovalStatus.Approved, entry.Status); Assert.Equal(15, item.AvailableQuantity);
        } else {
            Assert.IsType<ForbidHttpResult>(result); Assert.Equal(ApprovalStatus.Pending, entry.Status); Assert.Equal(10, item.AvailableQuantity);
            Assert.Single(await context.ItemTransactionLogs.ToListAsync());
        }
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? Id { get; set; } public Guid? UserId => Id;
        public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
