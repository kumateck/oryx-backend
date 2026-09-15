using System.Text.Json;
using APP.Services.FullProcedures;
using Xunit;

namespace APP.Tests.FullProcedures;

public class ProcedureActionContractSpikeTests
{
    private sealed record Fixture(
        ProcedureSpikeDefinition Graph, ProcedureSpikeRun Run,
        ProcedureActionCatalogEntry Catalog, ProcedureActionBinding Binding,
        ProcedureActionCapabilities Capabilities,
        ProcedureActionIssueManifest Manifest);

    private static Fixture Ready()
    {
        var stage = Guid.NewGuid();
        var graph = new ProcedureSpikeDefinition(Guid.NewGuid(),
            [new(stage, ProcedureSpikeNodeKind.Work, [])]);
        var entry = new ProcedureActionCatalogEntry(Guid.NewGuid(), 1,
            ProcedureActionSpikeKind.ManualWork,
            """{"instruction":"Confirm line clearance","limit":5}""");
        var binding = new ProcedureActionBinding(Guid.NewGuid(), stage,
            entry.ContractId, entry.Version, entry.Kind,
            ProcedureActionContractSpike.ContentHash(entry));
        var capabilities = new ProcedureActionCapabilities([1],
            [(ProcedureActionSpikeKind.ManualWork, 1)]);
        var manifest = ProcedureActionContractSpike.Issue(graph, [binding],
            [entry], capabilities, 1);
        return new(graph, ProcedureRuntimeSpike.Start(graph), entry,
            binding, capabilities, manifest);
    }

    [Fact]
    public void IssueRetainsExactVersionAndPayloadAfterCatalogDraftChange()
    {
        var fixture = Ready();
        var v2 = fixture.Catalog with
        {
            Version = 2,
            ContentJson = """{"instruction":"New wording","limit":8}"""
        };
        var restored = JsonSerializer.Deserialize<ProcedureActionIssueManifest>(
            JsonSerializer.Serialize(fixture.Manifest))!;
        var (blocked, action) = ProcedureActionContractSpike.Resolve(restored,
            fixture.Run, fixture.Binding.ActionOccurrenceId,
            new([1, 2], [(ProcedureActionSpikeKind.ManualWork, 1),
                (ProcedureActionSpikeKind.ManualWork, 2)]));
        Assert.Equal(ProcedureActionBlockCode.None, blocked);
        Assert.Equal(1, action!.ContractVersion);
        Assert.Equal(fixture.Catalog.ContentJson, action.FrozenContentJson);
        Assert.NotEqual(v2.ContentJson, action.FrozenContentJson);
    }

    [Fact]
    public void IssueNeverFallsForwardOrAcceptsSameVersionContentDrift()
    {
        var fixture = Ready();
        var changed = fixture.Catalog with
        {
            ContentJson = """{"instruction":"Changed in place","limit":5}"""
        };
        Assert.Throws<InvalidOperationException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [changed], fixture.Capabilities, 1));
        var v2 = fixture.Catalog with { Version = 2 };
        Assert.Throws<InvalidOperationException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [v2], new([1],
                    [(ProcedureActionSpikeKind.ManualWork, 2)]), 1));
    }

    [Fact]
    public void UnsupportedRuntimeOrActionVersionBlocksIssueAndExecution()
    {
        var fixture = Ready();
        Assert.Throws<InvalidOperationException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [fixture.Catalog], new([2],
                    [(ProcedureActionSpikeKind.ManualWork, 1)]), 1));
        Assert.Throws<InvalidOperationException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [fixture.Catalog], new([1],
                    [(ProcedureActionSpikeKind.ManualWork, 2)]), 1));
        Assert.Equal(ProcedureActionBlockCode.UnsupportedRuntime,
            ProcedureActionContractSpike.Resolve(fixture.Manifest,
                fixture.Run, fixture.Binding.ActionOccurrenceId,
                new([2], [(ProcedureActionSpikeKind.ManualWork, 1)])).Blocked);
        Assert.Equal(ProcedureActionBlockCode.UnsupportedAction,
            ProcedureActionContractSpike.Resolve(fixture.Manifest,
                fixture.Run, fixture.Binding.ActionOccurrenceId,
                new([1], [(ProcedureActionSpikeKind.ManualWork, 2)])).Blocked);
    }

    [Fact]
    public void AlteredManifestWrongRunAndMissingActionFailClosed()
    {
        var fixture = Ready();
        var altered = fixture.Manifest with
        {
            Actions = [fixture.Manifest.Actions[0] with
            {
                FrozenContentJson = """{"instruction":"Hidden change"}"""
            }]
        };
        Assert.Equal(ProcedureActionBlockCode.AlteredManifest,
            ProcedureActionContractSpike.Resolve(altered, fixture.Run,
                fixture.Binding.ActionOccurrenceId, fixture.Capabilities).Blocked);
        Assert.Equal(ProcedureActionBlockCode.WrongRun,
            ProcedureActionContractSpike.Resolve(fixture.Manifest,
                fixture.Run with { RevisionId = Guid.NewGuid() },
                fixture.Binding.ActionOccurrenceId, fixture.Capabilities).Blocked);
        Assert.Equal(ProcedureActionBlockCode.MissingAction,
            ProcedureActionContractSpike.Resolve(fixture.Manifest,
                fixture.Run, Guid.NewGuid(), fixture.Capabilities).Blocked);
    }

    [Fact]
    public void SameStageMayContainDistinctActionsButOccurrencesCannotRepeat()
    {
        var fixture = Ready();
        var second = fixture.Binding with { ActionOccurrenceId = Guid.NewGuid() };
        var issued = ProcedureActionContractSpike.Issue(fixture.Graph,
            [fixture.Binding, second], [fixture.Catalog], fixture.Capabilities, 1);
        Assert.Equal(2, issued.Actions.Length);
        Assert.All(issued.Actions, action =>
            Assert.Equal(fixture.Binding.StageOccurrenceId, action.StageOccurrenceId));
        Assert.Throws<ArgumentException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding, fixture.Binding], [fixture.Catalog],
                fixture.Capabilities, 1));
        Assert.Throws<ArgumentException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding with { StageOccurrenceId = Guid.NewGuid() }],
                [fixture.Catalog], fixture.Capabilities, 1));
    }

    [Fact]
    public void AmbiguousCatalogAndContentCannotBeIssued()
    {
        var fixture = Ready();
        Assert.Throws<ArgumentException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [fixture.Catalog, fixture.Catalog],
                fixture.Capabilities, 1));
        var ambiguous = fixture.Catalog with
        {
            ContentJson = """{"limits":{"min":1,"min":2}}"""
        };
        Assert.Throws<ArgumentException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [ambiguous], fixture.Capabilities, 1));
        var nonJson = fixture.Catalog with { ContentJson = "not json" };
        Assert.ThrowsAny<JsonException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [nonJson], fixture.Capabilities, 1));
        var unknownKind = fixture.Catalog with
        {
            Kind = (ProcedureActionSpikeKind)99
        };
        Assert.Throws<ArgumentException>(() =>
            ProcedureActionContractSpike.Issue(fixture.Graph,
                [fixture.Binding], [unknownKind], fixture.Capabilities, 1));
    }
}
