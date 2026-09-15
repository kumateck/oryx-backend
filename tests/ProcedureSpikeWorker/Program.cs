using System.Diagnostics;
using System.Globalization;
using APP.Services.FullProcedures;
using Npgsql;

// Test-only child process. Its effect table is a fake adapter, not a stock ledger.
if (args.Length != 4 || !Guid.TryParse(args[0], out var runId) ||
    !Guid.TryParse(args[1], out var commandId) ||
    !DateTimeOffset.TryParse(args[2], CultureInfo.InvariantCulture,
        DateTimeStyles.RoundtripKind, out var now) ||
    args[3] is not ("before-effect" or "after-effect" or "after-receipt" or "resume"))
    throw new ArgumentException("Run, command, time and crash/resume mode are required");

var url = Environment.GetEnvironmentVariable("ORYX_PROCEDURE_SPIKE_TEST_DATABASE_URL")
    ?? throw new InvalidOperationException("Disposable database URL is required");
if (new NpgsqlConnectionStringBuilder(url).Database != "oryx_procedure_spike_test")
    throw new InvalidOperationException("Refusing worker outside disposable spike DB");

await using var dataSource = NpgsqlDataSource.Create(url);
var run = await new ProcedureSqlSpikeStore(dataSource).ReadAsync(runId);
if (run.Version != 1) throw new InvalidOperationException("Committed run did not recover");

async Task<Guid> FakeExternalEffect(string key)
{
    // A separate connection and table persist the fake effect before outbox ack.
    await using var connection = await dataSource.OpenConnectionAsync();
    await using (var insert = new NpgsqlCommand("""
        INSERT INTO procedure_spike_external_effects (effect_key, receipt_id)
        VALUES (@key, @receipt) ON CONFLICT (effect_key) DO NOTHING
        """, connection))
    {
        insert.Parameters.AddWithValue("key", key);
        insert.Parameters.AddWithValue("receipt", Guid.NewGuid());
        await insert.ExecuteNonQueryAsync();
    }
    await using var select = new NpgsqlCommand(
        "SELECT receipt_id FROM procedure_spike_external_effects WHERE effect_key = @key",
        connection);
    select.Parameters.AddWithValue("key", key);
    return (Guid)(await select.ExecuteScalarAsync()
        ?? throw new InvalidOperationException("Fake effect receipt was not stored"));
}

var worker = new ProcedureSqlOutboxSpike(dataSource);
void KillAtWindow()
{
    Process.GetCurrentProcess().Kill();
    throw new InvalidOperationException("Process unexpectedly survived kill");
}
var delivered = await worker.DeliverOneAsync(now, FakeExternalEffect,
    afterEffectBeforeAck: args[3] == "after-receipt" ? KillAtWindow : null,
    onlyCommandId: commandId,
    afterClaimBeforeEffect: args[3] == "before-effect" ? KillAtWindow : null,
    afterEffectBeforeReceipt: args[3] == "after-effect" ? KillAtWindow : null);
if (!delivered) throw new InvalidOperationException("Expected outbox event was not claimed");
