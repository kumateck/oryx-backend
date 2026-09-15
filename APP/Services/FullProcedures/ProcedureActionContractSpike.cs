#nullable enable
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace APP.Services.FullProcedures;

// Isolated issue/interpretation proof; neither a governed catalog nor an effect adapter.
public enum ProcedureActionSpikeKind
{
    ManualWork = 0, QcResultWait = 1, IpcCheck = 2, MaterialEffect = 3
}

public enum ProcedureActionBlockCode
{
    None = 0, AlteredManifest = 1, WrongRun = 2,
    MissingAction = 3, UnsupportedRuntime = 4, UnsupportedAction = 5
}

public sealed record ProcedureActionCatalogEntry(
    Guid ContractId, int Version, ProcedureActionSpikeKind Kind, string ContentJson);

public sealed record ProcedureActionBinding(
    Guid ActionOccurrenceId, Guid StageOccurrenceId, Guid ContractId,
    int ContractVersion, ProcedureActionSpikeKind Kind, string ExpectedContentHash);

public sealed record ProcedureActionSnapshot(
    Guid ActionOccurrenceId, Guid StageOccurrenceId, Guid ContractId,
    int ContractVersion, ProcedureActionSpikeKind Kind,
    string FrozenContentJson, string ContentHash);

public sealed record ProcedureActionIssueManifest(
    Guid ProcedureRevisionId, string ProcedureDefinitionHash,
    int RuntimeInterpreterVersion, ProcedureActionSnapshot[] Actions,
    string ManifestHash);

public sealed record ProcedureActionCapabilities(
    int[] RuntimeVersions, (ProcedureActionSpikeKind Kind, int Version)[] ActionVersions)
{
    public bool SupportsRuntime(int version) => RuntimeVersions.Contains(version);
    public bool SupportsAction(ProcedureActionSpikeKind kind, int version) =>
        ActionVersions.Contains((kind, version));
}

public static class ProcedureActionContractSpike
{
    private static string Hash(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    // Raw retained bytes are hashed; this is not a canonical approval signature.
    public static string ContentHash(ProcedureActionCatalogEntry entry) =>
        Hash(JsonSerializer.Serialize(new { entry.ContractId, entry.Version,
            entry.Kind, entry.ContentJson }));

    private static string ManifestHash(ProcedureActionIssueManifest manifest) =>
        Hash(JsonSerializer.Serialize(new
        {
            manifest.ProcedureRevisionId,
            manifest.ProcedureDefinitionHash,
            manifest.RuntimeInterpreterVersion,
            manifest.Actions
        }));

    private static void RequireUnambiguousJson(string content)
    {
        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Action contract content must be a JSON object");
        void Check(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new ArgumentException("Duplicate action contract JSON property");
                    Check(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var value in element.EnumerateArray()) Check(value);
        }
        Check(document.RootElement);
    }

    public static ProcedureActionIssueManifest Issue(
        ProcedureSpikeDefinition graph,
        ProcedureActionBinding[] bindings,
        ProcedureActionCatalogEntry[] catalog,
        ProcedureActionCapabilities capabilities,
        int runtimeInterpreterVersion)
    {
        var run = ProcedureRuntimeSpike.Start(graph);
        if (bindings.Length == 0 || !capabilities.SupportsRuntime(runtimeInterpreterVersion))
            throw new InvalidOperationException("Actions or exact runtime version unavailable");
        var byContract = new Dictionary<(Guid, int), ProcedureActionCatalogEntry>();
        foreach (var entry in catalog)
        {
            if (entry.ContractId == Guid.Empty || entry.Version < 1 ||
                !Enum.IsDefined(entry.Kind) ||
                !byContract.TryAdd((entry.ContractId, entry.Version), entry))
                throw new ArgumentException("Contract identity/version must be unique");
            RequireUnambiguousJson(entry.ContentJson);
        }
        var stages = graph.Nodes.Select(node => node.OccurrenceId).ToHashSet();
        var actions = new HashSet<Guid>();
        var snapshots = new List<ProcedureActionSnapshot>();
        foreach (var binding in bindings)
        {
            if (binding.ActionOccurrenceId == Guid.Empty ||
                !actions.Add(binding.ActionOccurrenceId) ||
                !stages.Contains(binding.StageOccurrenceId))
                throw new ArgumentException("Action occurrence or stage is invalid");
            if (!byContract.TryGetValue((binding.ContractId, binding.ContractVersion),
                    out var entry) || entry.Kind != binding.Kind ||
                ContentHash(entry) != binding.ExpectedContentHash ||
                !capabilities.SupportsAction(entry.Kind, entry.Version))
                throw new InvalidOperationException(
                    "Exact approved action contract or interpreter is unavailable");
            snapshots.Add(new(binding.ActionOccurrenceId, binding.StageOccurrenceId,
                entry.ContractId, entry.Version, entry.Kind,
                entry.ContentJson, ContentHash(entry)));
        }
        var manifest = new ProcedureActionIssueManifest(graph.RevisionId,
            run.DefinitionHash, runtimeInterpreterVersion,
            [..snapshots.OrderBy(action => action.ActionOccurrenceId)], "");
        return manifest with { ManifestHash = ManifestHash(manifest) };
    }

    public static (ProcedureActionBlockCode Blocked, ProcedureActionSnapshot? Action)
        Resolve(ProcedureActionIssueManifest manifest, ProcedureSpikeRun run,
            Guid actionOccurrenceId, ProcedureActionCapabilities capabilities)
    {
        if (manifest.Actions.Length == 0 ||
            manifest.Actions.Select(action => action.ActionOccurrenceId).Distinct().Count()
                != manifest.Actions.Length ||
            manifest.Actions.Any(action => action.ActionOccurrenceId == Guid.Empty ||
                action.StageOccurrenceId == Guid.Empty ||
                action.ContractId == Guid.Empty || action.ContractVersion < 1 ||
                !Enum.IsDefined(action.Kind)) ||
            ManifestHash(manifest) != manifest.ManifestHash ||
            manifest.Actions.Any(action => ContentHash(new(action.ContractId,
                action.ContractVersion, action.Kind, action.FrozenContentJson)) !=
                action.ContentHash))
            return (ProcedureActionBlockCode.AlteredManifest, null);
        if (run.RevisionId != manifest.ProcedureRevisionId ||
            run.DefinitionHash != manifest.ProcedureDefinitionHash)
            return (ProcedureActionBlockCode.WrongRun, null);
        var action = manifest.Actions.SingleOrDefault(candidate =>
            candidate.ActionOccurrenceId == actionOccurrenceId);
        if (action is null)
            return (ProcedureActionBlockCode.MissingAction, null);
        if (!capabilities.SupportsRuntime(manifest.RuntimeInterpreterVersion))
            return (ProcedureActionBlockCode.UnsupportedRuntime, null);
        if (!capabilities.SupportsAction(action.Kind, action.ContractVersion))
            return (ProcedureActionBlockCode.UnsupportedAction, null);
        return (ProcedureActionBlockCode.None, action);
    }
}
