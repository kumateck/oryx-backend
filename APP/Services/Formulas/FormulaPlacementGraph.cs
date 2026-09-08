using System.Text.Json;

namespace APP.Services.Formulas;

internal static class FormulaPlacementGraph
{
    public static bool IsAcyclic(IEnumerable<FormulaPlacementDraftRequest> placements)
    {
        var graph = placements.ToDictionary(
            item => item.PlacementKey,
            item => Dependencies(item.Bindings),
            StringComparer.Ordinal);
        if (graph.Values.SelectMany(item => item).Any(item => !graph.ContainsKey(item)))
            return false;
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return graph.Keys.All(key => Visit(key, graph, visiting, visited));
    }

    private static bool Visit(
        string key,
        IReadOnlyDictionary<string, HashSet<string>> graph,
        ISet<string> visiting,
        ISet<string> visited)
    {
        if (visited.Contains(key)) return true;
        if (!visiting.Add(key)) return false;
        if (graph[key].Any(next => !Visit(next, graph, visiting, visited))) return false;
        visiting.Remove(key);
        visited.Add(key);
        return true;
    }

    private static HashSet<string> Dependencies(JsonElement bindings)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        if (bindings.ValueKind != JsonValueKind.Array) return result;
        foreach (var binding in bindings.EnumerateArray())
        {
            if (!binding.TryGetProperty("source", out var source) ||
                !source.TryGetProperty("dependencies", out var dependencies) ||
                dependencies.ValueKind != JsonValueKind.Array) continue;
            foreach (var dependency in dependencies.EnumerateArray())
                if (dependency.TryGetProperty("placementKey", out var placement) &&
                    placement.GetString() is { Length: > 0 } key) result.Add(key);
        }
        return result;
    }
}
