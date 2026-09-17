using APP.Services.FullProcedures;
using Npgsql;
using Xunit;

namespace APP.Tests.FullProcedures;

[Collection("Procedure SQL Spike")]
public class ProcedureSqlSpikeStoreTests
{
    private const string TestDatabase = "oryx_procedure_spike_test";

    [Fact]
    public async Task StoreRefusesOtherDatabaseWithoutOpeningIt()
    {
        await using var dataSource = NpgsqlDataSource.Create(
            "Host=127.0.0.1;Database=entrancedb;Username=gigisiri");
        var store = new ProcedureSqlSpikeStore(dataSource);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ReadAsync(Guid.NewGuid()));
    }

    internal static async Task<NpgsqlDataSource> OpenDisposableAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Disposable PostgreSQL spike URL is required");

        var dataSource = NpgsqlDataSource.Create(connectionString);
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync();
            await using var check = new NpgsqlCommand("SELECT current_database()", connection);
            if ((string?)await check.ExecuteScalarAsync() != TestDatabase)
                throw new InvalidOperationException("Refusing to write outside disposable spike DB");
            await using var schema = new NpgsqlCommand("""
                CREATE TABLE IF NOT EXISTS procedure_spike_runs (
                    run_id uuid PRIMARY KEY,
                    version integer NOT NULL,
                    state_json jsonb NOT NULL
                );
                CREATE TABLE IF NOT EXISTS procedure_spike_audit (
                    command_id uuid PRIMARY KEY,
                    run_id uuid NOT NULL REFERENCES procedure_spike_runs(run_id),
                    version integer NOT NULL,
                    intent_kind integer NOT NULL,
                    UNIQUE (run_id, version)
                );
                CREATE TABLE IF NOT EXISTS procedure_spike_outbox (
                    command_id uuid PRIMARY KEY,
                    run_id uuid NOT NULL REFERENCES procedure_spike_runs(run_id),
                    version integer NOT NULL,
                    event_json jsonb NOT NULL,
                    lease_until timestamptz NULL,
                    attempts integer NOT NULL DEFAULT 0,
                    delivered_at timestamptz NULL,
                    effect_receipt_id uuid NULL,
                    UNIQUE (run_id, version)
                );
                CREATE TABLE IF NOT EXISTS procedure_spike_effect_receipts (
                    effect_key text PRIMARY KEY,
                    receipt_id uuid NOT NULL
                );
                """, connection);
            await schema.ExecuteNonQueryAsync();
            return dataSource;
        }
        catch
        {
            await dataSource.DisposeAsync();
            throw;
        }
    }

    private static ProcedureSpikeDefinition Graph(Guid node) =>
        new(Guid.NewGuid(), [new(node, ProcedureSpikeNodeKind.Work, [])]);

    private static async Task<int> CountAsync(
        NpgsqlDataSource dataSource, string table, Guid runId)
    {
        // Table names are fixed test fixtures, never request input.
        if (table is not ("procedure_spike_audit" or "procedure_spike_outbox"))
            throw new ArgumentException("Unknown test table");
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            $"SELECT count(*) FROM {table} WHERE run_id = @run_id", connection);
        command.Parameters.AddWithValue("run_id", runId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    [DisposablePostgresFact]
    public async Task FaultBetweenAuditAndOutboxRollsBackAndRetryCommitsOnce()
    {
        await using var dataSource = await OpenDisposableAsync();
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var graph = Graph(node);
        var intent = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var now = DateTimeOffset.UtcNow;
        await new ProcedureSqlSpikeStore(dataSource).CreateAsync(runId, graph);
        var broken = new ProcedureSqlSpikeStore(dataSource,
            beforeOutbox: () => throw new InvalidOperationException("forced failure"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            broken.ExecuteAsync(runId, graph, intent, now));

        Assert.Equal(0, (await new ProcedureSqlSpikeStore(dataSource).ReadAsync(runId)).Version);
        Assert.Equal(0, await CountAsync(dataSource, "procedure_spike_audit", runId));
        Assert.Equal(0, await CountAsync(dataSource, "procedure_spike_outbox", runId));

        var committed = await new ProcedureSqlSpikeStore(dataSource)
            .ExecuteAsync(runId, graph, intent, now);
        Assert.Equal(1, committed.Version);
        Assert.Equal([node], committed.Completed);
        Assert.Equal(1, await CountAsync(dataSource, "procedure_spike_audit", runId));
        Assert.Equal(1, await CountAsync(dataSource, "procedure_spike_outbox", runId));
    }

    [DisposablePostgresFact]
    public async Task NewConnectionRestoresCommittedStateAndRetryDoesNotDuplicateOutbox()
    {
        await using var dataSource = await OpenDisposableAsync();
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var graph = Graph(node);
        var intent = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var store = new ProcedureSqlSpikeStore(dataSource);
        await store.CreateAsync(runId, graph);
        await store.ExecuteAsync(runId, graph, intent, DateTimeOffset.UtcNow);

        var url = Environment.GetEnvironmentVariable(
            "ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")!;
        await using var restartedDataSource = NpgsqlDataSource.Create(url);
        var restarted = new ProcedureSqlSpikeStore(restartedDataSource);
        Assert.Equal(1, (await restarted.ReadAsync(runId)).Version);
        Assert.Equal(1, (await restarted.ExecuteAsync(runId, graph,
            intent, DateTimeOffset.UtcNow)).Version);
        await Assert.ThrowsAsync<ProcedureSpikeConflict>(() =>
            restarted.ExecuteAsync(runId, graph,
                intent with { CommandId = Guid.NewGuid() }, DateTimeOffset.UtcNow));
        Assert.Equal(1, await CountAsync(restartedDataSource,
            "procedure_spike_outbox", runId));
    }

    [DisposablePostgresFact]
    public async Task SameEffectKeyCannotAcquireTwoReceiptIds()
    {
        await using var dataSource = await OpenDisposableAsync();
        var store = new ProcedureSqlSpikeStore(dataSource);
        var key = $"effect-{Guid.NewGuid():N}";
        var receipt = Guid.NewGuid();
        Assert.Equal(receipt, await store.ConfirmEffectAsync(key, receipt));
        Assert.Equal(receipt, await store.ConfirmEffectAsync(key, receipt));
        await Assert.ThrowsAsync<ProcedureSpikeConflict>(() =>
            store.ConfirmEffectAsync(key, Guid.NewGuid()));
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var count = new NpgsqlCommand(
            "SELECT count(*) FROM procedure_spike_effect_receipts " +
            "WHERE effect_key = @effect_key", connection);
        count.Parameters.AddWithValue("effect_key", key);
        Assert.Equal(1, Convert.ToInt32(await count.ExecuteScalarAsync()));
    }

    [DisposablePostgresFact]
    public async Task CompetingWritersLeaveOneVersionAndOneOutboxEvent()
    {
        await using var dataSource = await OpenDisposableAsync();
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var graph = Graph(node);
        var store = new ProcedureSqlSpikeStore(dataSource);
        await store.CreateAsync(runId, graph);
        var first = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var second = first with { CommandId = Guid.NewGuid() };
        var now = DateTimeOffset.UtcNow;
        var outcomes = await Task.WhenAll(
            Record.ExceptionAsync(() => store.ExecuteAsync(runId, graph, first, now)),
            Record.ExceptionAsync(() => store.ExecuteAsync(runId, graph, second, now)));
        Assert.Single(outcomes, error => error is null);
        Assert.Single(outcomes, error => error is ProcedureSpikeConflict);
        Assert.Equal(1, (await store.ReadAsync(runId)).Version);
        Assert.Equal(1, await CountAsync(dataSource, "procedure_spike_audit", runId));
        Assert.Equal(1, await CountAsync(dataSource, "procedure_spike_outbox", runId));
    }
}
