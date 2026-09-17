#nullable enable
using Npgsql;

namespace APP.Services.FullProcedures;

// Disposable-schema delivery proof; adapter must own its own idempotent effect.
public sealed class ProcedureSqlOutboxSpike(NpgsqlDataSource dataSource)
{
    private sealed record Claim(Guid CommandId, int Attempt);

    private async Task<Claim?> ClaimAsync(DateTimeOffset now, Guid? onlyCommandId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var command = new NpgsqlCommand("""
            WITH candidate AS (
                SELECT command_id FROM procedure_spike_outbox
                WHERE delivered_at IS NULL
                  AND (lease_until IS NULL OR lease_until <= @now)
                  AND (@command_id IS NULL OR command_id = @command_id)
                ORDER BY version, command_id
                LIMIT 1 FOR UPDATE SKIP LOCKED
            )
            UPDATE procedure_spike_outbox AS o
            SET lease_until = @lease_until, attempts = o.attempts + 1
            FROM candidate
            WHERE o.command_id = candidate.command_id
            RETURNING o.command_id, o.attempts
            """, connection, transaction);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("lease_until", now.AddSeconds(30));
        command.Parameters.Add("command_id", NpgsqlTypes.NpgsqlDbType.Uuid).Value =
            onlyCommandId.HasValue ? onlyCommandId.Value : DBNull.Value;
        Claim? claim;
        await using (var reader = await command.ExecuteReaderAsync())
            claim = await reader.ReadAsync()
                ? new Claim(reader.GetGuid(0), reader.GetInt32(1))
                : null;
        await transaction.CommitAsync();
        return claim;
    }

    public async Task<bool> DeliverOneAsync(
        DateTimeOffset now,
        Func<string, Task<Guid>> idempotentAdapter,
        Action? afterEffectBeforeAck = null,
        Guid? onlyCommandId = null,
        Action? afterClaimBeforeEffect = null,
        Action? afterEffectBeforeReceipt = null)
    {
        if (new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).Database
            != "oryx_procedure_spike_test")
            throw new InvalidOperationException("Refusing outbox spike outside disposable DB");
        var claim = await ClaimAsync(now, onlyCommandId);
        if (claim is null) return false;

        // Never hold the database transaction during external work.
        var effectKey = $"procedure-spike-outbox-{claim.CommandId:N}";
        afterClaimBeforeEffect?.Invoke(); // forced process-death window
        var receiptId = await idempotentAdapter(effectKey);
        afterEffectBeforeReceipt?.Invoke(); // forced process-death window
        await new ProcedureSqlSpikeStore(dataSource)
            .ConfirmEffectAsync(effectKey, receiptId);
        afterEffectBeforeAck?.Invoke(); // forced crash point for the test

        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            UPDATE procedure_spike_outbox
            SET delivered_at = @now, effect_receipt_id = @receipt_id
            WHERE command_id = @command_id AND attempts = @attempt
              AND delivered_at IS NULL
            """, connection);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("receipt_id", receiptId);
        command.Parameters.AddWithValue("command_id", claim.CommandId);
        command.Parameters.AddWithValue("attempt", claim.Attempt);
        if (await command.ExecuteNonQueryAsync() != 1)
            throw new ProcedureSpikeConflict("Outbox claim lost its fencing attempt");
        return true;
    }
}
