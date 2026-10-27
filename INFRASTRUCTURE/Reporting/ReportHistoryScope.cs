using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace INFRASTRUCTURE.Reporting;

/// <summary>Connection-local temporal views reuse authoritative report queries and never change live tables.</summary>
public sealed class ReportHistoryScope : IAsyncDisposable
{
    private readonly Context.ApplicationDbContext context;
    private readonly IDbContextTransaction transaction;
    private readonly QueryTrackingBehavior tracking;
    private ReportHistoryScope(Context.ApplicationDbContext context, IDbContextTransaction transaction, QueryTrackingBehavior tracking)
    { this.context = context; this.transaction = transaction; this.tracking = tracking; }

    public static async Task<Dictionary<string, DateTime>> Coverage(DbContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Database.IsNpgsql()) return [];
        var wasOpen = context.Database.GetDbConnection().State == ConnectionState.Open;
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"TableName\",\"AvailableFrom\" FROM public.\"ReportHistoryCoverage\"";
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            var mapped = context.Model.GetEntityTypes().Select(type => type.GetTableName()).Where(name => name is not null).ToHashSet(StringComparer.Ordinal);
            while (await reader.ReadAsync(cancellationToken))
                if (mapped.Contains(reader.GetString(0)) && !ReportHistoryCatalog.AuthenticationTables.Contains(reader.GetString(0), StringComparer.Ordinal)) result[reader.GetString(0)] = reader.GetDateTime(1);
            return result;
        }
        catch (PostgresException error) when (error.SqlState == PostgresErrorCodes.UndefinedTable) { return []; }
        finally { if (!wasOpen) await context.Database.CloseConnectionAsync(); }
    }

    public static DateTime Cutoff(DateTimeOffset requested, DateTime utcNow)
    {
        var cutoff = requested.UtcDateTime;
        if (cutoff.Date > utcNow.Date) throw new ArgumentException("Choose today or an earlier historical date.");
        return cutoff > utcNow ? utcNow : cutoff;
    }

    public static async Task<ReportHistoryScope> Open(Context.ApplicationDbContext context, DateTime cutoff, CancellationToken cancellationToken = default)
    {
        if (context.ReportAsOfUtc.HasValue || context.Database.CurrentTransaction is not null || context.ChangeTracker.HasChanges())
            throw new ReportHistoryUnavailableException("Historical reports require a separate, clean read transaction.");
        var coverage = await Coverage(context, cancellationToken);
        if (coverage.Count == 0) throw new ReportHistoryUnavailableException("Historical report capture is not installed on this server.");
        var availableFrom = coverage.Values.Max();
        if (cutoff < availableFrom) throw new ReportHistoryUnavailableException("The selected date is before recorded reporting history begins.", availableFrom);
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var scope = new ReportHistoryScope(context, transaction, context.ChangeTracker.QueryTrackingBehavior);
        try
        {
            var timestamp = cutoff.ToString("O", CultureInfo.InvariantCulture);
            foreach (var table in coverage.Keys)
            {
                // Both names and timestamp are server-owned, typed/allowlisted values, never raw query text.
                var quoted = table.Replace("\"", "\"\""); var literal = table.Replace("'", "''");
                var sql = $"""
                    CREATE TEMP VIEW "{quoted}" AS
                    SELECT (jsonb_populate_record(NULL::public."{quoted}",v."Payload")).*
                    FROM (SELECT DISTINCT ON (h."EntityKey") h."Payload",h."IsDeleted"
                      FROM public."ReportHistoryVersions" h
                      WHERE h."TableName"='{literal}' AND h."RecordedAt"<='{timestamp}'::timestamptz
                        AND NOT EXISTS(SELECT 1 FROM public."ReportHistoryVersions" later
                          WHERE later."TransactionId"=h."TransactionId" AND later."RecordedAt">'{timestamp}'::timestamptz)
                      ORDER BY h."EntityKey",h."RecordedAt" DESC,h."Sequence" DESC) v
                    WHERE NOT v."IsDeleted"
                    """;
                await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
            }
            // A changed search path also forces existing prepared statements to resolve the temporal views.
            await context.Database.ExecuteSqlRawAsync("SET LOCAL search_path=pg_temp,public", cancellationToken);
            context.ReportHistoryTables = coverage.Keys.ToHashSet(StringComparer.Ordinal);
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            context.ReportAsOfUtc = cutoff;
            return scope;
        }
        catch { await scope.DisposeAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        context.ReportAsOfUtc = null;
        context.ReportHistoryTables = null;
        context.ChangeTracker.QueryTrackingBehavior = tracking;
        try { await transaction.RollbackAsync(); }
        finally { await transaction.DisposeAsync(); }
    }
}
