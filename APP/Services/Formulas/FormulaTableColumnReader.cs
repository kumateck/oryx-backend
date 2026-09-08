using System.Text.Json;

#nullable enable

namespace APP.Services.Formulas;

internal static class FormulaTableColumnReader
{
    public static bool TryRead(
        JsonElement root,
        int column,
        out IReadOnlyList<string> values,
        IReadOnlyDictionary<(int Row, int Column), string>? calculatedCells = null)
    {
        var result = new List<string>();
        values = result;
        if (column < 0 || root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("tableData", out var table) ||
            table.ValueKind != JsonValueKind.Array) return false;
        for (var rowIndex = 0; rowIndex < table.GetArrayLength(); rowIndex++)
        {
            if (calculatedCells is not null &&
                calculatedCells.TryGetValue((rowIndex, column), out var calculated))
            {
                if (!string.IsNullOrWhiteSpace(calculated)) result.Add(calculated);
                continue;
            }
            var row = table[rowIndex];
            if (row.ValueKind != JsonValueKind.Array || column >= row.GetArrayLength())
                return false;
            var cell = row[column];
            switch (cell.ValueKind)
            {
                case JsonValueKind.String:
                    var text = cell.GetString();
                    if (!string.IsNullOrWhiteSpace(text)) result.Add(text);
                    break;
                case JsonValueKind.Number:
                    result.Add(cell.GetRawText());
                    break;
                case JsonValueKind.Null:
                    break;
                default:
                    return false;
            }
        }
        return result.Count > 0;
    }
}
