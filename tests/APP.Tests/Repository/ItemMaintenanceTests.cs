using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Vendors;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ItemMaintenanceTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid Unit = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new CurrentUser());
    private static ItemRepository Repository(ApplicationDbContext c)
    {
        var map = new MapperConfiguration(m => {
            m.CreateMap<CreateItemsRequest, Item>();
            m.CreateMap<ItemTransactionLog, ItemTransactionLogDto>();
        });
        return new(c, map.CreateMapper());
    }
    private static CreateItemsRequest Request() => new() {
        Name = "Gloves", Code = "GLV001", UnitOfMeasureId = Unit,
        Store = Store.ItStore, Classification = InventoryClassification.NonRecoverable,
        IsActive = true, Description = "Disposable gloves"
    };
    private static async Task<Item> Seed(ApplicationDbContext c)
    {
        c.UnitOfMeasures.Add(new UnitOfMeasure { Id = Unit, Name = "Pieces", Symbol = "pcs" });
        var item = new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV001",
            UnitOfMeasureId = Unit, Store = Store.ItStore, Classification = InventoryClassification.NonRecoverable, IsActive = true };
        c.Items.Add(item);
        await c.SaveChangesAsync();
        return item;
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task InvalidReferenceOrDomainValueDoesNotCreateAnItem(int invalid)
    {
        await using var c = Context();
        await Seed(c);
        var request = Request(); request.Code = "NEW"; request.Name = "New gloves";
        if (invalid == 0) request.Store = (Store)99;
        if (invalid == 1) request.Classification = (InventoryClassification)99;
        if (invalid == 2) request.UnitOfMeasureId = Guid.NewGuid();
        if (invalid == 3) request.ItemCategoryId = Guid.NewGuid();
        if (invalid == 4) request.ReorderLevel = -1;
        Assert.False((await Repository(c).CreateItem(request)).IsSuccess);
        Assert.Single(await c.Items.ToListAsync());
    }

    [Fact]
    public async Task HistoricalCodeCannotBeReusedWithinTheSameStore()
    {
        await using var c = Context(); var row = await Seed(c);
        row.DeletedAt = DateTime.UtcNow; await c.SaveChangesAsync();
        var request = Request(); request.Code = " glv001 "; request.Name = "Other item";
        Assert.False((await Repository(c).CreateItem(request)).IsSuccess);
    }

    [Fact]
    public async Task NewCodesCannotCreateCrossStoreLedgerCollisions()
    {
        await using var c = Context(); await Seed(c);
        var request = Request(); request.Store = Store.GeneralStore; request.Name = "Other gloves";
        Assert.False((await Repository(c).CreateItem(request)).IsSuccess);
    }

    [Fact]
    public async Task DuplicateNameOnUpdateIsRefused()
    {
        await using var c = Context(); var row = await Seed(c);
        c.Items.Add(new Item { Id = Guid.NewGuid(), Name = "Other item", Code = "OTHER", Store = Store.ItStore });
        await c.SaveChangesAsync();
        var request = Request(); request.Name = " other item ";
        Assert.False((await Repository(c).UpdateItem(row.Id, request)).IsSuccess);
        Assert.Equal("Gloves", row.Name);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task ReferencedIdentityCannotChangeButDescriptionCan(int field)
    {
        await using var c = Context(); var row = await Seed(c);
        c.VendorItems.Add(new VendorItem { Id = Guid.NewGuid(), ItemId = row.Id, DeletedAt = DateTime.UtcNow });
        var otherUnit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Boxes" }; c.Add(otherUnit);
        await c.SaveChangesAsync();
        var request = Request();
        if (field == 0) request.Code = "RENAMED";
        if (field == 1) request.Store = Store.GeneralStore;
        if (field == 2) request.Classification = InventoryClassification.Recoverable;
        if (field == 3) request.UnitOfMeasureId = otherUnit.Id;
        Assert.False((await Repository(c).UpdateItem(row.Id, request)).IsSuccess);
        Assert.True((await Repository(c).UpdateItem(row.Id, Request())).IsSuccess);
        Assert.Equal("Disposable gloves", row.Description);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task StockOrHistoricalReferencesPreventDeletion(bool stock)
    {
        await using var c = Context(); var row = await Seed(c);
        if (stock) row.AvailableQuantity = 1;
        else c.VendorItems.Add(new VendorItem { Id = Guid.NewGuid(), ItemId = row.Id, DeletedAt = DateTime.UtcNow });
        await c.SaveChangesAsync();
        Assert.False((await Repository(c).DeleteItem(row.Id, Actor)).IsSuccess);
        Assert.Null(row.DeletedAt);
    }

    [Fact]
    public async Task UnusedItemDeletionRecordsActorAndPreservesHistory()
    {
        await using var c = Context(); var row = await Seed(c);
        Assert.True((await Repository(c).DeleteItem(row.Id, Actor)).IsSuccess);
        Assert.Empty(await c.Items.ToListAsync());
        var historical = await c.Items.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(Actor, historical.LastDeletedById); Assert.False(historical.IsActive);
    }

    [Fact]
    public async Task CodeSharedAcrossStoresCannotReturnMixedLedger()
    {
        await using var c = Context(); await Seed(c);
        c.Items.Add(new Item { Id = Guid.NewGuid(), Name = "Other gloves", Code = "GLV001", Store = Store.GeneralStore });
        c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV001", Credit = 1 });
        await c.SaveChangesAsync();
        Assert.False((await Repository(c).GetItemTransactions("GLV001", null)).IsSuccess);
    }

    [Fact]
    public async Task InvalidTransactionFilterIsRefusedAndZeroFilterIsPreserved()
    {
        await using var c = Context(); await Seed(c);
        c.ItemTransactionLogs.AddRange(
            new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV001", TransactionType = TransactionType.Damaged },
            new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV001", TransactionType = TransactionType.Purchased });
        await c.SaveChangesAsync();
        Assert.False((await Repository(c).GetItemTransactions("GLV001", "99")).IsSuccess);
        Assert.Single((await Repository(c).GetItemTransactions("GLV001", "0")).Value);
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
