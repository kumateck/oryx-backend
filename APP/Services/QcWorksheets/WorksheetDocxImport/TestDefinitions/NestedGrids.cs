namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>
/// The titration grid nested in an assay cell: Blank / Sample 1 / Sample 2 across, and
/// Wt. taken / Final volume / Initial volume / Titre obtained down.
/// </summary>
public sealed class TitrationGrid
{
    /// <summary>The determinations across the top, in order ("Blank", "Sample 1", "Sample 2").</summary>
    public IReadOnlyList<string> Determinations { get; private init; }

    /// <summary>The quantities down the side, in order.</summary>
    public IReadOnlyList<string> Quantities { get; private init; }

    public static bool IsTitration(DocxTable table)
    {
        var labels = Enumerable.Range(0, table.Rows.Count).Select(index => ImportText.Canonical(table.Resolved(index, 0))).ToList();
        return labels.Any(label => label.StartsWith("titre"))
               || (labels.Any(label => label.StartsWith("final")) && labels.Any(label => label.StartsWith("initial")));
    }

    public static TitrationGrid Read(DocxTable table) => !IsTitration(table)
        ? null
        : new TitrationGrid
        {
            Determinations = table.OriginCells(0).Skip(1).Select(cell => cell.Text).Where(text => !ImportText.IsBlank(text)).ToList(),
            Quantities = Enumerable.Range(1, table.Rows.Count - 1).Select(row => table.Resolved(row, 0).TrimEnd('.', ' '))
                .Where(label => !ImportText.IsBlank(label)).ToList()
        };

    public static bool IsBlank(string determination) => ImportText.Canonical(determination).StartsWith("blank");
    public static bool IsWeight(string quantity) => ImportText.Canonical(quantity) is var text && (text.StartsWith("wt") || text.StartsWith("weight"));
    public static bool IsTitre(string quantity) => ImportText.Canonical(quantity).StartsWith("titre");
    public static bool IsFinal(string quantity) => ImportText.Canonical(quantity).StartsWith("final");
    public static bool IsInitial(string quantity) => ImportText.Canonical(quantity).StartsWith("initial");
}

/// <summary>
/// A grid of replicate readings nested in an assay cell, in any of the three layouts the sheets use:
/// injections down and Standard / Sample 1 / Sample 2 across (peak areas); solutions down and two
/// absorbance columns across ("Solution | Absorbance | Mean Abs"); or "Abs 1 / Abs 2 / Mean Abs" down
/// and the samples across. All three read as: one column per solution, one row per reading.
/// </summary>
public sealed class ReadingsGrid
{
    /// <summary>The solutions read, in order ("Standard", "Sample 1", "Sample 2").</summary>
    public IReadOnlyList<string> Solutions { get; private init; }

    /// <summary>The reading labels, in order ("1" … "5").</summary>
    public IReadOnlyList<string> Readings { get; private init; }

    public string RowHeader { get; private init; }
    public bool HasAverage { get; private init; }
    public bool HasSd { get; private init; }
    public bool HasRsd { get; private init; }

    /// <summary>True for an absorbance layout, false for peak areas.</summary>
    public bool IsAbsorbance { get; private init; }

    /// <summary>True when the rows are headed "Injection".</summary>
    public bool IsInjections { get; private init; }

    public static bool IsStandard(string solution) => ImportText.Canonical(solution) is var text && (text.StartsWith("std") || text.StartsWith("standard"));

    public static bool IsSample(string solution) => ImportText.Canonical(solution) is var text && (text.StartsWith("sample") || text.StartsWith("spl"));

    private static bool IsSolution(string text) => IsStandard(text) || IsSample(text);

    private static bool IsSummary(string canonical) =>
        canonical.StartsWith("average") || canonical.StartsWith("mean") || canonical.StartsWith("avg") || canonical is "sd" or "rsd" or "rsd%" || canonical.StartsWith("rsd");

    /// <summary>The definition's own table, for a sheet that prints none.</summary>
    public static ReadingsGrid Default(string rowHeader, IReadOnlyList<string> readings, IReadOnlyList<string> solutions) =>
        new() { RowHeader = rowHeader, Readings = readings, Solutions = solutions, HasAverage = true };

    public static ReadingsGrid Read(DocxTable table)
    {
        if (TitrationGrid.IsTitration(table))
            return null;

        var firstColumn = Enumerable.Range(0, table.Rows.Count).Select(row => table.Resolved(row, 0)).ToList();
        var canonical = firstColumn.Select(ImportText.Canonical).ToList();

        // Solutions across: the header row names a standard or sample in a column after the first.
        var header = Enumerable.Range(0, table.Rows.Count)
            .FirstOrDefault(row => table.OriginCells(row).Skip(1).Any(cell => IsSolution(cell.Text)), -1);
        if (header >= 0)
        {
            var solutions = table.OriginCells(header).Where(cell => cell.Column > 0 && IsSolution(cell.Text)).Select(cell => cell.Text).ToList();
            var rows = Enumerable.Range(header + 1, table.Rows.Count - header - 1).ToList();
            var readings = rows.Where(row => canonical[row].Length > 0 && !IsSummary(canonical[row])).Select(row => firstColumn[row].Trim()).ToList();
            var all = string.Join(" ", canonical);
            return readings.Count == 0 ? null : new ReadingsGrid
            {
                Solutions = solutions, Readings = readings,
                RowHeader = ImportText.IsBlank(firstColumn[header]) ? "Reading" : firstColumn[header].Trim(),
                HasAverage = rows.Any(row => canonical[row].StartsWith("average") || canonical[row].StartsWith("mean") || canonical[row].StartsWith("avg")),
                HasSd = rows.Any(row => canonical[row] == "sd"),
                HasRsd = rows.Any(row => canonical[row].StartsWith("rsd")),
                IsAbsorbance = all.Contains("abs") && !all.Contains("peak"),
                IsInjections = canonical[header].StartsWith("injection")
            };
        }

        // Solutions down: "Solution | Absorbance | Mean Abs" over Std / Sample 1 / Sample 2.
        var headerTexts = table.OriginCells(0).Select(cell => ImportText.Canonical(cell.Text)).ToList();
        if (!headerTexts.Any(text => text.StartsWith("absorbance")) && !headerTexts.Any(text => text.StartsWith("abs")))
            return null;

        var down = Enumerable.Range(1, table.Rows.Count - 1).Where(row => IsSolution(firstColumn[row])).Select(row => firstColumn[row].Trim()).ToList();
        if (down.Count == 0)
            return null;

        var mean = table.OriginCells(0).FirstOrDefault(cell => cell.Column > 0 && IsSummary(ImportText.Canonical(cell.Text)));
        var count = Math.Max(1, (mean?.Column ?? table.ColumnCount) - 1);
        return new ReadingsGrid
        {
            Solutions = down, Readings = Enumerable.Range(1, count).Select(number => number.ToString()).ToList(),
            RowHeader = "Reading", HasAverage = mean is not null, IsAbsorbance = true
        };
    }
}
