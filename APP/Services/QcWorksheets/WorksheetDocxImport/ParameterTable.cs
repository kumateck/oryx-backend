using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public enum ParameterKind
{
    Constant,
    Entry,

    /// <summary>Run/issuance data owned by the system: no field at all.</summary>
    HeaderData
}

public sealed record ParameterDecision(
    ParameterKind Kind,
    string Label,
    string Key,
    WorksheetFieldType Type,
    string ConstantValue,
    string Unit,
    ImportConfidence Confidence,
    string Reason,
    string FlagCode = null);

/// <summary>
/// Label | value pairs — a two-column table row, a "Label: value" cell, or a "Label: value"
/// paragraph. A printed value becomes a Constant unless the label is run data or the value
/// looks like run data; a blank (empty or a leader) becomes an Entry field.
/// </summary>
public static partial class ParameterTable
{
    [GeneratedRegex(@"\bpH\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DanglingPhRegex();

    // Units only ("(mL)", "(CFU/0.1mL)"); "(After 5 days of incubation)" is a qualifier, not a unit.
    [GeneratedRegex(@"\(([^()\s]{1,15})\)\s*$")]
    private static partial Regex TrailingUnitRegex();

    public static ParameterDecision Decide(string label, string value)
    {
        label = ImportText.Normalize(label).TrimEnd(':', ' ');
        value = ImportText.Normalize(value);

        if (RunDataLabels.TryMatch(label, out var runData))
        {
            return runData.Disposition == RunDataDisposition.HeaderData
                ? new ParameterDecision(ParameterKind.HeaderData, runData.Label, runData.Key, runData.Type, null, null,
                    ImportConfidence.High, "Issuance data owned by the system; printed value discarded")
                : new ParameterDecision(ParameterKind.Entry, label, runData.Key, runData.Type, null, null,
                    ImportConfidence.High, "Run-data label; printed value discarded");
        }

        var key = ImportText.SnakeKey(label);
        var (type, unit) = InferType(label);

        if (ImportText.HasLeader(value) || ImportText.IsBlank(value))
        {
            var remainder = ImportText.StripLeaders(value).TrimEnd('.', ',', ' ');
            if (ImportText.IsNoise(remainder))
                return new ParameterDecision(ParameterKind.Entry, label, key, type, null, unit, ImportConfidence.High,
                    "Blank to fill in");

            // "…… passages": the text after the blank is its unit.
            var after = value.TrimEnd().EndsWith(remainder, StringComparison.Ordinal);
            if (after && remainder.Length <= 15 && remainder.Split(' ').Length <= 2)
                return new ParameterDecision(ParameterKind.Entry, label, key, WorksheetFieldType.Number, null, remainder,
                    ImportConfidence.Medium, $"Blank followed by '{remainder}', taken as the unit");

            return new ParameterDecision(ParameterKind.Entry, $"{label} ({remainder})", key, type, null, unit,
                ImportConfidence.Low, $"Printed text '{remainder}' sits beside the blank; kept in the label",
                WorksheetImportFlagCodes.IncompleteValue);
        }

        if (RunDataLabels.LooksLikeRunData(value))
            return new ParameterDecision(ParameterKind.Entry, label, key, type, null, unit, ImportConfidence.Low,
                "Printed value looks like run data (a date, batch or issue number); discarded",
                WorksheetImportFlagCodes.SuspectedRunData);

        if (DanglingPhRegex().IsMatch(value))
            return new ParameterDecision(ParameterKind.Constant, label, key, WorksheetFieldType.ShortText, value, null,
                ImportConfidence.Low, "Value ends in 'pH' with no number — the printed pH may be a missing blank",
                WorksheetImportFlagCodes.IncompleteValue);

        return new ParameterDecision(ParameterKind.Constant, label, key, WorksheetFieldType.ShortText, value, null,
            ImportConfidence.High, "Printed method parameter");
    }

    /// <summary>Splits a "Label: value" text and decides it; null when there is no label.</summary>
    public static ParameterDecision DecideText(string text) =>
        ImportText.TrySplitLabel(text, out var label, out var value) ? Decide(label, value) : null;

    /// <summary>A table whose rows are label | value pairs (exactly two columns, labels filled).</summary>
    public static bool IsTwoColumnParameterTable(DocxTable table) =>
        table.ColumnCount == 2
        && table.Rows.Count > 0
        && Enumerable.Range(0, table.Rows.Count).All(row => !ImportText.IsBlank(table.Resolved(row, 0)))
        && !table.RowTexts(0).All(text => text.Length > 0 && text == text.ToUpperInvariant());

    public static IReadOnlyList<(int Row, ParameterDecision Decision)> ReadTwoColumn(DocxTable table) =>
        Enumerable.Range(0, table.Rows.Count)
            .Where(row => table.Cell(row, 1) is { IsContinuation: false } || table.Resolved(row, 1).Length == 0)
            .Select(row => (row, Decide(table.Resolved(row, 0), table.Resolved(row, 1))))
            .ToList();

    /// <summary>
    /// Every "Label: value" segment in a metadata table whose cells each carry their own
    /// label(s), e.g. the media sheet's "Batch No.: … | Culture Medium Name: … | Medium Code: …".
    /// </summary>
    public static IReadOnlyList<(DocxCell Cell, ParameterDecision Decision)> ReadLabelledCells(DocxTable table)
    {
        var result = new List<(DocxCell, ParameterDecision)>();
        foreach (var cell in table.Rows.SelectMany(row => row).Where(cell => !cell.IsContinuation && cell.Text.Length > 0))
        {
            foreach (var (label, value) in Segments(cell.Text))
                result.Add((cell, Decide(label, value)));
        }

        return result;
    }

    public static IReadOnlyList<(string Label, string Value)> Segments(string text)
    {
        var starts = RunDataLabels.LabelStartRegex().Matches(text).ToList();
        if (starts.Count == 0 || starts[0].Index > 0)
        {
            return ImportText.TrySplitLabel(text, out var label, out var value)
                ? [(label, value)]
                : [];
        }

        return starts.Select((match, index) =>
        {
            var end = index + 1 < starts.Count ? starts[index + 1].Index : text.Length;
            var label = match.Value.TrimEnd(':', ' ');
            return (ImportText.Normalize(label), ImportText.Normalize(text[(match.Index + match.Length)..end]));
        }).ToList();
    }

    /// <summary>Field type (and unit) implied by a label alone.</summary>
    public static (WorksheetFieldType Type, string Unit) InferType(string label)
    {
        var unitMatch = TrailingUnitRegex().Match(label ?? string.Empty);
        var unit = unitMatch.Success ? unitMatch.Groups[1].Value.Trim() : null;
        var canonical = ImportText.Canonical(label);

        if (canonical.Contains("date"))
            return (WorksheetFieldType.Date, null);
        if (canonical.EndsWith("time"))
            return (WorksheetFieldType.Time, null);
        if (canonical is "ph" or "phobserved" || canonical.StartsWith("phof"))
            return (WorksheetFieldType.Number, null);
        if (canonical.Contains("quantity") || canonical.Contains("volume") || canonical.Contains("weigh")
            || canonical.StartsWith("noof") || canonical.StartsWith("numberof") || canonical.Contains("count"))
            return (WorksheetFieldType.Number, unit);

        return (WorksheetFieldType.ShortText, unit);
    }
}
