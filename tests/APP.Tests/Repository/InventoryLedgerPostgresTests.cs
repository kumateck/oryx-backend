using APP.Repository;
using APP.Services.Inventory;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public partial class InventoryLedgerPostgresTests
{
    [Fact]
    public void Transaction_type_uses_the_existing_text_storage_contract()
    {
        using var context = Context("Host=127.0.0.1;Database=oryx_inventory_ledger_test;Username=postgres");
        var property = context.Model.FindEntityType(typeof(ItemTransactionLog))?.FindProperty(nameof(ItemTransactionLog.TransactionType));
        Assert.NotNull(property);
        Assert.Equal("text", property.GetColumnType());
        Assert.Equal(typeof(string), property.GetTypeMapping().Converter?.ProviderClrType);
    }

    [LocalInventoryLedgerFact]
    public async Task Existing_text_ledger_supports_receipt_approval_issues_and_filtered_reads()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_INVENTORY_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing disposable fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost") || parsed.Database != "oryx_inventory_ledger_test")
            throw new InvalidOperationException("Only the dedicated local disposable inventory fixture is permitted.");
        var identity = new CurrentUser();
        await using var context = Context(connection, identity);
        await context.Database.EnsureCreatedAsync();
        // Match the original production migration even when running this test against the broken int model.
        await context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "ItemTransactionLogs" ALTER COLUMN "TransactionType" TYPE text USING "TransactionType"::text;
            """);
        var actor = new User { Id = Guid.NewGuid(), UserName = "ledger-test", FirstName = "Test", LastName = "Storekeeper" };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "pc" };
        var department = new Department { Id = Guid.NewGuid(), Name = "Inventory test", Type = DepartmentType.NonProduction };
        var item = new Item { Id = Guid.NewGuid(), Name = "Test item", Code = $"TEST-{Guid.NewGuid():N}", UnitOfMeasure = unit,
            IsActive = true, Store = Store.GeneralStore, AvailableQuantity = 10 };
        var entry = new StockEntry { Id = Guid.NewGuid(), Item = item, Quantity = 5,
            Memo = new Memo { Id = Guid.NewGuid(), Code = "TEST-MEMO", Paid = true } };
        var requisition = new ItemStockRequisition { Id = Guid.NewGuid(), Number = "TEST-SR", RequestedBy = actor, Department = department,
            Justification = "Disposable regression fixture", RequisitionItems = [new() { Id = Guid.NewGuid(), Item = item, QuantityRequested = 3 }] };
        context.AddRange(entry, requisition);
        await context.SaveChangesAsync();
        identity.UserId = actor.Id;
        var logId = Guid.NewGuid();
        var now = DateTime.UtcNow.AddMinutes(-1);
        // A real legacy text value, not one written through the application's converter.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ItemTransactionLogs" ("Id", "Date", "CreatedAt", "TransactionType", "ItemCode", "Credit", "Debit", "TotalBalance")
            VALUES ({logId}, {now}, {now}, 'Purchased', {item.Code}, 10, 0, 10);
            """);
        context.ChangeTracker.Clear();
        var current = await context.Items.SingleAsync(row => row.Id == item.Id);
        var initial = await InventoryLedger.Balance(context, current);
        Assert.True(initial.IsSuccess);
        Assert.Equal(10m, initial.Value);

        var receiving = new InventoryProcurementRepository(context, null!, null!, null!);
        Assert.True((await receiving.ApproveItem(entry.Id)).IsSuccess);
        context.ChangeTracker.Clear();
        Assert.Equal(ApprovalStatus.Approved, (await context.StockEntries.SingleAsync(row => row.Id == entry.Id)).Status);
        Assert.Equal(15, (await context.Items.SingleAsync(row => row.Id == item.Id)).AvailableQuantity);
        Assert.False((await receiving.ApproveItem(entry.Id)).IsSuccess);

        var issuing = new ItemStockRequisitionRepository(context);
        var issued = await issuing.IssueStock(requisition.Id, new IssueStockAgainstRequisitionRequest {
            QuantitiesToIssue = [new() { ItemId = item.Id, Quantity = 3 }] }, actor.Id);
        Assert.True(issued.IsSuccess);
        context.ChangeTracker.Clear();
        Assert.Equal(12, (await context.Items.SingleAsync(row => row.Id == item.Id)).AvailableQuantity);
        Assert.Equal(12m, (await InventoryLedger.Balance(context, await context.Items.SingleAsync(row => row.Id == item.Id))).Value);
        var credits = await context.ItemTransactionLogs.Where(row => row.ItemCode == item.Code && row.TransactionType == TransactionType.Purchased).ToListAsync();
        Assert.Equal(2, credits.Count);
        var debit = Assert.Single(await context.ItemTransactionLogs.Where(row => row.ItemCode == item.Code && row.TransactionType == TransactionType.Issued).ToListAsync());
        Assert.Equal(3m, debit.Debit);
        Assert.Equal(actor.Id, debit.CreatedById);
        Assert.Equal(IssueItemStockRequisitionStatus.Completed, (await context.ItemStockRequisitions.SingleAsync(row => row.Id == requisition.Id)).Status);

        foreach (var type in Enum.GetValues<TransactionType>())
            context.ItemTransactionLogs.Add(new ItemTransactionLog { Id = Guid.NewGuid(), ItemCode = $"{item.Code}-{type}", TransactionType = type });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        foreach (var type in Enum.GetValues<TransactionType>())
            Assert.Equal(type, (await context.ItemTransactionLogs.SingleAsync(row => row.ItemCode == $"{item.Code}-{type}")).TransactionType);
        await VerifyStorageUpgrade(context, logId);
    }

    private static ApplicationDbContext Context(string connection, CurrentUser? identity = null) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, identity ?? new CurrentUser());
    private sealed class CurrentUser : ICurrentUserService
    {
        public Guid? UserId { get; set; }
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}

public sealed class LocalInventoryLedgerFactAttribute : FactAttribute
{
    public LocalInventoryLedgerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_INVENTORY_TEST_CONNECTION")))
            Skip = "Set ORYX_INVENTORY_TEST_CONNECTION to the dedicated disposable local PostgreSQL fixture.";
    }
}
