using APP.Repository;
using APP.Services.Stores;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class StoreStockRequisitionTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Department = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());

    private static async Task<(Item gloves, Item cable)> Seed(ApplicationDbContext c, Store cableStore = Store.ItStore)
    {
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "pc" };
        c.Add(unit);
        c.Users.Add(new User { Id = Actor, UserName = "storekeeper", FirstName = "Ama", LastName = "Owusu" });
        c.Departments.Add(new Department { Id = Department, Name = "Quality" });
        var gloves = new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV", Store = Store.ItStore, IsActive = true, AvailableQuantity = 10, UnitOfMeasureId = unit.Id };
        var cable = new Item { Id = Guid.NewGuid(), Name = "Cable", Code = "CBL", Store = cableStore, IsActive = true, AvailableQuantity = 4, UnitOfMeasureId = unit.Id };
        c.AddRange(gloves, cable);
        c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV", Credit = 10, TotalBalance = 10 });
        c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "CBL", Credit = 4, TotalBalance = 4 });
        await c.SaveChangesAsync();
        return (gloves, cable);
    }

    private static CreateItemStockRequisitionRequest Request(params (Item item, int quantity)[] lines) => new()
    {
        Number = "SR-1", DepartmentId = Department, RequestedById = Guid.NewGuid(), Justification = "Lab use",
        StockItems = lines.Select(l => new StockItems { ItemId = l.item.Id, QuantityRequested = l.quantity }).ToList()
    };

    private static IssueStockAgainstRequisitionRequest Issue(params (Item item, int quantity)[] lines) => new()
    {
        QuantitiesToIssue = lines.Select(l => new IssueQuantityDto { ItemId = l.item.Id, Quantity = l.quantity }).ToList()
    };

    [Fact]
    public async Task TheRequesterIsTheSignedInUserAndARequisitionDrawsOnOneStore()
    {
        await using var c = Context(); var (gloves, cable) = await Seed(c, Store.GeneralStore);
        var repository = new ItemStockRequisitionRepository(c);
        Assert.False((await repository.CreateItemStockRequisition(Request((gloves, 2), (cable, 1)), Actor)).IsSuccess);
        var created = await repository.CreateItemStockRequisition(Request((gloves, 2)), Actor);
        Assert.True(created.IsSuccess);
        Assert.Equal(Actor, (await c.ItemStockRequisitions.SingleAsync()).RequestedById);
        Assert.False((await repository.CreateItemStockRequisition(Request((gloves, 1)), Actor)).IsSuccess);
    }

    [Fact]
    public async Task RepeatIssuesAreTrackedPerLineAndCannotExceedWhatWasRequested()
    {
        await using var c = Context(); var (gloves, cable) = await Seed(c);
        var repository = new ItemStockRequisitionRepository(c);
        var id = (await repository.CreateItemStockRequisition(Request((gloves, 5), (cable, 2)), Actor)).Value;

        Assert.True((await repository.IssueStock(id, Issue((gloves, 3)), Actor)).IsSuccess);
        var partial = (await repository.GetItemStockRequisition(id)).Value;
        Assert.Equal(IssueItemStockRequisitionStatus.Partial, partial.Status);
        Assert.Equal(Store.ItStore, partial.Store);
        var line = partial.RequisitionItems.Single(i => i.ItemId == gloves.Id);
        Assert.Equal(3, line.QuantityIssued); Assert.Equal(2, line.QuantityRemaining);

        // Before the repair a second issue ignored the first and could take another 5.
        var over = await repository.IssueStock(id, Issue((gloves, 3)), Actor);
        Assert.False(over.IsSuccess); Assert.Equal("Quantity.OverIssue", over.Error.Code);
        Assert.Equal(7, (await c.Items.SingleAsync(i => i.Id == gloves.Id)).AvailableQuantity);

        Assert.True((await repository.IssueStock(id, Issue((gloves, 2), (cable, 2)), Actor)).IsSuccess);
        var done = (await repository.GetItemStockRequisition(id)).Value;
        Assert.Equal(IssueItemStockRequisitionStatus.Completed, done.Status);
        Assert.All(done.RequisitionItems, i => Assert.Equal(0, i.QuantityRemaining));
        Assert.Equal(5, (await c.Items.SingleAsync(i => i.Id == gloves.Id)).AvailableQuantity);
        var postings = await c.ItemTransactionLogs.Where(l => l.TransactionType == TransactionType.Issued).ToListAsync();
        Assert.Equal(3, postings.Count); Assert.All(postings, p => Assert.Equal(Actor, p.CreatedById));
        Assert.All(await c.IssueItemStockRequisitions.ToListAsync(), x => Assert.NotNull(x.ItemStockRequisitionItemId));
    }

    [Fact]
    public async Task IssuesRespectStockTheLedgerAndTheRequisitionsOwnItems()
    {
        await using var c = Context(); var (gloves, cable) = await Seed(c);
        var repository = new ItemStockRequisitionRepository(c);
        var id = (await repository.CreateItemStockRequisition(Request((cable, 9)), Actor)).Value;
        Assert.Equal("Stock.Insufficient", (await repository.IssueStock(id, Issue((cable, 5)), Actor)).Error.Code);
        Assert.Equal("Request.InvalidItem", (await repository.IssueStock(id, Issue((gloves, 1)), Actor)).Error.Code);
        Assert.False((await repository.IssueStock(id, Issue((cable, -1)), Actor)).IsSuccess);
        cable.AvailableQuantity = 3; await c.SaveChangesAsync();
        Assert.Equal("Inventory.Unreconciled", (await repository.IssueStock(id, Issue((cable, 1)), Actor)).Error.Code);
        Assert.Empty(await c.IssueItemStockRequisitions.ToListAsync());
    }

    [Fact]
    public async Task AnIssuedRequisitionCannotBeEditedOrDeleted()
    {
        await using var c = Context(); var (gloves, _) = await Seed(c);
        var repository = new ItemStockRequisitionRepository(c);
        var id = (await repository.CreateItemStockRequisition(Request((gloves, 5)), Actor)).Value;
        Assert.True((await repository.UpdateItemStockRequisition(id, Request((gloves, 4)))).IsSuccess);
        Assert.True((await repository.IssueStock(id, Issue((gloves, 1)), Actor)).IsSuccess);
        Assert.Equal("ItemStockRequisition.Issued", (await repository.UpdateItemStockRequisition(id, Request((gloves, 1)))).Error.Code);
        Assert.Equal("ItemStockRequisition.Issued", (await repository.DeleteItemStockRequisition(id, Actor)).Error.Code);
    }

    [Fact]
    public async Task UnattributedHistoryBlocksFurtherIssues()
    {
        await using var c = Context(); var (gloves, cable) = await Seed(c);
        var repository = new ItemStockRequisitionRepository(c);
        var id = (await repository.CreateItemStockRequisition(Request((gloves, 5), (cable, 2)), Actor)).Value;
        c.IssueItemStockRequisitions.Add(new IssueItemStockRequisition { Id = Guid.NewGuid(), ItemStockRequisitionId = id, QuantityIssued = 2 });
        (await c.ItemStockRequisitions.SingleAsync()).Status = IssueItemStockRequisitionStatus.Partial;
        await c.SaveChangesAsync();
        Assert.False((await repository.GetItemStockRequisition(id)).Value.IssueHistoryAttributed);
        Assert.Equal("Requisition.Unattributed", (await repository.IssueStock(id, Issue((gloves, 1)), Actor)).Error.Code);
    }

    [Fact]
    public async Task ListsOnlyRequisitionsWhoseStoresAreReadable()
    {
        await using var c = Context(); var (gloves, cable) = await Seed(c, Store.GeneralStore);
        var repository = new ItemStockRequisitionRepository(c);
        await repository.CreateItemStockRequisition(Request((gloves, 1)), Actor);
        var general = Request((cable, 1)); general.Number = "SR-2";
        await repository.CreateItemStockRequisition(general, Actor);
        var page = (await repository.GetItemStockRequisitions(1, 10, null, [Store.ItStore], null, null)).Value;
        Assert.Equal(1, page.TotalRecordCount); Assert.Equal("SR-1", page.Data.Single().Number);
    }

    [Fact]
    public async Task WorkflowKeysNeedTheViewKeyAndCurrentGrants()
    {
        await using var c = Context(); await Seed(c);
        var role = Guid.NewGuid();
        c.Roles.Add(new Role { Id = role, Name = "Super account" });
        c.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor, RoleId = role });
        foreach (var key in new[] { "CanCreateItStoreIssueStock", "CanViewGeneralStoreIssueStocks", "CanCreateGeneralStoreIssueStock" })
        {
            var claim = new IdentityRoleClaim<Guid> { RoleId = role, ClaimType = AppConstants.Permission, ClaimValue = key };
            c.RoleClaims.Add(claim); await c.SaveChangesAsync();
            c.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Access" });
        }
        await c.SaveChangesAsync();
        Assert.False(await StoreWorkflowAccess.Allows(c, Actor, Store.ItStore, StoreWorkflow.IssueStock, StoreWorkflowAction.Create));
        Assert.True(await StoreWorkflowAccess.Allows(c, Actor, Store.GeneralStore, StoreWorkflow.IssueStock, StoreWorkflowAction.Create));
        Assert.False(await StoreWorkflowAccess.AllowsAll(c, Actor, [Store.GeneralStore, Store.ItStore], StoreWorkflow.IssueStock, StoreWorkflowAction.Create));
        Assert.Equal([Store.GeneralStore], await StoreWorkflowAccess.Readable(c, Actor, StoreWorkflow.IssueStock));
        Assert.False(await StoreWorkflowAccess.Allows(c, null, Store.GeneralStore, StoreWorkflow.IssueStock, StoreWorkflowAction.View));
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
