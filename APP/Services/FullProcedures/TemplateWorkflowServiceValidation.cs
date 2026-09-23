using DOMAIN.Entities.FullProcedures;

#nullable enable

namespace APP.Services.FullProcedures;

// Pure, DB-free structural validation of a workflow graph shape. This proves the declared
// graph is well-formed (single entry, every node reachable, no unbounded cycles, fork/join and
// hold/resume pairs match) but is not a full soundness proof of runtime flow semantics — see
// docs/full-procedures-workflow-revisions.md for the recorded scope limits.
internal static class TemplateWorkflowServiceValidation
{
    internal static bool ValidShape(TemplateWorkflowContentRequest request)
    {
        if (request is null || request.Name.Trim().Length is < 2 or > 150 ||
            request.Description.Trim().Length > 4000 ||
            request.Nodes.Count is < 2 or > 200 || request.Edges.Count > 500 ||
            request.Layouts.Count > request.Nodes.Count)
            return false;
        return ValidNodeShape(request.Nodes) && ValidEdgeShape(request.Nodes, request.Edges) &&
               ValidLayoutShape(request.Nodes, request.Layouts) && ValidGraphRules(request);
    }

    private static bool ValidNodeShape(IReadOnlyList<TemplateWorkflowNodeRequest> nodes) =>
        Unique(nodes.Select(item => item.Key.Trim()), StringComparer.OrdinalIgnoreCase) &&
        Unique(nodes.Select(item => item.Order)) && nodes.All(ValidNode);

    private static bool ValidNode(TemplateWorkflowNodeRequest node)
    {
        if (!ValidKey(node.Key) || node.Name.Trim().Length is < 1 or > 150 ||
            node.Order < 0 || !Enum.IsDefined(node.NodeType))
            return false;
        var kind = new[]
        {
            node.TemplateActivityId.HasValue || node.TemplateActivityRevisionId.HasValue,
            !string.IsNullOrWhiteSpace(node.JoinGroupKey),
            node.WaitKind.HasValue,
            !string.IsNullOrWhiteSpace(node.HoldGroupKey),
            !string.IsNullOrWhiteSpace(node.ReworkTargetKey) || node.ReworkMaxAttempts.HasValue,
        };
        return node.NodeType switch
        {
            TemplateWorkflowNodeType.Start or TemplateWorkflowNodeType.End
                or TemplateWorkflowNodeType.Branch => kind.All(set => !set),
            TemplateWorkflowNodeType.Activity =>
                node.TemplateActivityId is not null && node.TemplateActivityId != Guid.Empty &&
                node.TemplateActivityRevisionId is not null &&
                node.TemplateActivityRevisionId != Guid.Empty && OnlyIndexSet(kind, 0),
            TemplateWorkflowNodeType.Fork or TemplateWorkflowNodeType.Join =>
                !string.IsNullOrWhiteSpace(node.JoinGroupKey) &&
                node.JoinGroupKey.Trim().Length is >= 2 and <= 100 && OnlyIndexSet(kind, 1),
            TemplateWorkflowNodeType.Wait or TemplateWorkflowNodeType.Ipc =>
                node.WaitKind.HasValue && Enum.IsDefined(node.WaitKind.Value) &&
                (node.WaitConfiguration is null || node.WaitConfiguration.Trim().Length <= 2000) &&
                OnlyIndexSet(kind, 2),
            TemplateWorkflowNodeType.Hold or TemplateWorkflowNodeType.Resume =>
                !string.IsNullOrWhiteSpace(node.HoldGroupKey) &&
                node.HoldGroupKey.Trim().Length is >= 2 and <= 100 && OnlyIndexSet(kind, 3),
            TemplateWorkflowNodeType.Rework =>
                !string.IsNullOrWhiteSpace(node.ReworkTargetKey) && ValidKey(node.ReworkTargetKey) &&
                node.ReworkMaxAttempts is >= 1 and <= 10 && OnlyIndexSet(kind, 4),
            _ => false,
        };
    }

