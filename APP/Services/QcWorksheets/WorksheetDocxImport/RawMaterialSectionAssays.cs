using APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The table-shaped definitions (brief 12): titration, replicate readings and numbered weighings.
/// <para>
/// A worksheet formula can reach a table only through a whole column (<c>AVG({table.column})</c>) or,
/// inside a calculated column, the cells of its own row. So the assay of each sample is calculated
/// in that sample's row, and anything every sample shares — the blank titre, the standard's mean
/// reading — is a field of its own rather than a row of the same table.
/// </para>
/// </summary>
internal sealed partial class RawMaterialSectionBody
{
    /// <summary>
    /// Sample rows × weight, burette readings, titre and % assay; the blank's readings are fields,
    /// because every sample row's assay subtracts the same blank titre.
    /// </summary>
    private void ApplyTitration(List<(DocxTable Table, ImportSourceLocation Location)> tables)
    {
        AddMissingInputs();

        var printed = tables.FirstOrDefault(item => TitrationGrid.IsTitration(item.Table));
        var grid = printed.Table is null ? null : TitrationGrid.Read(printed.Table);
        var location = printed.Location ?? _end;
        var table = Definition.Table;

        var determinations = grid?.Determinations ?? ["Blank", .. table.Rows];
        var quantities = grid?.Quantities ?? table.Columns.Select(column => column.Label).ToList();
        var samples = determinations.Where(item => !TitrationGrid.IsBlank(item)).ToList();
        var hasBlank = determinations.Any(TitrationGrid.IsBlank);
        var hasReadings = quantities.Any(TitrationGrid.IsFinal) && quantities.Any(TitrationGrid.IsInitial);
        // The printed formula has no unit conversion, so the weight is entered in the equivalence's unit.
        var weightUnit = _literals.GetValueOrDefault("equivalence:unit");

        var columns = new List<GridColumn>
        {
            new() { Key = "determination", Label = table.RowHeader, Type = WorksheetFieldType.ShortText, FixedValues = samples, RowHeader = true, Reason = "Printed sample columns" }
        };

        string final = null, initial = null, titre = null, weight = null, blankFinal = null, blankInitial = null;
        foreach (var (quantity, index) in quantities.Select((quantity, index) => (quantity, index)))
        {
            var column = new GridColumn
            {
                Key = ImportText.CamelKey(quantity), Label = quantity, SourceColumn = index + 1, Type = WorksheetFieldType.Number,
                Unit = "mL", Reason = "Titration reading"
            };

            if (TitrationGrid.IsWeight(quantity))
            {
                weight = column.Key;
                column.Unit = weightUnit ?? "g";
                column.Confidence = ImportConfidence.Medium;
                column.Reason = weightUnit is not null
                    ? $"Sample weight; the printed formula has no unit conversion, so it is entered in the equivalence's unit ({weightUnit})"
                    : "Sample weight; unit not printed (reviewer confirms)";
            }
            else if (TitrationGrid.IsFinal(quantity))
                final = column.Key;
            else if (TitrationGrid.IsInitial(quantity))
                initial = column.Key;
            else if (TitrationGrid.IsTitre(quantity))
            {
                titre = column.Key;
                if (hasReadings && final is not null && initial is not null)
                {
                    column.Mode = WorksheetFieldMode.Calculated;
                    column.Formula = $"{{{final}}} - {{{initial}}}";
                    column.Confidence = ImportConfidence.Medium;
                    column.Reason = "Titre = final volume − initial volume (reviewer confirms)";
                }
            }

            columns.Add(column);

            // The blank column of the printed grid: one field per reading, and its titre.
            if (!hasBlank || TitrationGrid.IsWeight(quantity))
                continue;

            if (TitrationGrid.IsTitre(quantity))
            {
                _calculated["blank_titre"] = blankFinal is not null && blankInitial is not null
                    ? AddCalculated("blank_titre", $"Blank – {quantity}", $"{{{blankFinal}}} - {{{blankInitial}}}", false, "mL", location,
                        "Blank titre = final volume − initial volume (reviewer confirms)", null).FieldKey
                    : AddEntry("blank_titre", $"Blank – {quantity}", WorksheetFieldType.Number, "mL", location, "Blank titration reading").FieldKey;
                continue;
            }

            var blank = AddEntry($"blank_{ImportText.SnakeKey(quantity, 24)}", $"Blank – {quantity}", WorksheetFieldType.Number, "mL", location,
                "Blank titration reading: the printed grid's Blank column").FieldKey;
            if (TitrationGrid.IsFinal(quantity))
                blankFinal = blank;
            else if (TitrationGrid.IsInitial(quantity))
                blankInitial = blank;
        }

        foreach (var (key, label, unit) in new[] { (weight, "Wt. taken", weightUnit ?? "g"), (titre, "Titre obtained", "mL") })
        {
            if (key is not null)
                continue;
            columns.Add(new GridColumn
            {
                Key = ImportText.CamelKey(label), Label = label, Type = WorksheetFieldType.Number, Unit = unit, Confidence = ImportConfidence.Medium,
                Reason = $"Required by the '{Definition.Title}' definition but not printed in the grid; added",
                FlagCode = WorksheetImportFlagCodes.DefinitionInputAdded
            });
        }

        var row = new Dictionary<string, string> { ["titre"] = titre ?? "titreObtained", ["weight"] = weight ?? "wtTaken" };
        var assay = Definition.Calculations.First(calculation => !calculation.IsResult);
        var result = Definition.Calculations.First(calculation => calculation.IsResult);

        var read = FromPrintedTerms();
        var template = read?.Template ?? (hasBlank ? assay.Formula : assay.Formula.Replace("({row:titre} - {blank_titre})", "{row:titre}"));
        var formula = Expand(template, 0, row);
        var valid = QcFormulaEvaluator.Analyze(formula).IsValid;
        columns.Add(new GridColumn
        {
            Key = "assay", Label = assay.Label, Type = WorksheetFieldType.Number, Unit = assay.Unit,
            Mode = WorksheetFieldMode.Calculated, Formula = valid ? formula : null,
            Confidence = valid && !PrintUnread(read?.Text) ? ImportConfidence.Medium : ImportConfidence.Low,
            Reason = valid ? Provenance(read?.Text) : "The definition's formula could not be completed from this sheet; enter it before saving",
            FlagCode = valid ? WorksheetImportFlagCodes.FormulaFromDefinition : WorksheetImportFlagCodes.FormulaNeedsReview
        });

        var tableKey = builder.AddTable($"{prefix}_{table.Key}", table.Label, columns, location,
            "Titration: one row per sample, titre and % assay calculated in each row").FieldKey;
        if (grid is null)
            builder.Flag(WorksheetImportFlagCodes.DefinitionInputAdded,
                $"{sectionName}: the titration grid is required by the '{Definition.Title}' definition but is not printed on the sheet; it was added.", location);

        AddOtherTables(tables, printed.Table);
        AddDefinitionCalculation(result.Key, PrintedLabelFor(result), $"AVG({{{tableKey}.assay}})", result.Unit, true, _end, read?.Text,
            WorksheetImportFlagCodes.FormulaFromDefinition);
    }

