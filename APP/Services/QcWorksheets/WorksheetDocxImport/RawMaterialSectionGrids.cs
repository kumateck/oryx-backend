using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>The grids nested in a raw-material test cell: the titration table and the HPLC peak areas.</summary>
internal sealed partial class RawMaterialSectionBody
{
    private void ReadTables(BodyCell cell)
    {
        foreach (var table in cell.Tables)
            ReadTable(table, cell.Location);
    }

    /// <summary>A grid read by its layout: titration, peak areas or other readings, else kept as a generic table (never dropped).</summary>
    private void ReadTable(DocxTable table, ImportSourceLocation location)
    {
        var labels = Enumerable.Range(0, table.Rows.Count).Select(index => ImportText.Canonical(table.Resolved(index, 0))).ToList();
        if (TitrationGrid.IsTitration(table))
            AddTitration(table, location);
        else if (labels.Contains("injection"))
            AddPeakAreas(table, labels.IndexOf("injection"), location);
        else if (ReadingsGrid.Read(table) is { } grid)
            AddReadings(grid, "readings", grid.IsAbsorbance ? "Absorbance" : "Readings", location, summaries: true);
        else
            AddGenericTable(table, location);
    }

    private void AddGenericTable(DocxTable table, ImportSourceLocation location)
    {
        var columns = DataGrid.Read(table).Columns;
        if (columns.Count == 0)
            return;
        var title = ParameterTable.Title(table);
        builder.AddTable($"{prefix}_{(title is null ? "table" : ImportText.SnakeKey(title, 30))}", title ?? "Table", columns, location,
            "Nested table kept as printed (reviewer confirms its columns)");
    }

    /// <summary>
    /// One column per solution and one fixed row per reading; the Average / SD / RSD rows are
    /// calculated fields over each column. Returns the table key and, per solution, the key of its average.
    /// </summary>
    private (string TableKey, List<(string Solution, string AverageKey)> Averages) AddReadings(
        ReadingsGrid grid, string key, string label, ImportSourceLocation location, bool summaries, bool alwaysAverage = false)
    {
        var columns = new List<GridColumn>
        {
            new() { Key = "reading", Label = grid.RowHeader, Type = WorksheetFieldType.ShortText, FixedValues = grid.Readings.ToList(), RowHeader = true, Reason = "Printed reading numbers" }
        };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "reading" };
        foreach (var solution in grid.Solutions)
        {
            var columnKey = ImportText.CamelKey(solution);
            for (var suffix = 2; !used.Add(columnKey); suffix++)
                columnKey = $"{ImportText.CamelKey(solution)}{suffix}";
            columns.Add(new GridColumn { Key = columnKey, Label = solution, Type = WorksheetFieldType.Number, Reason = grid.IsAbsorbance ? "Absorbance" : "Peak area" });
        }

        var tableKey = builder.AddTable($"{prefix}_{key}", label, columns, location, $"{label}: one column per solution, fixed reading rows").FieldKey;
        var averages = new List<(string, string)>();
        foreach (var column in columns.Skip(1))
        {
            var name = ImportText.SnakeKey(column.Label, 20);
            var reference = $"{{{tableKey}.{column.Key}}}";
            if (alwaysAverage || (summaries && grid.HasAverage))
                averages.Add((column.Label, AddCalculated($"{name}_average", $"Average – {column.Label}", $"AVG({reference})", false, null, location,
                    grid.HasAverage ? "Printed average row" : "Average of the readings, which the assay formula uses", null).FieldKey));
            if (summaries && grid.HasSd)
                AddCalculated($"{name}_sd", $"SD – {column.Label}", $"RSD({reference}) * AVG({reference}) / 100", false, null, location, "Printed 'SD' row", null);
            if (summaries && grid.HasRsd)
                AddCalculated($"{name}_rsd", $"RSD – {column.Label}", $"RSD({reference})", false, "%", location, "Printed 'RSD' row", null);
        }

