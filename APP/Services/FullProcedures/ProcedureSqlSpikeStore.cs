#nullable enable
using System.Text.Json;
using Npgsql;

namespace APP.Services.FullProcedures;

// Disposable-schema PostgreSQL proof only. Do not register against the ERP database.
public sealed class ProcedureSqlSpikeStore(
    NpgsqlDataSource dataSource,
    Action? beforeOutbox = null)
{
    private const string DisposableDatabase = "oryx_procedure_spike_test";

    private void RequireDisposableDatabase()
    {
        if (new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Database
            != DisposableDatabase)
            throw new InvalidOperationException(
                "Refusing to use Procedure SQL spike outside its disposable test database");
    }

    public async Task CreateAsync(Guid runId, ProcedureSpikeDefinition definition)
    {
        RequireDisposableDatabase();
        if (runId == Guid.Empty) throw new ArgumentException("Run ID is required");
        var run = ProcedureRuntimeSpike.Start(definition);
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO procedure_spike_runs (run_id, version, state_json) " +
            "VALUES (@run_id, @version, @state)", connection);
        command.Parameters.AddWithValue("run_id", runId);
        command.Parameters.AddWithValue("version", run.Version);
        command.Parameters.AddWithValue("state", NpgsqlTypes.NpgsqlDbType.Jsonb,
            JsonSerializer.Serialize(run));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<ProcedureSpikeRun> ReadAsync(Guid runId)
    {
        RequireDisposableDatabase();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT state_json FROM procedure_spike_runs WHERE run_id = @run_id", connection);
        command.Parameters.AddWithValue("run_id", runId);
        var json = (string?)await command.ExecuteScalarAsync();
        return json is null
            ? throw new KeyNotFoundException("Run not found")
            : JsonSerializer.Deserialize<ProcedureSpikeRun>(json)
                ?? throw new InvalidOperationException("Run state is unreadable");
    }

    public async Task<ProcedureSpikeRun> ExecuteAsync(
        Guid runId,
        ProcedureSpikeDefinition definition,
        ProcedureSpikeCommand intent,
        DateTimeOffset now)
    {
        RequireDisposableDatabase();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var select = new NpgsqlCommand(
            "SELECT state_json FROM procedure_spike_runs " +
            "WHERE run_id = @run_id FOR UPDATE", connection, transaction);
        select.Parameters.AddWithValue("run_id", runId);
        var json = (string?)await select.ExecuteScalarAsync();
        if (json is null) throw new KeyNotFoundException("Run not found");
        var current = JsonSerializer.Deserialize<ProcedureSpikeRun>(json)
            ?? throw new InvalidOperationException("Run state is unreadable");
        var next = ProcedureRuntimeSpike.Apply(definition, current, intent, now);
        if (ReferenceEquals(next, current))
        {
            await transaction.CommitAsync();
            return current;
        }

        await using (var update = new NpgsqlCommand(
            "UPDATE procedure_spike_runs SET version = @version, " +
            "state_json = @state WHERE run_id = @run_id AND version = @previous",
            connection, transaction))
        {
            update.Parameters.AddWithValue("run_id", runId);
            update.Parameters.AddWithValue("version", next.Version);
            update.Parameters.AddWithValue("previous", current.Version);
            update.Parameters.AddWithValue("state", NpgsqlTypes.NpgsqlDbType.Jsonb,
                JsonSerializer.Serialize(next));
            if (await update.ExecuteNonQueryAsync() != 1)
                throw new ProcedureSpikeConflict("Run version changed while locked");
        }
        await using (var audit = new NpgsqlCommand(
            "INSERT INTO procedure_spike_audit (command_id, run_id, version, intent_kind) " +
            "VALUES (@command_id, @run_id, @version, @kind)", connection, transaction))
        {
            audit.Parameters.AddWithValue("command_id", intent.CommandId);
            audit.Parameters.AddWithValue("run_id", runId);
            audit.Parameters.AddWithValue("version", next.Version);
            audit.Parameters.AddWithValue("kind", (int)intent.Kind);
            await audit.ExecuteNonQueryAsync();
        }

        beforeOutbox?.Invoke(); // deliberate fault point for rollback proof
        await using (var outbox = new NpgsqlCommand(
            "INSERT INTO procedure_spike_outbox " +
            "(command_id, run_id, version, event_json) " +
            "VALUES (@command_id, @run_id, @version, @event)", connection, transaction))
        {
            outbox.Parameters.AddWithValue("command_id", intent.CommandId);
            outbox.Parameters.AddWithValue("run_id", runId);
            outbox.Parameters.AddWithValue("version", next.Version);
            outbox.Parameters.AddWithValue("event", NpgsqlTypes.NpgsqlDbType.Jsonb,
                JsonSerializer.Serialize(new { runId, intent.CommandId, next.Version }));
            await outbox.ExecuteNonQueryAsync();
        }
        await transaction.CommitAsync();
        return next;
    }

    public async Task<Guid> ConfirmEffectAsync(string effectKey, Guid receiptId)
    {
        RequireDisposableDatabase();
        if (string.IsNullOrWhiteSpace(effectKey) || receiptId == Guid.Empty)
            throw new ArgumentException("Effect key and receipt are required");
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var insert = new NpgsqlCommand(
            "INSERT INTO procedure_spike_effect_receipts (effect_key, receipt_id) " +
            "VALUES (@effect_key, @receipt_id) ON CONFLICT (effect_key) DO NOTHING",
            connection, transaction))
        {
            insert.Parameters.AddWithValue("effect_key", effectKey);
            insert.Parameters.AddWithValue("receipt_id", receiptId);
            await insert.ExecuteNonQueryAsync();
        }
        await using var select = new NpgsqlCommand(
            "SELECT receipt_id FROM procedure_spike_effect_receipts " +
            "WHERE effect_key = @effect_key", connection, transaction);
        select.Parameters.AddWithValue("effect_key", effectKey);
        var existing = (Guid?)await select.ExecuteScalarAsync();
        if (existing != receiptId)
            throw new ProcedureSpikeConflict("Effect key reused with a different receipt");
        await transaction.CommitAsync();
        return existing.Value;
    }
}
