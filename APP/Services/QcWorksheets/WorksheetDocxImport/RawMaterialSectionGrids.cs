using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>The grids nested in a raw-material test cell: the titration table and the HPLC peak areas.</summary>
internal sealed partial class RawMaterialSectionBody
{
    private void ReadTables(DocxBlock block, int row, DocxCell cell)
    {
        var location = ImportProposalBuilder.At(block, row, cell.Column);
        foreach (var table in cell.NestedTables)
        {
            var labels = Enumerable.Range(0, table.Rows.Count).Select(index => ImportText.Canonical(table.Resolved(index, 0))).ToList();
            if (labels.Any(label => label.StartsWith("titre")) || (labels.Any(label => label.StartsWith("final")) && labels.Any(label => label.StartsWith("initial"))))
                AddTitration(table, location);
            else if (labels.Contains("injection"))
                AddPeakAreas(table, labels.IndexOf("injection"), location);
            else
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                    $"{sectionName}: a nested table ('{DocxDocumentReader.Describe(table)}') was not turned into fields.", location);
        }
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
