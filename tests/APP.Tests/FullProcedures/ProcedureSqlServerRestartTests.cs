using System.Diagnostics;
using APP.Services.FullProcedures;
using Npgsql;
using Xunit;

namespace APP.Tests.FullProcedures;

/// <summary>
/// Proves ADR-001's remaining "database-server restart" boundary: killing a
/// .NET worker process (ProcedureSqlProcessDeathTests) does not prove the
/// PostgreSQL server itself can crash and restart without losing committed
/// state or duplicating an in-flight effect. These tests actually stop and
/// restart the disposable cluster's postmaster process via pg_ctl.
/// </summary>
[Collection("Procedure SQL Spike")]
public class ProcedureSqlServerRestartTests
{
    private static string PgDataDir => Environment.GetEnvironmentVariable(
        "ORYX_PROCEDURE_SPIKE_TEST_PGDATA_DIR")!;

    private static string PgCtlPath => Environment.GetEnvironmentVariable(
        "ORYX_PROCEDURE_SPIKE_TEST_PGCTL_PATH") ?? "pg_ctl";

    private static string PgLogPath => Path.Combine(PgDataDir, "..", "pg-restart-test.log");

    /// <summary>
    /// pg_ctl start does NOT reapply the original postmaster.opts on its own —
    /// confirmed empirically: a plain "pg_ctl start" after this stop tried to
    /// bind the default port 5432 instead of the disposable cluster's port.
    /// Always pass the listen address/port back explicitly.
    /// </summary>
    private static string StartOptions()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")!);
        return $"-p {builder.Port} -h {builder.Host}";
    }

    private static async Task RunPgCtlAsync(params string[] args)
    {
        var start = new ProcessStartInfo(PgCtlPath)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };
        start.ArgumentList.Add("-D");
        start.ArgumentList.Add(PgDataDir);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("pg_ctl did not start");
        // Drain both streams concurrently with the wait: an unread pipe fills its
        // OS buffer and deadlocks the child (postgres logs verbosely on restart).
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await process.WaitForExitAsync(deadline.Token);
        await Task.WhenAll(stdout, stderr);
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"pg_ctl {string.Join(' ', args)} failed: {await stderr}");
    }

    /// <summary>Immediate mode skips a checkpoint, matching an actual crash rather than a clean shutdown.</summary>
    private static Task CrashServerAsync() => RunPgCtlAsync("stop", "-m", "immediate");

    private static Task StartServerAsync() =>
        RunPgCtlAsync("-l", PgLogPath, "-o", StartOptions(), "start", "-w", "-t", "20");

    private static async Task<NpgsqlDataSource> WaitForServerReadyAsync(string url)
    {
        var dataSource = NpgsqlDataSource.Create(url);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(deadline.Token);
                return dataSource;
            }
            catch (Exception) when (!deadline.IsCancellationRequested)
            {
                await Task.Delay(200, CancellationToken.None);
            }
        }
    }

    private static async Task<(int Audit, int Outbox)> CountAuditAndOutboxAsync(
        NpgsqlDataSource dataSource, Guid runId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var audit = new NpgsqlCommand(
            "SELECT count(*) FROM procedure_spike_audit WHERE run_id = @run_id", connection);
        audit.Parameters.AddWithValue("run_id", runId);
        await using var outbox = new NpgsqlCommand(
            "SELECT count(*) FROM procedure_spike_outbox WHERE run_id = @run_id", connection);
        outbox.Parameters.AddWithValue("run_id", runId);
        return (
            Convert.ToInt32(await audit.ExecuteScalarAsync()),
            Convert.ToInt32(await outbox.ExecuteScalarAsync()));
    }

    [DisposablePostgresServerControlFact]
    public async Task CrashAndRestartPreservesCommittedStateAndRetryIsIdempotent()
    {
        var url = Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")!;
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var definition = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(node, ProcedureSpikeNodeKind.Work, [])]);
        var intent = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var now = DateTimeOffset.UtcNow;

        await using (var dataSource = await ProcedureSqlSpikeStoreTests.OpenDisposableAsync())
        {
            var store = new ProcedureSqlSpikeStore(dataSource);
            await store.CreateAsync(runId, definition);
            await store.ExecuteAsync(runId, definition, intent, now);
        }

        await CrashServerAsync();
        await StartServerAsync();

        await using var restarted = await WaitForServerReadyAsync(url);
        var restartedStore = new ProcedureSqlSpikeStore(restarted);
        Assert.Equal(1, (await restartedStore.ReadAsync(runId)).Version);

        Assert.Equal(1, (await restartedStore.ExecuteAsync(
            runId, definition, intent, now)).Version);
        var (auditCount, outboxCount) = await CountAuditAndOutboxAsync(restarted, runId);
        Assert.Equal(1, auditCount);
        Assert.Equal(1, outboxCount);
    }

    [DisposablePostgresServerControlFact]
    public async Task CrashDuringTransactionLeavesNoPartialEffectAndRetryCommitsExactlyOnce()
    {
        var url = Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")!;
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var definition = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(node, ProcedureSpikeNodeKind.Work, [])]);
        var intent = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var now = DateTimeOffset.UtcNow;

        await using (var dataSource = await ProcedureSqlSpikeStoreTests.OpenDisposableAsync())
        {
            await new ProcedureSqlSpikeStore(dataSource).CreateAsync(runId, definition);
            var crashing = new ProcedureSqlSpikeStore(dataSource,
                beforeOutbox: () => CrashServerAsync().GetAwaiter().GetResult());
            await Assert.ThrowsAnyAsync<Exception>(() =>
                crashing.ExecuteAsync(runId, definition, intent, now));
        }

        await StartServerAsync();

        await using var restarted = await WaitForServerReadyAsync(url);
        var restartedStore = new ProcedureSqlSpikeStore(restarted);
        Assert.Equal(0, (await restartedStore.ReadAsync(runId)).Version);
        var (auditBefore, outboxBefore) = await CountAuditAndOutboxAsync(restarted, runId);
        Assert.Equal(0, auditBefore);
        Assert.Equal(0, outboxBefore);

        var committed = await restartedStore.ExecuteAsync(runId, definition, intent, now);
        Assert.Equal(1, committed.Version);
        var (auditAfter, outboxAfter) = await CountAuditAndOutboxAsync(restarted, runId);
        Assert.Equal(1, auditAfter);
        Assert.Equal(1, outboxAfter);
    }
}