    /// <summary>
    /// A readings table (peak areas, absorbances) with one column per solution; each column's mean
    /// is a calculated field, and each sample's assay is calculated from its own mean.
    /// </summary>
    private void ApplyReadings(List<(DocxTable Table, ImportSourceLocation Location)> tables)
    {
        AddMissingInputs();

        var table = Definition.Table;
        var printed = tables.Select(item => (item.Table, item.Location, Grid: ReadingsGrid.Read(item.Table))).FirstOrDefault(item => item.Grid is not null);
        var location = printed.Location ?? _end;
        var grid = printed.Grid ?? ReadingsGrid.Default(table.RowHeader, table.Rows, table.Columns.Select(column => column.Label).ToList());

        var (_, averages) = AddReadings(grid, table.Key, table.Label, location, summaries: true, alwaysAverage: true);
        if (printed.Grid is null)
            builder.Flag(WorksheetImportFlagCodes.DefinitionInputAdded,
                $"{sectionName}: the '{table.Label}' table is required by the '{Definition.Title}' definition but is not printed on the sheet; it was added.", location);

        _standardReading = averages.FirstOrDefault(item => ReadingsGrid.IsStandard(item.Solution)).AverageKey;
        _sampleReadings.AddRange(averages.Where(item => !ReadingsGrid.IsStandard(item.Solution)).Select(item => item.AverageKey));
        AddOtherTables(tables, printed.Table);

        var assay = Definition.Calculations.First(calculation => !calculation.IsResult);
        var result = Definition.Calculations.First(calculation => calculation.IsResult);
        var read = FromPrintedTerms();
        var template = read?.Template
                       ?? (_variant is not null && _variant.Formulas.TryGetValue(assay.Key, out var replaced) ? replaced : assay.Formula);

        // Expanded first, so any input the formula needs is added ahead of the calculations.
        var samples = Math.Max(1, _sampleReadings.Count);
        var formulas = Enumerable.Range(1, samples).Select(sample => Expand(template, sample, null)).ToList();
        var keys = new List<string>();
        for (var sample = 1; sample <= samples; sample++)
        {
            var label = samples == 1 ? assay.Label : $"{assay.Label} ({Roman[Math.Min(sample, Roman.Length) - 1]})";
            var key = samples == 1 ? result.Key : $"{assay.Key}_{sample}";
            keys.Add(AddDefinitionCalculation(key, label, formulas[sample - 1], assay.Unit, samples == 1, _end, read?.Text,
                WorksheetImportFlagCodes.FormulaFromDefinition).FieldKey);
        }

        if (samples > 1)
            AddDefinitionCalculation(result.Key, PrintedLabelFor(result), FormulaLibrary.Average(keys.ToArray()), result.Unit, true, _end, read?.Text,
                WorksheetImportFlagCodes.FormulaFromDefinition);
    }

