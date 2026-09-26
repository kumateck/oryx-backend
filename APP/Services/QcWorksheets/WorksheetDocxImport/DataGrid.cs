using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public sealed class DataGridResult
{
    public int HeaderRows { get; init; }
    public IReadOnlyList<int> DataRows { get; init; } = [];
    public List<GridColumn> Columns { get; init; } = [];

    /// <summary>True when at least one column is fixed, so the table has a fixed row count.</summary>
    public bool HasFixedRows => Columns.Any(column => column.FixedValues is not null);
}

/// <summary>
/// A table with one or two header rows. Columns filled in every data row become template-owned
/// <c>fixedValues</c>; empty columns become Entry columns; a header cell spanning several
/// columns becomes their <c>group</c>; units come from the header text.
/// </summary>
public static partial class DataGrid
{
    [GeneratedRegex(@"^\(([^()]+)\)$")]
    private static partial Regex UnitOnlyRegex();

    [GeneratedRegex(@"\(([^()]+)\)")]
    private static partial Regex ParenthesisRegex();

    public static int HeaderRowCount(DocxTable table)
    {
        if (table.Rows.Count < 2)
            return 1;

        var count = 1;
        while (count < table.Rows.Count - 1 && count < 3)
        {
            var spansAbove = table.Rows[count - 1].Any(cell => cell.IsHorizontalSpan);
            var mergedFromAbove = table.Rows[count].Any(cell => cell.OriginRow < count && !cell.IsHorizontalSpan);
            if (!spansAbove && !mergedFromAbove)
                break;
            count++;
        }

        return count;
    }

    public static DataGridResult Read(DocxTable table)
    {
        var headerRows = HeaderRowCount(table);
        var dataRows = Enumerable.Range(headerRows, Math.Max(0, table.Rows.Count - headerRows)).ToList();
        while (dataRows.Count > 0 && table.RowTexts(dataRows[^1]).All(ImportText.IsBlank))
            dataRows.RemoveAt(dataRows.Count - 1);

        var columns = new List<GridColumn>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var column = 0; column < table.ColumnCount; column++)
        {
            // A header cell spanning columns names each of them once, at its origin.
            var (label, group, unit) = Header(table, headerRows, column);
            if (label.Length == 0 && group is null)
                label = $"Column {column + 1}";

            var (type, inferredUnit) = ColumnTypes.Infer(label, unit);
            var key = Unique(group is null ? ImportText.CamelKey(label) : $"{ImportText.CamelKey(group)}_{ImportText.CamelKey(label)}", usedKeys);
            var gridColumn = new GridColumn
            {
                Key = key, Label = label, Group = group, Unit = unit ?? inferredUnit, Type = type, SourceColumn = column
            };

            var values = dataRows.Select(row => table.Resolved(row, column)).ToList();
            var filled = values.Count(value => !ImportText.IsBlank(value));

            if (values.Count > 0 && filled == values.Count)
            {
                gridColumn.FixedValues = values;
                gridColumn.Reason = "Printed in every row: template-owned fixed values";
            }
            else if (filled > 0)
            {
                gridColumn.Confidence = ImportConfidence.Low;
                gridColumn.FlagCode = WorksheetImportFlagCodes.MixedColumn;
                gridColumn.Reason = $"Printed in {filled} of {values.Count} rows ({string.Join(", ", values.Where(v => !ImportText.IsBlank(v)))}); proposed as Entry";
            }
            else
            {
                gridColumn.Reason = "Empty in every row: entered by the analyst";
            }

            columns.Add(gridColumn);
        }

        var firstFixed = columns.FirstOrDefault(column => column.FixedValues is not null);
        if (firstFixed is not null)
            firstFixed.RowHeader = true;

        return new DataGridResult { HeaderRows = headerRows, DataRows = dataRows, Columns = columns };
    }

    private static (string Label, string Group, string Unit) Header(DocxTable table, int headerRows, int column)
    {
        var top = table.Resolved(0, column);

        // A distinct second-row header makes the top cell its group ("New Batch" → "Plate 1"),
        // even when the top cell covers a single column; a vertically merged top cell has no
        // sub-header of its own; a unit-only sub-header ("(CFU/4Hrs)") is just the unit.
        string label = top, group = null;
        if (headerRows >= 2)
        {
            var sub = table.Cell(headerRows - 1, column);
            var subText = sub is null || (sub.OriginRow < headerRows - 1 && !sub.IsHorizontalSpan) ? null : table.Resolved(headerRows - 1, column);
            if (!string.IsNullOrWhiteSpace(subText) && subText != top)
            {
                if (UnitOnlyRegex().IsMatch(subText))
                    return (top, null, UnitOnlyRegex().Match(subText).Groups[1].Value.Replace(" ", ""));
                (label, group) = (subText, top.Length > 0 ? top : null);
            }
        }

        var unitMatch = ParenthesisRegex().Match(label);
        string unit = null;
        if (unitMatch.Success && unitMatch.Groups[1].Value.Contains("CFU", StringComparison.OrdinalIgnoreCase))
        {
            unit = unitMatch.Groups[1].Value.Split(" of ", StringSplitOptions.TrimEntries)[0].Replace(" ", "");
            label = ImportText.Normalize(label.Remove(unitMatch.Index, unitMatch.Length));
        }

        return (label, group, unit);
    }

    private static string Unique(string key, HashSet<string> used)
    {
        var candidate = key;
        for (var suffix = 2; !used.Add(candidate); suffix++)
            candidate = $"{key}{suffix}";
        return candidate;
    }
}
