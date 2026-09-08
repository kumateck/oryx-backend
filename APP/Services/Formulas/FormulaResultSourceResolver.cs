using System.Text.Json;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaResultSourceResolver
{
    public static async Task<string?> ResolveAsync(
        ApplicationDbContext context,
        ResponseFormulaSnapshot snapshot,
        JsonElement source,
        IFormulaCalculationClient calculationClient,
        CancellationToken cancellationToken,
        HashSet<Guid> resolutionPath)
    {
        if (!TryDependency(source, out var placementKey, out var resultKey) ||
            placementKey == snapshot.PlacementKey) return null;
        var sourceSnapshot = await context.ResponseFormulaSnapshots.AsNoTracking()
            .Where(item => item.ResponseId == snapshot.ResponseId &&
                item.PlacementKey == placementKey)
            .OrderByDescending(item => item.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (sourceSnapshot is null) return null;
        sourceSnapshot.Response = snapshot.Response;
        var execution = await context.FormulaExecutions.AsNoTracking()
            .Where(item => item.ResponseFormulaSnapshotId == sourceSnapshot.Id &&
                item.Status == FormulaExecutionStatus.Valid &&
                item.Authority == FormulaExecutionAuthority.AuthoritativeServer)
            .OrderByDescending(item => item.ExecutedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (execution is null) return null;
        var currentInputs = await FormulaResponseInputResolver.ResolveAsync(
            context, sourceSnapshot, calculationClient, cancellationToken, resolutionPath);
        if (currentInputs.IsFailure) return null;
        var currentInputHash = FormulaCanonicalJson.HashCanonical(
            "oryx:formula-resolved-inputs:v1", currentInputs.Value);
        if (!string.Equals(currentInputHash, execution.InputHash,
                StringComparison.Ordinal)) return null;
        using var results = JsonDocument.Parse(execution.RawResultsJson);
        if (results.RootElement.ValueKind != JsonValueKind.Array) return null;
        var result = results.RootElement.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("key", out var key) && key.GetString() == resultKey);
        if (result.ValueKind != JsonValueKind.Object ||
            !result.TryGetProperty("value", out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String when !string.IsNullOrWhiteSpace(value.GetString()) =>
                value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    internal static bool TryDependency(
        JsonElement source, out string placementKey, out string resultKey)
    {
        placementKey = resultKey = string.Empty;
        if (!source.TryGetProperty("dependencies", out var dependencies) ||
            dependencies.ValueKind != JsonValueKind.Array ||
            dependencies.GetArrayLength() != 1) return false;
        var dependency = dependencies[0];
        if (!dependency.TryGetProperty("placementKey", out var placement) ||
            !dependency.TryGetProperty("resultKey", out var result)) return false;
        placementKey = placement.GetString() ?? string.Empty;
        resultKey = result.GetString() ?? string.Empty;
        if (placementKey.Length == 0 || resultKey.Length == 0 ||
            !source.TryGetProperty("reference", out var referenceElement) ||
            referenceElement.ValueKind != JsonValueKind.String) return false;
        var reference = referenceElement.GetString();
        if (reference is null || !TryReference(reference,
                out var referencedPlacement, out var referencedResult)) return false;
        return string.Equals(placementKey, referencedPlacement, StringComparison.Ordinal) &&
            string.Equals(resultKey, referencedResult, StringComparison.Ordinal);
    }

    private static bool TryReference(
        string reference, out string placementKey, out string resultKey)
    {
        placementKey = resultKey = string.Empty;
        var parts = reference.Split('/');
        if (parts.Length != 4 || parts[0] != "placement" || parts[2] != "result" ||
            !HasValidPercentEncoding(parts[1]) || !HasValidPercentEncoding(parts[3]))
            return false;
        placementKey = Uri.UnescapeDataString(parts[1]);
        resultKey = Uri.UnescapeDataString(parts[3]);
        return placementKey.Length > 0 && resultKey.Length > 0;
    }

    private static bool HasValidPercentEncoding(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%') continue;
            if (index + 2 >= value.Length || !Uri.IsHexDigit(value[index + 1]) ||
                !Uri.IsHexDigit(value[index + 2])) return false;
            index += 2;
        }
        return true;
    }
}
