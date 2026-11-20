using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace APP.Tests.Repository;

/// <summary>
/// Issuing a stock requisition zeroed each issued lot's assigned quantity although the rest stayed on its shelves, so the
/// lot's next stock transfer was refused with StockTransfer.Balance (Ethanol 96%, Syrup to Ointment, 2026-11-20).
/// </summary>
public sealed class IssueKeepsLotBalancePostgresTests
{
    private static Task<Dictionary<Guid, decimal>> ShelfTotals(INFRASTRUCTURE.Context.ApplicationDbContext db) =>
        db.ShelfMaterialBatches.IgnoreQueryFilters().Where(row => row.DeletedAt == null)
            .GroupBy(row => row.MaterialBatchId).ToDictionaryAsync(group => group.Key, group => group.Sum(row => row.Quantity));

    [RepairPostgresFact]
    public async Task Issuing_part_of_a_lot_keeps_the_rest_assigned()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: true);
        await using var db = fixture.Open();
        var shelves = await ShelfTotals(db);
        var lots = await db.MaterialBatches.IgnoreQueryFilters().Where(lot => shelves.Keys.Contains(lot.Id)).ToListAsync();
        Assert.NotEmpty(lots);
        Assert.All(lots, lot => Assert.True(shelves[lot.Id] > 0 && lot.QuantityAssigned == shelves[lot.Id],
            $"{lot.BatchNumber}: assigned {lot.QuantityAssigned}, on shelves {shelves[lot.Id]}"));
    }

    [RepairPostgresFact]
    public async Task The_repair_restores_a_zeroed_lot_to_its_shelf_stock_and_leaves_others()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        var shelves = await ShelfTotals(db);
        var lots = await db.MaterialBatches.IgnoreQueryFilters().Where(lot => shelves.Keys.Contains(lot.Id)).OrderBy(lot => lot.BatchNumber).ToListAsync();
        lots[0].QuantityAssigned = 0;
        lots[1].QuantityAssigned = shelves[lots[1].Id] + 5; // above its shelves: not lowered
        await db.SaveChangesAsync();
        var migration = new INFRASTRUCTURE.Migrations.RepairLotAssignedFromShelves();
        var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
        foreach (var command in generator.Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.ChangeTracker.Clear();
        var after = await db.MaterialBatches.IgnoreQueryFilters().Where(lot => lots.Select(item => item.Id).Contains(lot.Id)).ToDictionaryAsync(lot => lot.Id, lot => lot.QuantityAssigned);
        Assert.Equal(shelves[lots[0].Id], after[lots[0].Id]);
        Assert.Equal(shelves[lots[1].Id] + 5, after[lots[1].Id]);
    }
}