    private static bool OnlyIndexSet(bool[] flags, int index) =>
        flags[index] && flags.Where((_, position) => position != index).All(flag => !flag);

    private static bool ValidEdgeShape(IReadOnlyList<TemplateWorkflowNodeRequest> nodes,
        IReadOnlyList<TemplateWorkflowEdgeRequest> edges)
    {
        var nodeKeys = nodes.Select(item => item.Key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (edges.Any(edge => !ValidKey(edge.SourceKey) || !ValidKey(edge.TargetKey) ||
                string.Equals(edge.SourceKey.Trim(), edge.TargetKey.Trim(),
                    StringComparison.OrdinalIgnoreCase) ||
                !nodeKeys.Contains(edge.SourceKey.Trim()) || !nodeKeys.Contains(edge.TargetKey.Trim()) ||
                edge.Order < 0 ||
                (edge.BranchKey is not null && edge.BranchKey.Trim().Length is < 1 or > 100) ||
                (edge.BranchExpression is not null && edge.BranchExpression.Trim().Length > 500)))
            return false;
        if (!Unique(edges.Select(edge => (edge.SourceKey.Trim().ToUpperInvariant(),
                edge.TargetKey.Trim().ToUpperInvariant()))))
            return false;
        return edges.GroupBy(edge => edge.SourceKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .All(group => Unique(group.Select(edge => edge.Order)));
    }

    private static bool ValidLayoutShape(IReadOnlyList<TemplateWorkflowNodeRequest> nodes,
        IReadOnlyList<TemplateWorkflowNodeLayoutRequest> layouts)
    {
        var nodeKeys = nodes.Select(item => item.Key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Unique(layouts.Select(item => item.NodeKey.Trim()), StringComparer.OrdinalIgnoreCase) &&
               layouts.All(item => nodeKeys.Contains(item.NodeKey.Trim()) &&
                   double.IsFinite(item.PositionX) && double.IsFinite(item.PositionY));
    }

    private static bool ValidGraphRules(TemplateWorkflowContentRequest request)
    {
        var nodesByKey = request.Nodes.ToDictionary(item => item.Key.Trim(),
            StringComparer.OrdinalIgnoreCase);
        var outgoing = request.Edges.GroupBy(edge => edge.SourceKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        var incoming = request.Edges.GroupBy(edge => edge.TargetKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var starts = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Start).ToList();
        var ends = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.End).ToList();
        if (starts.Count != 1 || ends.Count == 0) return false;
        var start = starts[0];
        if (incoming.ContainsKey(start.Key.Trim()) || Degree(outgoing, start.Key) != 1) return false;
        if (ends.Any(end => outgoing.ContainsKey(end.Key.Trim()) || Degree(incoming, end.Key) < 1))
            return false;

        foreach (var node in request.Nodes)
        {
            var key = node.Key.Trim();
            var outCount = Degree(outgoing, key);
            var inCount = Degree(incoming, key);
            if (node.NodeType != TemplateWorkflowNodeType.Start && inCount < 1) return false;
            if (node.NodeType != TemplateWorkflowNodeType.End && outCount < 1) return false;
            var outs = outgoing.TryGetValue(key, out var list) ? list : [];
            if (!ValidDegreeForType(node.NodeType, outCount, inCount, outs)) return false;
        }

        var forks = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Fork).ToList();
        var joins = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Join).ToList();
        var forkKeys = forks.Select(item => item.JoinGroupKey!.Trim()).ToList();
        var joinKeys = joins.Select(item => item.JoinGroupKey!.Trim()).ToList();
        if (!Unique(forkKeys, StringComparer.OrdinalIgnoreCase) ||
            !Unique(joinKeys, StringComparer.OrdinalIgnoreCase) ||
            !forkKeys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase).SequenceEqual(
                joinKeys.OrderBy(item => item, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase))
            return false;

        var holdKeys = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Hold)
            .Select(item => item.HoldGroupKey!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resumeKeys = request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Resume)
            .Select(item => item.HoldGroupKey!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!holdKeys.SetEquals(resumeKeys)) return false;

        foreach (var node in request.Nodes.Where(item => item.NodeType == TemplateWorkflowNodeType.Rework))
        {
            var targetKey = node.ReworkTargetKey!.Trim();
            if (!nodesByKey.TryGetValue(targetKey, out var target) || target.Order >= node.Order)
                return false;
        }

        return IsDagAndReachable(request.Nodes, request.Edges, start.Key.Trim());
    }

