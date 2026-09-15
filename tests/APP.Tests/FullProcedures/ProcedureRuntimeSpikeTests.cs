using System.Text.Json;
using APP.Services.FullProcedures;
using Xunit;

namespace APP.Tests.FullProcedures;

public class ProcedureRuntimeSpikeTests
{
    private static readonly Guid Dispense = Guid.NewGuid();
    private static readonly Guid Filling = Guid.NewGuid();
    private static readonly Guid Qc = Guid.NewGuid();
    private static readonly Guid BranchA = Guid.NewGuid();
    private static readonly Guid BranchB = Guid.NewGuid();
    private static readonly Guid Ipc = Guid.NewGuid();
    private static readonly Guid Join = Guid.NewGuid();
    private static readonly DateTimeOffset Due = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static ProcedureSpikeDefinition Graph() => new(Guid.NewGuid(),
    [
        new(Dispense, ProcedureSpikeNodeKind.Work, []),
        new(Filling, ProcedureSpikeNodeKind.Work, [Dispense]),
        new(Qc, ProcedureSpikeNodeKind.QcWait, [Filling]),
        new(BranchA, ProcedureSpikeNodeKind.Work, [Qc]),
        new(BranchB, ProcedureSpikeNodeKind.Work, [Qc]),
        new(Ipc, ProcedureSpikeNodeKind.Ipc, [BranchA], Due),
        new(Join, ProcedureSpikeNodeKind.Work, [Ipc, BranchB])
    ]);

    private static ProcedureSpikeCommand Complete(Guid node, int version) =>
        new(Guid.NewGuid(), ProcedureSpikeCommandKind.Complete, version, node);

    private static ProcedureSpikeRun ReachBranches(ProcedureSpikeDefinition graph)
    {
        var run = ProcedureRuntimeSpike.Start(graph);
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(Dispense, run.Version), Due);
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(Filling, run.Version), Due);
        return ProcedureRuntimeSpike.Apply(graph, run,
            new(Guid.NewGuid(), ProcedureSpikeCommandKind.AcceptQcResult,
                run.Version, Qc, Guid.NewGuid()), Due);
    }

    [Fact]
    public void RepeatedClearanceOccurrencesAndQcWaitAreDistinct()
    {
        var graph = Graph();
        var run = ProcedureRuntimeSpike.Start(graph);
        Assert.Equal([Dispense], ProcedureRuntimeSpike.Eligible(graph, run, Due));
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(Dispense, 0), Due);
        Assert.Equal([Filling], ProcedureRuntimeSpike.Eligible(graph, run, Due));
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(Filling, 1), Due);
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph, run, Complete(Qc, 2), Due));
        Assert.Throws<ArgumentException>(() =>
            ProcedureRuntimeSpike.Apply(graph, run,
                new(Guid.NewGuid(), ProcedureSpikeCommandKind.AcceptQcResult,
                    2, Qc, Guid.Empty), Due));
        run = ProcedureRuntimeSpike.Apply(graph, run,
            new(Guid.NewGuid(), ProcedureSpikeCommandKind.AcceptQcResult,
                2, Qc, Guid.NewGuid()), Due);
        Assert.Equal([BranchA, BranchB], ProcedureRuntimeSpike.Eligible(graph, run, Due));
    }

    [Fact]
    public void ParallelJoinWaitsForBothBranchesAndDueIpc()
    {
        var graph = Graph();
        var run = ReachBranches(graph);
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(BranchA, run.Version), Due.AddSeconds(-2));
        Assert.Equal([BranchB], ProcedureRuntimeSpike.Eligible(graph, run, Due.AddSeconds(-1)));
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(BranchB, run.Version), Due.AddSeconds(-1));
        Assert.Empty(ProcedureRuntimeSpike.Eligible(graph, run, Due.AddSeconds(-1)));
        Assert.Equal([Ipc], ProcedureRuntimeSpike.Eligible(graph, run, Due));
        run = ProcedureRuntimeSpike.Apply(graph, run, Complete(Ipc, run.Version), Due);
        Assert.Equal([Join], ProcedureRuntimeSpike.Eligible(graph, run, Due));
    }

    [Fact]
    public void IdenticalRetryAndSerializedRestartDoNotRepeatTransition()
    {
        var graph = Graph();
        var initial = ProcedureRuntimeSpike.Start(graph);
        var command = Complete(Dispense, 0);
        var run = ProcedureRuntimeSpike.Apply(graph, initial, command, Due);
        var restored = JsonSerializer.Deserialize<ProcedureSpikeRun>(JsonSerializer.Serialize(run))!;
        Assert.Same(restored, ProcedureRuntimeSpike.Apply(graph, restored, command, Due));
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph, restored, Complete(Filling, 0), Due));
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph, restored,
                command with { OccurrenceId = Filling }, Due));
    }

    [Fact]
    public void AbortRequiresRemarksAndStopsOrdinaryWork()
    {
        var graph = Graph();
        var run = ProcedureRuntimeSpike.Start(graph);
        Assert.Throws<ArgumentException>(() =>
            ProcedureRuntimeSpike.Apply(graph, run,
                new(Guid.NewGuid(), ProcedureSpikeCommandKind.Abort, 0, Remarks: "  "), Due));
        run = ProcedureRuntimeSpike.Apply(graph, run,
            new(Guid.NewGuid(), ProcedureSpikeCommandKind.Abort, 0,
                Remarks: "Unexpected result"), Due);
        Assert.True(run.Aborted);
        Assert.Equal("Unexpected result", run.AbortRemarks);
        Assert.Empty(ProcedureRuntimeSpike.Eligible(graph, run, Due));
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph, run, Complete(Dispense, 1), Due));
    }

    [Fact]
    public void CompletedRunCannotBeAborted()
    {
        var last = Guid.NewGuid();
        var graph = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(last, ProcedureSpikeNodeKind.Work, [])]);
        var run = ProcedureRuntimeSpike.Apply(graph, ProcedureRuntimeSpike.Start(graph),
            Complete(last, 0), Due);
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph, run,
                new(Guid.NewGuid(), ProcedureSpikeCommandKind.Abort, 1,
                    Remarks: "Too late"), Due));
    }

    [Fact]
    public void InvalidGraphAndRevisionMismatchFailClosed()
    {
        var graph = Graph();
        Assert.Throws<ArgumentException>(() => ProcedureRuntimeSpike.Start(graph with
        {
            Nodes = [graph.Nodes[0], graph.Nodes[0]]
        }));
        Assert.Throws<ArgumentException>(() => ProcedureRuntimeSpike.Start(graph with
        {
            Nodes = [new(Dispense, ProcedureSpikeNodeKind.Work, [Filling]),
                new(Filling, ProcedureSpikeNodeKind.Work, [Dispense])]
        }));
        var run = ProcedureRuntimeSpike.Start(graph);
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph with { RevisionId = Guid.NewGuid() },
                run, Complete(Dispense, 0), Due));
        Assert.Throws<ProcedureSpikeConflict>(() =>
            ProcedureRuntimeSpike.Apply(graph with
            {
                Nodes = [graph.Nodes[0] with { DependsOn = [BranchA] }, ..graph.Nodes.Skip(1)]
            }, run, Complete(Dispense, 0), Due));
    }
}