        return (tableKey, averages);
    }

    /// <summary>
    /// Blank / Sample 1 / Sample 2 × Wt taken, Final volume, Initial volume, Titre → a Table with
    /// fixed row labels; Titre is the per-row calculated column final − initial.
    /// </summary>
    private void AddTitration(DocxTable table, ImportSourceLocation location)
    {
        var rowLabels = table.OriginCells(0).Skip(1).Select(item => item.Text).Where(text => !ImportText.IsBlank(text)).ToList();
        var columns = new List<GridColumn>
        {
            new() { Key = "determination", Label = "Determination", Type = WorksheetFieldType.ShortText, FixedValues = rowLabels, RowHeader = true, Reason = "Printed row labels" }
        };

        string final = null, initial = null;
        for (var index = 1; index < table.Rows.Count; index++)
        {
            var label = table.Resolved(index, 0);
            var canonical = ImportText.Canonical(label);
            if (ImportText.IsBlank(label))
                continue;

            var column = new GridColumn
            {
                Key = ImportText.CamelKey(label), Label = label.TrimEnd('.'), SourceColumn = index,
                Type = WorksheetFieldType.Number, Unit = canonical.StartsWith("wt") || canonical.StartsWith("weight") ? "g" : "mL",
                Reason = "Titration reading"
            };

            if (canonical.StartsWith("final"))
                final = column.Key;
            else if (canonical.StartsWith("initial"))
                initial = column.Key;
            else if (canonical.StartsWith("titre") && final is not null && initial is not null)
            {
                column.Mode = WorksheetFieldMode.Calculated;
                column.Formula = $"{{{final}}} - {{{initial}}}";
                column.Confidence = ImportConfidence.Medium;
                column.Reason = "Titre = final volume − initial volume (reviewer confirms)";
            }

            columns.Add(column);
        }

        builder.AddTable($"{prefix}_titration", "Titration", columns, location, "Titration grid: fixed rows, per-row titre");
    }

    /// <summary>
    /// Injection 1…n × Standard / Sample 1 / Sample 2 → a Table with fixed injection rows. The
    /// printed Average and RSD rows become calculated fields over each column.
    /// </summary>
    private void AddPeakAreas(DocxTable table, int headerRow, ImportSourceLocation location)
    {
        var header = table.OriginCells(headerRow).Skip(1).Where(item => !ImportText.IsBlank(item.Text)).ToList();
        var injections = Enumerable.Range(headerRow + 1, table.Rows.Count - headerRow - 1)
            .Select(index => table.Resolved(index, 0).Trim())
            .Where(label => label.Length > 0 && label.All(char.IsDigit))
            .ToList();
        var summaries = Enumerable.Range(headerRow + 1, table.Rows.Count - headerRow - 1)
            .Select(index => ImportText.Canonical(table.Resolved(index, 0)))
            .ToList();

        var columns = new List<GridColumn>
        {
            new() { Key = "injection", Label = "Injection", Type = WorksheetFieldType.ShortText, FixedValues = injections, RowHeader = true, Reason = "Printed injection numbers" }
        };
        columns.AddRange(header.Select(item => new GridColumn
        {
            Key = ImportText.CamelKey(item.Text), Label = item.Text, Type = WorksheetFieldType.Number, SourceColumn = item.Column, Reason = "Peak area"
        }));

        var key = builder.AddTable($"{prefix}_peak_areas", "Peak areas", columns, location, "Peak-area grid: fixed injection rows").FieldKey;

        foreach (var column in columns.Skip(1))
        {
            if (summaries.Contains("average"))
                AddCalculated($"{ImportText.SnakeKey(column.Label, 20)}_average_area", $"Average peak area – {column.Label}",
                    FormulaLibrary.ColumnAverage(key, column.Key), false, null, location, "Printed 'Average' row", null);
            if (summaries.Contains("rsd"))
                AddCalculated($"{ImportText.SnakeKey(column.Label, 20)}_rsd", $"RSD – {column.Label}",
                    $"RSD({{{key}.{column.Key}}})", false, "%", location, "Printed 'RSD' row", null);
        }
    }
}
