using APP.Services.FullProcedures;
using Npgsql;
using Xunit;

namespace APP.Tests.FullProcedures;

[CollectionDefinition("Procedure SQL Spike", DisableParallelization = true)]
public class ProcedureSqlSpikeCollection { }

[Collection("Procedure SQL Spike")]
public class ProcedureSqlOutboxSpikeTests
{
    [DisposablePostgresFact]
    public async Task FailureAfterFakeEffectRetriesWithoutRepeatingEffect()
    {
        await using var dataSource = await ProcedureSqlSpikeStoreTests.OpenDisposableAsync();
        var runId = Guid.NewGuid();
        var node = Guid.NewGuid();
        var graph = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(node, ProcedureSpikeNodeKind.Work, [])]);
        var command = new ProcedureSpikeCommand(Guid.NewGuid(),
            ProcedureSpikeCommandKind.Complete, 0, node);
        var store = new ProcedureSqlSpikeStore(dataSource);
        var now = DateTimeOffset.UtcNow;
        await store.CreateAsync(runId, graph);
        await store.ExecuteAsync(runId, graph, command, now);

        var fakeReceipts = new Dictionary<string, Guid>();
        var physicalEffects = 0;
        Task<Guid> FakeIdempotentAdapter(string effectKey)
        {
            if (!fakeReceipts.TryGetValue(effectKey, out var receipt))
            {
                receipt = Guid.NewGuid();
                fakeReceipts[effectKey] = receipt;
                physicalEffects++;
            }
            return Task.FromResult(receipt);
        }

        var dispatcher = new ProcedureSqlOutboxSpike(dataSource);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DeliverOneAsync(now, FakeIdempotentAdapter,
                afterEffectBeforeAck: () =>
                    throw new InvalidOperationException("forced worker death"),
                onlyCommandId: command.CommandId));
        Assert.Equal(1, physicalEffects);

        await using (var connection = await dataSource.OpenConnectionAsync())
        await using (var check = new NpgsqlCommand(
            "SELECT delivered_at, attempts FROM procedure_spike_outbox " +
            "WHERE command_id = @command_id", connection))
        {
            check.Parameters.AddWithValue("command_id", command.CommandId);
            await using var reader = await check.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.True(await reader.IsDBNullAsync(0));
            Assert.Equal(1, reader.GetInt32(1));
        }

        Assert.True(await dispatcher.DeliverOneAsync(
            now.AddSeconds(31), FakeIdempotentAdapter,
            onlyCommandId: command.CommandId));
        Assert.Equal(1, physicalEffects);
        Assert.False(await dispatcher.DeliverOneAsync(
            now.AddSeconds(32), FakeIdempotentAdapter,
            onlyCommandId: command.CommandId));
        await using var finalConnection = await dataSource.OpenConnectionAsync();
        await using var finalCheck = new NpgsqlCommand(
            "SELECT attempts, effect_receipt_id, delivered_at " +
            "FROM procedure_spike_outbox WHERE command_id = @command_id",
            finalConnection);
        finalCheck.Parameters.AddWithValue("command_id", command.CommandId);
        await using var finalReader = await finalCheck.ExecuteReaderAsync();
        Assert.True(await finalReader.ReadAsync());
        Assert.Equal(2, finalReader.GetInt32(0));
        Assert.Equal(fakeReceipts.Values.Single(), finalReader.GetGuid(1));
        Assert.False(await finalReader.IsDBNullAsync(2));
    }
}
