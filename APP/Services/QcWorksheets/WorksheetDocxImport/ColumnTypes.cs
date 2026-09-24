using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>Field type implied by a table column header, from the vocabulary of the corpus.</summary>
public static class ColumnTypes
{
    public static (WorksheetFieldType Type, string Unit) Infer(string label, string unit)
    {
        var canonical = ImportText.Canonical(label);
        var hasCfu = unit?.Contains("CFU", StringComparison.OrdinalIgnoreCase) == true;

        if (canonical.Contains("code") || canonical is "roomno" or "idno" or "roomnoidno")
            return (WorksheetFieldType.ShortText, null);
        if (canonical.Contains("strain") || canonical.Contains("organism") || canonical.Contains("microorganism"))
            return (WorksheetFieldType.Organism, null);
        if (canonical.StartsWith("inoculum"))
            return (WorksheetFieldType.Number, null);
        if (canonical.StartsWith("incubationtemp") || canonical is "temp" or "temperature")
            return (WorksheetFieldType.IncubationTemperature, "°C");
        if (canonical.StartsWith("incubationperiod") || canonical is "period")
            return (WorksheetFieldType.IncubationPeriod, null);
        if (canonical.StartsWith("plate"))
            return (WorksheetFieldType.ColonyCount, hasCfu ? null : "CFU");
        if (PlateAverage.IsAverage(label))
            return (WorksheetFieldType.CalculatedValue, hasCfu ? null : "CFU");
        if (canonical.Contains("dilution"))
            return (WorksheetFieldType.Dilution, null);
        if (canonical.Contains("zone") && canonical.Contains("observed"))
            return (WorksheetFieldType.Measurement, "mm");
        if (hasCfu)
            return (WorksheetFieldType.ColonyCount, null);

        return (WorksheetFieldType.ShortText, null);
    }
}

/// <summary>
/// Plate 1 / Plate 2 / Av. columns become two Entry colony counts and a Calculated average.
/// Also the formula library: the calculations the corpus implies but almost never writes out
/// (only 3 textual formulas exist, one incomplete). A reviewer confirms each one.
/// </summary>
public static class PlateAverage
{
    public static bool IsAverage(string label) =>
        ImportText.Canonical(label) is "av" or "avg" or "average" or "mean" or "averagecount" or "averagecfu";

    /// <summary>
    /// Within each group, turns the average column into a Calculated column over that group's
    /// plate columns. Returns the average columns it made calculated.
    /// </summary>
    public static IReadOnlyList<GridColumn> Apply(IEnumerable<GridColumn> columns)
    {
        var calculated = new List<GridColumn>();

        foreach (var group in columns.GroupBy(column => column.Group ?? string.Empty))
        {
            var plates = group.Where(column => ImportText.Canonical(column.Label).StartsWith("plate")).ToList();
            var average = group.Where(column => IsAverage(column.Label)).ToList();

            if (average.Count != 1)
                continue;

            if (plates.Count < 2)
            {
                average[0].Type = WorksheetFieldType.Number;
                average[0].Confidence = ImportConfidence.Low;
                average[0].Reason = "Average column without two plate columns to average; proposed as Entry";
                continue;
            }

            foreach (var plate in plates)
                plate.Type = WorksheetFieldType.ColonyCount;

            average[0].Type = WorksheetFieldType.CalculatedValue;
            average[0].Mode = WorksheetFieldMode.Calculated;
            average[0].Formula = FormulaLibrary.RowAverage(plates.Select(plate => plate.Key).ToList());
            average[0].Confidence = ImportConfidence.Medium;
            average[0].Reason = "Formula library: average of the plate counts in the same row (reviewer confirms)";
            calculated.Add(average[0]);
        }

        return calculated;
    }
}

/// <summary>Formulas in <see cref="QcFormulaEvaluator"/> syntax.</summary>
public static class FormulaLibrary
{
    /// <summary>Per-row mean of sibling columns: <c>({plate1} + {plate2}) / 2</c>.</summary>
    public static string RowAverage(IReadOnlyList<string> columnKeys) =>
        $"({string.Join(" + ", columnKeys.Select(key => $"{{{key}}}"))}) / {columnKeys.Count}";

    /// <summary>Mean of a scalar pair: <c>({plate_1} + {plate_2}) / 2</c>.</summary>
    public static string Average(params string[] fieldKeys) => RowAverage(fieldKeys);

    /// <summary>cfu = average × dilution factor.</summary>
    public static string CfuFromAverage(string averageKey, string dilutionFactorKey) =>
        $"{{{averageKey}}} * {{{dilutionFactorKey}}}";

    /// <summary>Aggregate over a table column: <c>AVG({table.column})</c>.</summary>
    public static string ColumnAverage(string tableKey, string columnKey) => $"AVG({{{tableKey}.{columnKey}}})";
}
