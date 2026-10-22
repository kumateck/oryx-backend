using APP.Repository;
using APP.Services.Inventory;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.StockEntries;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class InventoryReceivingTests
{
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private static InventoryProcurementRepository Repository(ApplicationDbContext c) => new(c,
        new MapperConfiguration(m => m.CreateMap<StockEntry, StockEntryDto>()).CreateMapper(), null, null);
    private static async Task<StockEntry> Seed(ApplicationDbContext c, int balance = 10)
    {
        var item = new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV", IsActive = true, AvailableQuantity = balance };
        var entry = new StockEntry { Id = Guid.NewGuid(), Item = item, Quantity = 5, Status = ApprovalStatus.Pending };
        c.StockEntries.Add(entry);
        c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = item.Code, Credit = balance, TotalBalance = balance });
        await c.SaveChangesAsync();
        return entry;
    }
    [Fact]
    public async Task ApprovalRetainsOriginalReceiptAndPostsOneReconciledCredit()
    {
        await using var c = Context(); var entry = await Seed(c);
        Assert.True((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        c.ChangeTracker.Clear();
        var saved = await c.StockEntries.Include(e => e.Item).SingleAsync();
        Assert.Equal(5m, saved.Quantity); Assert.Equal(15, saved.Item.AvailableQuantity);
        Assert.Equal(ApprovalStatus.Approved, saved.Status);
        var posting = await c.ItemTransactionLogs.SingleAsync(t => t.TransactionType == TransactionType.Purchased);
        Assert.Equal(5m, posting.Credit); Assert.Equal(15m, posting.TotalBalance);
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.Equal(2, await c.ItemTransactionLogs.CountAsync());
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(0.5)]
    public async Task InvalidReceiptQuantityCannotPostStock(decimal quantity)
    {
        await using var c = Context(); var entry = await Seed(c); entry.Quantity = quantity; await c.SaveChangesAsync();
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.Equal(10, entry.Item.AvailableQuantity); Assert.Equal(ApprovalStatus.Pending, entry.Status);
        Assert.Single(await c.ItemTransactionLogs.ToListAsync());
    }
    [Theory]
    [InlineData(ApprovalStatus.Approved)] [InlineData(ApprovalStatus.Rejected)]
    public async Task TerminalEntriesCannotBeApprovedOrRejectedAgain(ApprovalStatus state)
    {
        await using var c = Context(); var entry = await Seed(c); entry.Status = state; await c.SaveChangesAsync();
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.False((await Repository(c).RejectItem(entry.Id)).IsSuccess);
        Assert.Equal(state, entry.Status); Assert.Equal(10, entry.Item.AvailableQuantity);
    }
    [Fact]
    public async Task RejectionDoesNotChangeTheStockOrLedger()
    {
        await using var c = Context(); var entry = await Seed(c);
        Assert.True((await Repository(c).RejectItem(entry.Id)).IsSuccess);
        Assert.Equal(ApprovalStatus.Rejected, entry.Status); Assert.Equal(10, entry.Item.AvailableQuantity);
        Assert.Single(await c.ItemTransactionLogs.ToListAsync());
    }
    [Fact]
    public async Task UnreconciledLegacyBalanceCannotBeSilentlyReplaced()
    {
        await using var c = Context(); var entry = await Seed(c); entry.Item.AvailableQuantity = 11; await c.SaveChangesAsync();
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.Equal(11, entry.Item.AvailableQuantity); Assert.Equal(ApprovalStatus.Pending, entry.Status);
    }
    [Fact]
    public async Task CrossStoreCodeCollisionCannotPostIntoAMixedLedger()
    {
        await using var c = Context(); var entry = await Seed(c);
        c.Items.Add(new Item { Id = Guid.NewGuid(), Name = "Other item", Code = "GLV", Store = Store.ReagentStore }); await c.SaveChangesAsync();
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.Equal(10, entry.Item.AvailableQuantity);
    }
    [Fact]
    public async Task WholeNumberOverflowCannotPostOrTruncateStock()
    {
        await using var c = Context(); var entry = await Seed(c, int.MaxValue);
        Assert.False((await Repository(c).ApproveItem(entry.Id)).IsSuccess);
        Assert.Equal(int.MaxValue, entry.Item.AvailableQuantity);
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Guid.Parse("258da105-a506-44a9-8ff5-8d005a54f294");
        public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
