using DOMAIN.Entities.ItemTransactionLogs;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Xunit;

namespace APP.Tests.Repository;

public partial class InventoryLedgerPostgresTests
{
    private const string ToInteger = """
        ALTER TABLE "ItemTransactionLogs" ALTER COLUMN "TransactionType" TYPE integer
        USING CASE "TransactionType"
            WHEN 'Damaged' THEN 0 WHEN 'Missing' THEN 1 WHEN 'Returned' THEN 2
            WHEN 'Issued' THEN 3 WHEN 'Purchased' THEN 4 WHEN 'Adjustment' THEN 5
        END;
        """;

    private static async Task VerifyStorageUpgrade(ApplicationDbContext context, Guid logId)
    {
        var sql = new MigrationProbe().UpgradeSql();
        var before = await Snapshot(context);
        await context.Database.ExecuteSqlRawAsync(sql);
        await context.Database.ExecuteSqlRawAsync(sql);
        Assert.Equal(before, await Snapshot(context));

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await context.Database.ExecuteSqlRawAsync(ToInteger);
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"ItemTransactionLogs\" SET \"TransactionType\" = 999 WHERE \"Id\" = {logId}");
            var error = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlRawAsync(sql));
            Assert.Contains("Unknown inventory transaction type", error.MessageText);
            await transaction.RollbackAsync();
        }
        Assert.Equal(before, await Snapshot(context));

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            await context.Database.ExecuteSqlRawAsync(ToInteger);
            await context.Database.ExecuteSqlRawAsync(sql);
            await transaction.CommitAsync();
        }
        Assert.Equal(before, await Snapshot(context));
        Assert.Empty(new MigrationProbe().DowngradeOperations());
    }

    private sealed record LedgerSnapshot(Guid Id, TransactionType Type, decimal Credit, decimal Debit, decimal Balance, DateTime CreatedAt, Guid? CreatedById);
    private static Task<List<LedgerSnapshot>> Snapshot(ApplicationDbContext context) => context.ItemTransactionLogs.AsNoTracking()
        .OrderBy(row => row.Id).Select(row => new LedgerSnapshot(row.Id, row.TransactionType, row.Credit, row.Debit, row.TotalBalance, row.CreatedAt, row.CreatedById)).ToListAsync();

    private sealed class MigrationProbe : RestoreItemTransactionTypeText
    {
        public string UpgradeSql()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Up(builder);
            return Assert.IsType<SqlOperation>(Assert.Single(builder.Operations)).Sql;
        }
        public IReadOnlyList<MigrationOperation> DowngradeOperations()
        {
            var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
            Down(builder);
            return builder.Operations;
        }
    }
}
