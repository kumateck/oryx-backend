using System.Diagnostics;
using APP.Services.FullProcedures;
using Npgsql;
using Xunit;

namespace APP.Tests.FullProcedures;

[Collection("Procedure SQL Spike")]
public class ProcedureSqlProcessDeathTests
{
    private static async Task<(int ExitCode, string Error)> RunWorkerAsync(
        string url, Guid runId, Guid commandId, DateTimeOffset now, string mode)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
            ?? throw new InvalidOperationException("Test build configuration not found");
        var assembly = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../ProcedureSpikeWorker/bin", configuration, "net9.0",
            "ProcedureSpikeWorker.dll"));
        if (!File.Exists(assembly))
            throw new FileNotFoundException("Test worker must be built with APP.Tests", assembly);
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        start.ArgumentList.Add(assembly);
        start.ArgumentList.Add(runId.ToString());
        start.ArgumentList.Add(commandId.ToString());
        start.ArgumentList.Add(now.ToString("O"));
        start.ArgumentList.Add(mode);
        start.Environment["ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL"] = url;
        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Disposable worker did not start");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill();
            throw new TimeoutException("Disposable worker did not exit in 15 seconds");
        }
        return (process.ExitCode, await process.StandardError.ReadToEndAsync());
    }

    private static async Task AssertDeathWindowAsync(
        string deathWindow, bool externalEffectStored, bool localReceiptStored)
    {
        await using var dataSource = await ProcedureSqlSpikeStoreTests.OpenDisposableAsync();
        await using (var connection = await dataSource.OpenConnectionAsync())
        await using (var schema = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS procedure_spike_external_effects (
                effect_key text PRIMARY KEY,
                receipt_id uuid NOT NULL
            )
            """, connection))
            await schema.ExecuteNonQueryAsync();

        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var definition = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(node, ProcedureSpikeNodeKind.Work, [])]);
        var command = new ProcedureSpikeCommand(commandId,
            ProcedureSpikeCommandKind.Complete, 0, node);
        var store = new ProcedureSqlSpikeStore(dataSource);
        await store.CreateAsync(runId, definition);
        await store.ExecuteAsync(runId, definition, command, now);

        var url = Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")!;
        var first = await RunWorkerAsync(url, runId, commandId, now, deathWindow);
        Assert.NotEqual(0, first.ExitCode);

        var effectKey = $"procedure-spike-outbox-{commandId:N}";
        await using (var connection = await dataSource.OpenConnectionAsync())
        {
            await using var outbox = new NpgsqlCommand(
                "SELECT attempts, delivered_at FROM procedure_spike_outbox " +
                "WHERE command_id = @command_id", connection);
            outbox.Parameters.AddWithValue("command_id", commandId);
            await using (var reader = await outbox.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal(1, reader.GetInt32(0));
                Assert.True(await reader.IsDBNullAsync(1));
            }
            await using var external = new NpgsqlCommand(
                "SELECT count(*) FROM procedure_spike_external_effects " +
                "WHERE effect_key = @key", connection);
            external.Parameters.AddWithValue("key", effectKey);
            Assert.Equal(externalEffectStored ? 1 : 0,
                Convert.ToInt32(await external.ExecuteScalarAsync()));
            await using var local = new NpgsqlCommand(
                "SELECT count(*) FROM procedure_spike_effect_receipts " +
                "WHERE effect_key = @key", connection);
            local.Parameters.AddWithValue("key", effectKey);
            Assert.Equal(localReceiptStored ? 1 : 0,
                Convert.ToInt32(await local.ExecuteScalarAsync()));
        }
        Assert.False(await new ProcedureSqlOutboxSpike(dataSource).DeliverOneAsync(
            now.AddSeconds(15), _ => throw new InvalidOperationException(
                "Leased effect must not run"), onlyCommandId: commandId));

        var second = await RunWorkerAsync(url, runId, commandId,
            now.AddSeconds(31), "resume");
        Assert.True(second.ExitCode == 0, second.Error);
        await using var finalConnection = await dataSource.OpenConnectionAsync();
        await using var final = new NpgsqlCommand("""
            SELECT o.attempts, o.delivered_at, o.effect_receipt_id,
                   e.receipt_id, r.receipt_id
            FROM procedure_spike_outbox o
            JOIN procedure_spike_external_effects e ON e.effect_key = @key
            JOIN procedure_spike_effect_receipts r ON r.effect_key = @key
            WHERE o.command_id = @command_id
            """, finalConnection);
        final.Parameters.AddWithValue("key", effectKey);
        final.Parameters.AddWithValue("command_id", commandId);
        await using var finalReader = await final.ExecuteReaderAsync();
        Assert.True(await finalReader.ReadAsync());
        Assert.Equal(2, finalReader.GetInt32(0));
        Assert.False(await finalReader.IsDBNullAsync(1));
        Assert.Equal(finalReader.GetGuid(2), finalReader.GetGuid(3));
        Assert.Equal(finalReader.GetGuid(3), finalReader.GetGuid(4));
        Assert.False(await finalReader.ReadAsync());
    }

    [DisposablePostgresFact]
    public async Task DeathAfterClaimBeforeEffectReplaysOneFakeEffect() =>
        await AssertDeathWindowAsync("before-effect", false, false);

    [DisposablePostgresFact]
    public async Task DeathAfterEffectBeforeReceiptReusesFakeEffect() =>
        await AssertDeathWindowAsync("after-effect", true, false);

    [DisposablePostgresFact]
    public async Task DeathAfterReceiptBeforeAckReusesFakeEffect() =>
        await AssertDeathWindowAsync("after-receipt", true, true);
}
