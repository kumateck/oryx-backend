using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class ProductMicroWalker
{
    [GeneratedRegex(@"Result\s+in\s+(?<unit>cfu\s*/\s*[A-Za-z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ResultUnitRegex();

    private void ReadTable(DocxBlock block)
    {
        var table = block.Table;
        var caption = _caption;
        _caption = null;

        if (SignOffTable.Is(table))
            return;

        if (MediaReference.IsMediaReferenceTable(table))
        {
            MediaReference.Apply(block, builder, _mediumCodes);
            return;
        }

        if (EquipmentTable.Is(table))
        {
            (_prefix, _inOrganism, _judgedKey) = (null, false, null);
            EquipmentTable.Apply(block, builder, _citedMedia);
            return;
        }

        if (IsPlateTable(table))
        {
            ReadEnumeration(block);
            return;
        }

        if (IsTestMatrix(table))
        {
            ReadTestMatrix(block);
            return;
        }

        if (ParameterTable.IsTwoColumnParameterTable(table))
        {
            ReadParameters(block, ParameterTable.Title(table) ?? caption);
            return;
        }

        var grid = DataGrid.Read(table);
        builder.AddTable(Join(_prefix, ImportText.SnakeKey(caption ?? "table", 30)), caption ?? "Table", grid.Columns,
            ImportProposalBuilder.At(block), "Unrecognized grid, proposed as a table");
        builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
            "A table that matches no product-sheet layout; proposed as a generic table.", ImportProposalBuilder.At(block));
    }

    /// <summary>Label | value rows under an optional title ("Pre-incubation", "Selection") or caption ("Subculture").</summary>
    private void ReadParameters(DocxBlock block, string title)
    {
        var sectionName = builder.CurrentSection?.Name;
        var group = title is null || string.Equals(title, sectionName, StringComparison.OrdinalIgnoreCase) ? null : title;
        var keyPrefix = Join(_prefix, group is null ? null : ImportText.StepKey(group));

        foreach (var (row, decision) in ParameterTable.ReadTwoColumn(block.Table))
        {
            var labelled = group is null ? decision : decision with { Label = $"{group} – {decision.Label}" };
            var field = builder.AddDecision(labelled, ImportProposalBuilder.At(block, row), keyPrefix.Length == 0 ? null : keyPrefix);
            if (field is not null && ImportText.Canonical(decision.Label) == "dilution")
                ReadDilution(block, row, decision);
        }
    }

    /// <summary>
    /// "Dilution | 1 in 10". Constant fields have no stored value, so they cannot feed a
    /// formula; a printed dilution is therefore inlined into the result formula as its numeric
    /// factor (10). A blank dilution instead gets an Entry "dilution_factor" the formula reads.
    /// </summary>
    private void ReadDilution(DocxBlock block, int row, ParameterDecision decision)
    {
        if (decision.Kind == ParameterKind.Constant && FormulaLibrary.TryParseDilutionFactor(decision.ConstantValue, out var factor))
        {
            _dilutionFactor = factor;
            return;
        }

        if (decision.Kind == ParameterKind.Constant)
            builder.Flag(WorksheetImportFlagCodes.IncompleteValue,
                $"Dilution '{decision.ConstantValue}' is not a recognizable 'a in b' dilution; the factor is entered instead.",
                ImportProposalBuilder.At(block, row));

        _dilutionFactorKey = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = "dilution_factor", Label = "Dilution factor", Type = WorksheetFieldType.Number, Mode = WorksheetFieldMode.Entry
        }, ImportProposalBuilder.At(block, row), ImportConfidence.Medium, "The dilution is entered per run, so its factor is too").FieldKey;
    }

    internal static bool IsTestMatrix(DocxTable table) =>
        table.ColumnCount >= 2 && table.Rows.Count >= 2
        && ImportText.IsBlank(table.Resolved(0, 0))
        && Enumerable.Range(1, table.ColumnCount - 1).All(column => !ImportText.IsBlank(table.Resolved(0, column)));

    /// <summary>"'' | TAMC | TYMC" with Method / Medium / Incubation rows: each column's cells are that test's constants.</summary>
    private void ReadTestMatrix(DocxBlock block)
    {
        var table = block.Table;
        for (var column = 1; column < table.ColumnCount; column++)
        {
            if (table.Cell(0, column) is { IsHorizontalSpan: true })
                continue;

            var test = table.Resolved(0, column);
            var prefix = EnumerationTests.GetValueOrDefault(ImportText.Canonical(test)) ?? ImportText.SnakeKey(test, 20);
            StartSection(test, organism: false);

            for (var row = 1; row < table.Rows.Count; row++)
                builder.AddDecision(ParameterTable.Decide(table.Resolved(row, 0), table.Resolved(row, column)),
                    ImportProposalBuilder.At(block, row, column), prefix);
        }
    }

    private static bool IsPlateTable(DocxTable table) =>
        table.ColumnCount >= 2
        && ImportText.Canonical(table.Resolved(0, 0)).StartsWith("plate1")
        && ImportText.Canonical(table.Resolved(0, 1)).StartsWith("plate2");

    /// <summary>
    /// Plate 1 / Plate 2 → ColonyCount entries; the average → a Calculated scalar from the formula
    /// library (the printed "Plate 1 + Plate 2 … cfu / 2" is not copied); the result → a
    /// Result-typed Calculated field (average × dilution factor), which is what the OOS check
    /// judges against the Specification characteristic bound to its key.
    /// </summary>
    private void ReadEnumeration(DocxBlock block)
    {
        var table = block.Table;
        var prefix = _prefix ?? "count";
        var location = ImportProposalBuilder.At(block);

        string Plate(int column, string key)
        {
            var decision = ParameterTable.DecideText(table.Resolved(0, column));
            var unit = decision?.Unit ?? "cfu";
            return builder.AddField(new ProposedWorksheetField
            {
                FieldKey = Join(prefix, key), Label = $"Plate {column + 1}", Type = WorksheetFieldType.ColonyCount,
                Mode = WorksheetFieldMode.Entry, Unit = unit
            }, ImportProposalBuilder.At(block, 0, column), ImportConfidence.High, "Plate count blank").FieldKey;
        }

        var plate1 = Plate(0, "plate_1");
        var plate2 = Plate(1, "plate_2");

        var average = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = Join(prefix, "average"), Label = "Average count", Type = WorksheetFieldType.CalculatedValue,
            Mode = WorksheetFieldMode.Calculated, Unit = "cfu", FormulaExpression = FormulaLibrary.Average(plate1, plate2)
        }, location, ImportConfidence.High, "Formula library: mean of the two plates").FieldKey;

        var resultText = Enumerable.Range(0, table.Rows.Count).Select(row => table.Resolved(row, 0))
            .FirstOrDefault(text => ResultUnitRegex().IsMatch(text));
        var unit = resultText is null ? null : ResultUnitRegex().Match(resultText).Groups["unit"].Value.Replace(" ", "");
        if (unit is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No 'Result in cfu/…' line: the result unit must be set by hand.", location);

        if (_dilutionFactor is null && _dilutionFactorKey is null)
        {
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No dilution before the counts; the dilution factor is entered per run.", location);
            _dilutionFactorKey = builder.AddField(new ProposedWorksheetField
            {
                FieldKey = "dilution_factor", Label = "Dilution factor", Type = WorksheetFieldType.Number, Mode = WorksheetFieldMode.Entry
            }, location, ImportConfidence.Low, "No printed dilution found").FieldKey;
        }

        var formula = _dilutionFactor is { } factor
            ? FormulaLibrary.CfuFromAverage(average, factor)
            : FormulaLibrary.CfuFromAverage(average, _dilutionFactorKey);

        _judgedOptions = null;
        _judgedKey = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = Join(prefix, "result"), Label = unit is null ? "Result" : $"Result ({unit})",
            Type = WorksheetFieldType.Result, Mode = WorksheetFieldMode.Calculated, Unit = unit, FormulaExpression = formula
        }, location, ImportConfidence.Medium,
            "Formula library: average count × dilution factor (printed 'Average Count x Dilution Factor'); reviewer confirms").FieldKey;
    }
}
