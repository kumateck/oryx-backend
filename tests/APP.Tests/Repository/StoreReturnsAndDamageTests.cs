using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.RecoverableItemsReports;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class StoreReturnsAndDamageTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());

    private static async Task<(Item item, Guid line)> Issued(ApplicationDbContext c, InventoryClassification kind = InventoryClassification.Recoverable, int issue = 3)
    {
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "pc" };
        var department = new Department { Id = Guid.NewGuid(), Name = "Quality" };
        c.AddRange(unit, department, new User { Id = Actor, UserName = "storekeeper" });
        var item = new Item { Id = Guid.NewGuid(), Name = "Drill", Code = "DRL", Store = Store.EquipmentStore, Classification = kind, IsActive = true, AvailableQuantity = 10, UnitOfMeasureId = unit.Id };
        c.Add(item);
        c.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = "DRL", Credit = 10, TotalBalance = 10 });
        await c.SaveChangesAsync();
        var requisitions = new ItemStockRequisitionRepository(c);
        var id = (await requisitions.CreateItemStockRequisition(new CreateItemStockRequisitionRequest
        {
            Number = "SR-9", DepartmentId = department.Id, StockItems = [new StockItems { ItemId = item.Id, QuantityRequested = 5 }]
        }, Actor)).Value;
        if (issue > 0) Assert.True((await requisitions.IssueStock(id, new IssueStockAgainstRequisitionRequest { QuantitiesToIssue = [new IssueQuantityDto { ItemId = item.Id, Quantity = issue }] }, Actor)).IsSuccess);
        var line = await c.ItemStockRequisitionItems.SingleAsync();
        return (item, line.Id);
    }

    private static CreateRecoverableItemReportRequest Return(Item item, Guid? line, int quantity) =>
        new() { ItemId = item.Id, ItemStockRequisitionItemId = line, Quantity = quantity, Reason = "Finished with it" };

    [Fact]
    public async Task AReturnPutsStockBackAndKeepsTheLedgerReconciled()
    {
        await using var c = Context(); var (item, line) = await Issued(c);
        var repository = new RecoverableItemReportRepository(c);
        Assert.True((await repository.CreateRecoverableItemReport(Return(item, line, 2), Actor)).IsSuccess);
        c.ChangeTracker.Clear();
        var stored = await c.Items.SingleAsync();
        Assert.Equal(9, stored.AvailableQuantity);
        // Before the repair the ledger was credited but stock was not, so the next issue failed reconciliation.
        Assert.True((await APP.Services.Inventory.InventoryLedger.Balance(c, stored)).IsSuccess);
        var credit = await c.ItemTransactionLogs.SingleAsync(l => l.TransactionType == TransactionType.Returned);
        Assert.Equal(2m, credit.Credit); Assert.Equal(9m, credit.TotalBalance); Assert.Equal(Actor, credit.CreatedById);
        var listed = (await repository.GetRecoverableItemReport([Store.EquipmentStore])).Value.Single();
        Assert.Equal("SR-9", listed.RequisitionNumber); Assert.Equal("Drill", listed.Item.Name);
        Assert.Empty((await repository.GetRecoverableItemReport([Store.ItStore])).Value);
    }

    [Fact]
    public async Task ReturnsCannotExceedWhatWasIssuedLessEarlierReturns()
    {
        await using var c = Context(); var (item, line) = await Issued(c);
        var repository = new RecoverableItemReportRepository(c);
        Assert.Equal("Return.Exceeds", (await repository.CreateRecoverableItemReport(Return(item, line, 4), Actor)).Error.Code);
        Assert.True((await repository.CreateRecoverableItemReport(Return(item, line, 2), Actor)).IsSuccess);
        Assert.Equal("Return.Exceeds", (await repository.CreateRecoverableItemReport(Return(item, line, 2), Actor)).Error.Code);
        Assert.Equal("Return.Line", (await repository.CreateRecoverableItemReport(Return(item, null, 1), Actor)).Error.Code);
        Assert.Equal(9, (await c.Items.SingleAsync()).AvailableQuantity);
    }

    [Fact]
    public async Task NonRecoverableItemsCannotBeReturned()
    {
        await using var c = Context(); var (item, line) = await Issued(c, InventoryClassification.NonRecoverable);
        Assert.Equal("Item.NotRecoverable", (await new RecoverableItemReportRepository(c).CreateRecoverableItemReport(Return(item, line, 1), Actor)).Error.Code);
    }

    private static DamagedStocksRepository Damage(ApplicationDbContext c) => new(c,
        new MapperConfiguration(m => m.CreateMap<CreateDamagedStockRequest, DamagedStock>()).CreateMapper());

    [Fact]
    public async Task CorrectingADamageRecordPostsTheDifferenceBothWays()
    {
        await using var c = Context(); var (item, _) = await Issued(c, issue: 0);
        var repository = Damage(c);
        var id = (await repository.CreateDamagedStocks(new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = DamageStatus.Damage, QuantityDamaged = 4 }, Actor)).Value;
        Assert.True((await repository.UpdateDamagedStocks(id, new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = DamageStatus.Damage, QuantityDamaged = 1 }, Actor)).IsSuccess);
        Assert.Equal(9, (await c.Items.SingleAsync()).AvailableQuantity);
        Assert.Equal(3m, (await c.ItemTransactionLogs.OrderByDescending(l => l.CreatedAt).FirstAsync(l => l.Credit == 3)).Credit);
        Assert.True((await repository.UpdateDamagedStocks(id, new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = DamageStatus.Missing, QuantityDamaged = 2 }, Actor)).IsSuccess);
        var stored = await c.Items.SingleAsync();
        Assert.Equal(8, stored.AvailableQuantity);
        Assert.True((await APP.Services.Inventory.InventoryLedger.Balance(c, stored)).IsSuccess);
        Assert.Equal("DamagedStock.Item", (await repository.UpdateDamagedStocks(id, new CreateDamagedStockRequest { ItemId = Guid.NewGuid(), DamageStatus = DamageStatus.Damage, QuantityDamaged = 2 }, Actor)).Error.Code);
    }

    [Fact]
    public async Task DeletingADamageRecordGivesTheStockBackOnTheLedger()
    {
        await using var c = Context(); var (item, _) = await Issued(c, issue: 0);
        var repository = Damage(c);
        var id = (await repository.CreateDamagedStocks(new CreateDamagedStockRequest { ItemId = item.Id, DamageStatus = DamageStatus.Missing, QuantityDamaged = 3 }, Actor)).Value;
        Assert.True((await repository.DeleteDamagedStocks(id, Actor)).IsSuccess);
        var stored = await c.Items.SingleAsync();
        Assert.Equal(10, stored.AvailableQuantity);
        Assert.True((await APP.Services.Inventory.InventoryLedger.Balance(c, stored)).IsSuccess);
        Assert.Empty(await c.DamagedStocks.ToListAsync());
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId => Actor; public Guid? DepartmentId => null; public string DepartmentType => string.Empty;
    }
}