    private static bool ValidDegreeForType(TemplateWorkflowNodeType type, int outCount, int inCount,
        IReadOnlyList<TemplateWorkflowEdgeRequest> outs) => type switch
        {
            TemplateWorkflowNodeType.Start or TemplateWorkflowNodeType.Activity or
                TemplateWorkflowNodeType.Wait or TemplateWorkflowNodeType.Ipc or
                TemplateWorkflowNodeType.Hold or TemplateWorkflowNodeType.Resume or
                TemplateWorkflowNodeType.Rework =>
                outCount == 1 && outs.All(edge => edge.BranchKey is null),
            TemplateWorkflowNodeType.Branch => outCount >= 2 && HasUniqueBranchKeys(outs),
            TemplateWorkflowNodeType.Fork => outCount >= 2 && outs.All(edge => edge.BranchKey is null),
            TemplateWorkflowNodeType.Join => inCount >= 2 && outCount == 1 &&
                outs.All(edge => edge.BranchKey is null),
            TemplateWorkflowNodeType.End => true,
            _ => false,
        };

    private static bool HasUniqueBranchKeys(IReadOnlyList<TemplateWorkflowEdgeRequest> outs)
    {
        var keys = outs.Select(edge => edge.BranchKey?.Trim()).ToArray();
        return keys.All(key => !string.IsNullOrEmpty(key)) &&
               Unique(keys, StringComparer.OrdinalIgnoreCase);
    }

    private static int Degree(IReadOnlyDictionary<string, List<TemplateWorkflowEdgeRequest>> map,
        string key) => map.TryGetValue(key.Trim(), out var list) ? list.Count : 0;

    private static bool IsDagAndReachable(IReadOnlyList<TemplateWorkflowNodeRequest> nodes,
        IReadOnlyList<TemplateWorkflowEdgeRequest> edges, string startKey)
    {
        var adjacency = edges.GroupBy(edge => edge.SourceKey.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key,
                group => group.Select(edge => edge.TargetKey.Trim()).ToList(),
                StringComparer.OrdinalIgnoreCase);
        var allKeys = nodes.Select(item => item.Key.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { startKey };
        var queue = new Queue<string>();
        queue.Enqueue(startKey);
        while (queue.Count > 0)
        {
            if (!adjacency.TryGetValue(queue.Dequeue(), out var targets)) continue;
            foreach (var target in targets)
                if (visited.Add(target)) queue.Enqueue(target);
        }
        if (!allKeys.All(visited.Contains)) return false;

        var inDegree = allKeys.ToDictionary(key => key, _ => 0, StringComparer.OrdinalIgnoreCase);
        foreach (var targets in adjacency.Values)
            foreach (var target in targets)
                if (inDegree.ContainsKey(target)) inDegree[target]++;
        var ready = new Queue<string>(inDegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var processed = 0;
        while (ready.Count > 0)
        {
            var current = ready.Dequeue();
            processed++;
            if (!adjacency.TryGetValue(current, out var targets)) continue;
            foreach (var target in targets)
            {
                inDegree[target]--;
                if (inDegree[target] == 0) ready.Enqueue(target);
            }
        }
        return processed == allKeys.Count;
    }

    private static bool ValidKey(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length is >= 2 and <= 100;

    private static bool Unique<T>(IEnumerable<T> items, IEqualityComparer<T>? comparer = null)
    {
        var array = items.ToArray();
        return array.Distinct(comparer).Count() == array.Length;
    }
}
