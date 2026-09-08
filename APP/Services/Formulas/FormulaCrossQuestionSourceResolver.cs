using System.Text.Json;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaCrossQuestionSourceResolver
{
    private const int QuestionValue = 4;
    private const int QuestionTableColumnStat = 6;

    public static async Task<string?> ResolveAsync(
        ApplicationDbContext context,
        ResponseFormulaSnapshot snapshot,
        JsonElement source,
        CancellationToken cancellationToken)
    {
        var sourceType = source.GetProperty("sourceType").GetInt32();
        var reference = source.GetProperty("reference").GetString();
        if (reference is null || !TryParse(reference, out var placementKey,
                out var member, out var column)) return null;
        var field = await context.FormFieldRevisions.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.FormRevisionId == snapshot.Response.FormRevisionId &&
                item.PlacementKey == placementKey, cancellationToken);
        if (field is null || field.PlacementKey == snapshot.PlacementKey) return null;
        var response = await context.FormResponses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ResponseId == snapshot.ResponseId &&
                item.FormFieldId == field.FormFieldId, cancellationToken);
        if (response is null) return null;
        using var value = JsonDocument.Parse(response.Value);
        var resolved = sourceType switch
        {
            QuestionValue when member == "value" => Scalar(value.RootElement),
            QuestionTableColumnStat when member == "table" && column.HasValue =>
                TableStatistic(value.RootElement, source, column.Value),
            _ => null
        };
        return resolved is not null && source.TryGetProperty("inputDecimalPlaces", out var places) &&
               !FormulaDecimalStatistics.HasPermittedScale(resolved, places.GetInt32())
            ? null : resolved;
    }

    private static bool TryParse(string reference, out string placementKey,
        out string member, out int? column)
    {
        placementKey = member = string.Empty;
        column = null;
        var parts = reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || parts[0] != "placement") return false;
        placementKey = Uri.UnescapeDataString(parts[1]);
        member = parts[2];
        if (member == "value") return parts.Length == 3;
        if (member != "table" || parts.Length != 5 || parts[3] != "column" ||
            !int.TryParse(parts[4], out var parsed) || parsed < 0) return false;
        column = parsed;
        return true;
    }

    private static string? TableStatistic(JsonElement root, JsonElement source, int column)
    {
        if (!FormulaTableColumnReader.TryRead(root, column, out var rawValues) ||
            !source.TryGetProperty("statistic", out var statistic)) return null;
        return FormulaDecimalStatistics.Calculate(
            rawValues, statistic.GetInt32(),
            OptionalInteger(source, "inputDecimalPlaces"),
            OptionalInteger(source, "tableCalculationDecimalPlaces"));
    }

    private static string? Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.String => value.GetString(),
        _ => null
    };

    private static int? OptionalInteger(JsonElement source, string property) =>
        source.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;
}
