using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class InventoryDamagePostingTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
    private static DamagedStocksRepository Repository(ApplicationDbContext c) => new(c,
        new MapperConfiguration(m => m.CreateMap<CreateDamagedStockRequest, DamagedStock>()).CreateMapper());
    private static async Task<Item> Seed(ApplicationDbContext c)
    {
        var item = new Item { Id = Guid.NewGuid(), Name = "Gloves", Code = "GLV", IsActive = true, AvailableQuantity = 10 };
        c.Add(item); c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "GLV", Credit = 10, TotalBalance = 10 });
        await c.SaveChangesAsync(); return item;
    }
    [Theory]
    [InlineData(DamageStatus.Damage)] [InlineData(DamageStatus.Missing)]
    public async Task ReportStockAndLedgerCarryTheSameQuantityAndAuditActor(DamageStatus kind)
    {
        await using var c = Context(); var item = await Seed(c);
        var result = await Repository(c).CreateDamagedStocks(new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = kind, QuantityDamaged = 2, Remarks = "Broken" }, Actor);
        Assert.True(result.IsSuccess); c.ChangeTracker.Clear();
        var report = await c.DamagedStocks.SingleAsync(); Assert.Equal(result.Value, report.Id); Assert.Equal(Actor, report.CreatedById);
        Assert.Equal(8, (await c.Items.SingleAsync()).AvailableQuantity);
        var posting = await c.ItemTransactionLogs.SingleAsync(l => l.Debit == 2);
        Assert.Equal(8m, posting.TotalBalance); Assert.Equal(Actor, posting.CreatedById);
        Assert.Equal(kind == DamageStatus.Damage ? TransactionType.Damaged : TransactionType.Missing, posting.TransactionType);
    }
    [Theory]
    [InlineData(0, 0)] [InlineData(-1, 0)] [InlineData(11, 0)] [InlineData(2, 99)]
    public async Task InvalidReportsCannotChangeStock(int quantity, int kind)
    {
        await using var c = Context(); var item = await Seed(c);
        var result = await Repository(c).CreateDamagedStocks(new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = (DamageStatus)kind, QuantityDamaged = quantity }, Actor);
        Assert.False(result.IsSuccess); Assert.Equal(10, item.AvailableQuantity); Assert.Empty(await c.DamagedStocks.ToListAsync());
        Assert.Single(await c.ItemTransactionLogs.ToListAsync());
    }
    [Fact]
    public async Task UnreconciledLedgerCannotCreateAReportOrDebit()
    {
        await using var c = Context(); var item = await Seed(c); item.AvailableQuantity = 9; await c.SaveChangesAsync();
        Assert.False((await Repository(c).CreateDamagedStocks(new CreateDamagedStockRequest { ItemId = item.Id, QuantityDamaged = 2 }, Actor)).IsSuccess);
        Assert.Empty(await c.DamagedStocks.ToListAsync()); Assert.Equal(9, item.AvailableQuantity);
    }
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
