using APP.Repository;
using APP.Services.Inventory;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.StockEntries;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;
public class InventoryConcurrencyTests
{
    [Fact]
    public async Task AStaleReceiptCannotOverwriteAnotherDecision()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var id = Guid.NewGuid();
        await using var first = new ApplicationDbContext(options, new CurrentUser());
        first.StockEntries.Add(new StockEntry { Id = id, Status = ApprovalStatus.Pending, Quantity = 5 }); await first.SaveChangesAsync();
        await using var stale = new ApplicationDbContext(options, new CurrentUser());
        await stale.StockEntries.SingleAsync();
        (await first.StockEntries.SingleAsync()).Status = ApprovalStatus.Approved; await first.SaveChangesAsync();
        var repo = new InventoryProcurementRepository(stale, new MapperConfiguration(m => m.CreateMap<StockEntry, StockEntryDto>()).CreateMapper(), null, null);
        var result = await repo.RejectItem(id);
        Assert.False(result.IsSuccess); Assert.Equal("Inventory.Changed", result.Error.Code);
        await using var verify = new ApplicationDbContext(options, new CurrentUser());
        Assert.Equal(ApprovalStatus.Approved, (await verify.StockEntries.SingleAsync()).Status);
    }
    [Fact]
    public async Task AStaleAvailableQuantityCannotOverwriteAConcurrentStockChange()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var first = new ApplicationDbContext(options, new CurrentUser());
        first.Items.Add(new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV", AvailableQuantity = 10 }); await first.SaveChangesAsync();
        await using var stale = new ApplicationDbContext(options, new CurrentUser());
        var second = await stale.Items.SingleAsync();
        (await first.Items.SingleAsync()).AvailableQuantity = 9; await first.SaveChangesAsync();
        second.AvailableQuantity = 8;
        Assert.False((await InventoryCommit.Save(stale)).IsSuccess);
        await using var verify = new ApplicationDbContext(options, new CurrentUser());
        Assert.Equal(9, (await verify.Items.SingleAsync()).AvailableQuantity);
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
