#nullable enable
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace APP.Services.FullProcedures;

// Phase-0 semantics proof only: no persistence, permissions, signatures, or effects.
public enum ProcedureSpikeNodeKind { Work = 0, QcWait = 1, Ipc = 2 }
public enum ProcedureSpikeCommandKind { Complete = 0, AcceptQcResult = 1, Abort = 2 }

public sealed record ProcedureSpikeNode(
    Guid OccurrenceId,
    ProcedureSpikeNodeKind Kind,
    Guid[] DependsOn,
    DateTimeOffset? DueAt = null);

public sealed record ProcedureSpikeDefinition(Guid RevisionId, ProcedureSpikeNode[] Nodes);

public sealed record ProcedureSpikeRun(
    Guid RevisionId,
    string DefinitionHash,
    int Version,
    Guid[] Completed,
    bool Aborted,
    string? AbortRemarks,
    Dictionary<Guid, string> CommandReceipts);

public sealed record ProcedureSpikeCommand(
    Guid CommandId,
    ProcedureSpikeCommandKind Kind,
    int ExpectedVersion,
    Guid? OccurrenceId = null,
    Guid? ApprovedQcReceiptId = null,
    string? Remarks = null);

public sealed class ProcedureSpikeConflict(string message) : Exception(message);

public static class ProcedureRuntimeSpike
{
    private static string Hash(ProcedureSpikeDefinition definition) =>
        Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition))));

    public static void Validate(ProcedureSpikeDefinition definition)
    {
        if (definition.RevisionId == Guid.Empty || definition.Nodes.Length == 0)
            throw new ArgumentException("A revision and nodes are required");

        var nodes = new Dictionary<Guid, ProcedureSpikeNode>();
        foreach (var node in definition.Nodes)
        {
            if (node.OccurrenceId == Guid.Empty || !nodes.TryAdd(node.OccurrenceId, node))
                throw new ArgumentException("Node occurrence IDs must be unique and nonempty");
            if (node.Kind == ProcedureSpikeNodeKind.Ipc && node.DueAt is null)
                throw new ArgumentException("IPC requires a due time");
        }

        var visiting = new HashSet<Guid>();
        var visited = new HashSet<Guid>();
        void Visit(Guid id)
        {
            if (visiting.Contains(id))
                throw new ArgumentException("Cycles require explicit rework semantics");
            if (visited.Contains(id)) return;
            if (!nodes.TryGetValue(id, out var node))
                throw new ArgumentException("Unknown predecessor");
            visiting.Add(id);
            foreach (var predecessor in node.DependsOn) Visit(predecessor);
            visiting.Remove(id);
            visited.Add(id);
        }
        foreach (var node in definition.Nodes) Visit(node.OccurrenceId);
    }

    public static ProcedureSpikeRun Start(ProcedureSpikeDefinition definition)
    {
        Validate(definition);
        return new ProcedureSpikeRun(
            definition.RevisionId, Hash(definition), 0, [], false, null,
            new Dictionary<Guid, string>());
    }

    public static Guid[] Eligible(
        ProcedureSpikeDefinition definition,
        ProcedureSpikeRun run,
        DateTimeOffset now)
    {
        if (run.Aborted || run.RevisionId != definition.RevisionId
            || run.DefinitionHash != Hash(definition)) return [];
        var completed = run.Completed.ToHashSet();
        return definition.Nodes
            .Where(node => !completed.Contains(node.OccurrenceId)
                && node.DependsOn.All(completed.Contains)
                && (node.Kind != ProcedureSpikeNodeKind.Ipc || now >= node.DueAt))
            .Select(node => node.OccurrenceId)
            .ToArray();
    }

    public static ProcedureSpikeRun Apply(
        ProcedureSpikeDefinition definition,
        ProcedureSpikeRun run,
        ProcedureSpikeCommand command,
        DateTimeOffset now)
    {
        if (run.RevisionId != definition.RevisionId
            || run.DefinitionHash != Hash(definition))
            throw new ProcedureSpikeConflict("Run is pinned to another definition content");
        if (command.CommandId == Guid.Empty)
            throw new ArgumentException("Command ID is required");

        // ExpectedVersion is excluded so an identical retry returns its first receipt.
        var fingerprint = JsonSerializer.Serialize(new
        {
            command.Kind,
            command.OccurrenceId,
            command.ApprovedQcReceiptId,
            command.Remarks
        });
        if (run.CommandReceipts.TryGetValue(command.CommandId, out var previous))
        {
            if (previous != fingerprint)
                throw new ProcedureSpikeConflict("Command ID reused with different intent");
            return run;
        }
        if (command.ExpectedVersion != run.Version)
            throw new ProcedureSpikeConflict("Expected run version is stale");
        if (run.Aborted)
            throw new ProcedureSpikeConflict("Aborted work cannot progress");

        Guid[] completed = run.Completed;
        var aborted = false;
        string? remarks = null;
        if (command.Kind == ProcedureSpikeCommandKind.Abort)
        {
            if (string.IsNullOrWhiteSpace(command.Remarks))
                throw new ArgumentException("Abort remarks are required");
            if (run.Completed.Length == definition.Nodes.Length)
                throw new ProcedureSpikeConflict("Completed work cannot be aborted");
            aborted = true;
            remarks = command.Remarks.Trim();
        }
        else
        {
            var node = definition.Nodes.SingleOrDefault(
                candidate => candidate.OccurrenceId == command.OccurrenceId);
            if (node is null) throw new ArgumentException("Unknown node occurrence");
            if (node.Kind == ProcedureSpikeNodeKind.QcWait)
            {
                if (command.Kind != ProcedureSpikeCommandKind.AcceptQcResult)
                    throw new ProcedureSpikeConflict("QC wait requires a QC result command");
                if (command.ApprovedQcReceiptId is null || command.ApprovedQcReceiptId == Guid.Empty)
                    throw new ArgumentException("An approved QC receipt reference is required");
            }
            else if (command.Kind != ProcedureSpikeCommandKind.Complete)
                throw new ProcedureSpikeConflict("QC result cannot complete ordinary work");
            if (!Eligible(definition, run, now).Contains(node.OccurrenceId))
                throw new ProcedureSpikeConflict("Predecessor, due-time or state gate is unmet");
            completed = [..run.Completed, node.OccurrenceId];
        }

        var receipts = new Dictionary<Guid, string>(run.CommandReceipts)
        {
            [command.CommandId] = fingerprint
        };
        return run with
        {
            Version = run.Version + 1,
            Completed = completed,
            Aborted = aborted,
            AbortRemarks = remarks,
            CommandReceipts = receipts
        };
    }
}
