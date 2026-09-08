using System.Text.Json;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaSnapshotExecutionOrder
{
    public static async Task<IReadOnlyList<FormulaSnapshotDto>?> OrderAsync(
        ApplicationDbContext context,
        Guid responseId,
        IReadOnlyList<FormulaSnapshotDto> snapshots,
        CancellationToken cancellationToken)
    {
        var ids = snapshots.Select(item => item.Id).ToList();
        var persisted = await context.ResponseFormulaSnapshots.AsNoTracking()
            .Where(item => item.ResponseId == responseId && ids.Contains(item.Id))
            .Select(item => new { item.Id, item.PlacementKey, item.BindingsJson })
            .ToListAsync(cancellationToken);
        if (persisted.Count != snapshots.Count) return null;
        var byKey = snapshots.ToDictionary(item => item.PlacementKey,
            StringComparer.Ordinal);
        var incoming = byKey.Keys.ToDictionary(key => key, _ => 0,
            StringComparer.Ordinal);
        var outgoing = byKey.Keys.ToDictionary(key => key,
            _ => new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        try
        {
            foreach (var item in persisted)
            {
                using var document = JsonDocument.Parse(item.BindingsJson);
                foreach (var binding in document.RootElement.EnumerateArray())
                {
                    var source = binding.GetProperty("source");
                    if (source.GetProperty("sourceType").GetInt32() != 8) continue;
                    if (!FormulaResultSourceResolver.TryDependency(source,
                            out var dependency, out _) || !byKey.ContainsKey(dependency) ||
                        dependency == item.PlacementKey) return null;
                    if (outgoing[dependency].Add(item.PlacementKey))
                        incoming[item.PlacementKey] += 1;
                }
            }
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return null;
        }
        var ready = new SortedSet<string>(incoming.Where(item => item.Value == 0)
            .Select(item => item.Key), StringComparer.Ordinal);
        var ordered = new List<FormulaSnapshotDto>(snapshots.Count);
        while (ready.Count > 0)
        {
            var key = ready.Min!;
            ready.Remove(key);
            ordered.Add(byKey[key]);
            foreach (var target in outgoing[key].Order(StringComparer.Ordinal))
            {
                incoming[target] -= 1;
                if (incoming[target] == 0) ready.Add(target);
            }
        }
        return ordered.Count == snapshots.Count ? ordered : null;
    }
}