    /// <summary>"01) … 20)": one fixed row per weighing, with their total and average.</summary>
    private void ApplyWeighings()
    {
        var table = Definition.Table;
        var added = _shellWeights == 0;
        var count = added ? table.Rows.Count : _shellWeights;
        var location = _shellWeightsLocation ?? _end;
        var column = table.Columns[0];

        var tableKey = builder.AddTable($"{prefix}_{table.Key}", table.Label,
        [
            new GridColumn
            {
                Key = "shell", Label = table.RowHeader, Type = WorksheetFieldType.ShortText, RowHeader = true, Reason = "Printed numbering",
                FixedValues = Enumerable.Range(1, count).Select(number => number.ToString("00")).ToList()
            },
            new GridColumn
            {
                Key = column.Key, Label = column.Label, Type = column.Type, Unit = column.Unit, Confidence = ImportConfidence.Medium,
                Reason = "Unit not printed; mg as in the Specification (reviewer confirms)"
            }
        ], location, "Numbered weighing blanks").FieldKey;
        _shellWeights = 0;

        if (added)
            builder.Flag(WorksheetImportFlagCodes.DefinitionInputAdded,
                $"{sectionName}: the {count} individual weights are required by the '{Definition.Title}' definition but are not printed on the sheet; they were added.", location);

        foreach (var calculation in Definition.Calculations)
            AddDefinitionCalculation(calculation.Key, PrintedLabelFor(calculation), $"{calculation.Formula}({{{tableKey}.{column.Key}}})", calculation.Unit,
                calculation.IsResult, _end, null, WorksheetImportFlagCodes.FormulaFromDefinition);
    }

    /// <summary>Any nested table the definition does not use is kept as a table and flagged, never dropped.</summary>
    private void AddOtherTables(List<(DocxTable Table, ImportSourceLocation Location)> tables, DocxTable used)
    {
        foreach (var (table, location) in tables.Where(item => !ReferenceEquals(item.Table, used)))
        {
            ReadTable(table, location);
            FlagExtraLine($"table '{Truncate(DocxDocumentReader.Describe(table))}'", location);
        }
    }

    private static string Truncate(string text) => text.Length > 60 ? text[..60] + "…" : text;
}
