using System.Globalization;
using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Specifications printed in the sheet ("Specification: NMT 2000 cfu/g", "pH Range: 7.2 ± 0.2",
/// AREA | SPECIFICATION tables) become Specification-characteristic proposals for the
/// Specifications phase — never template content.
/// </summary>
public static partial class PrintedSpecification
{
    [GeneratedRegex(@"^\s*Specifications?\s*:\s*(?<criteria>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex StatementRegex();

    [GeneratedRegex(@"(?<centre>\d+(?:\.\d+)?)\s*±\s*(?<delta>\d+(?:\.\d+)?)")]
    private static partial Regex PlusMinusRegex();

    [GeneratedRegex(@"(?<low>\d+(?:\.\d+)?)\s*[-–—]\s*(?<high>\d+(?:\.\d+)?)")]
    private static partial Regex RangeRegex();

    [GeneratedRegex(@"\b(NMT|NLT|not\s+more\s+than|not\s+less\s+than|absence\s+of|absent)\b", RegexOptions.IgnoreCase)]
    private static partial Regex LimitRegex();

    /// <summary>"Specification: …" → the criteria text.</summary>
    public static bool TryParseStatement(string text, out string criteria)
    {
        var match = StatementRegex().Match(text ?? string.Empty);
        criteria = match.Success ? ImportText.Normalize(match.Groups["criteria"].Value) : null;
        return match.Success && criteria.Length > 0;
    }

    public static bool IsLimit(string text) => LimitRegex().IsMatch(text ?? string.Empty);

    /// <summary>
    /// A numeric range as printed, with "centre ± delta" also spelled out as low – high:
    /// "7.2 ± 0.2" → "7.2 ± 0.2 (7.0 – 7.4)"; "5.00 – 5.40" stays as printed.
    /// </summary>
    public static bool TryParseRange(string text, out string criteria)
    {
        criteria = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var plusMinus = PlusMinusRegex().Match(text);
        if (plusMinus.Success)
        {
            var centre = decimal.Parse(plusMinus.Groups["centre"].Value, CultureInfo.InvariantCulture);
            var delta = decimal.Parse(plusMinus.Groups["delta"].Value, CultureInfo.InvariantCulture);
            var decimals = Math.Max(Decimals(plusMinus.Groups["centre"].Value), Decimals(plusMinus.Groups["delta"].Value));
            var format = "F" + decimals;
            criteria = $"{plusMinus.Value} ({(centre - delta).ToString(format, CultureInfo.InvariantCulture)} – "
                       + $"{(centre + delta).ToString(format, CultureInfo.InvariantCulture)})";
            return true;
        }

        var range = RangeRegex().Match(text);
        if (!range.Success)
            return false;

        criteria = $"{range.Groups["low"].Value} – {range.Groups["high"].Value}";
        return true;
    }

    public static SpecificationCharacteristicProposal Proposal(
        string testName, string criteria, string sourceFieldKey, ImportSourceLocation location,
        ImportConfidence confidence, string groupName = null, string analyte = null) =>
        new()
        {
            TestName = testName?.Length > 200 ? testName[..200] : testName,
            Analyte = analyte,
            AcceptanceCriteria = criteria,
            SourceFieldKey = sourceFieldKey,
            GroupName = groupName,
            Confidence = confidence,
            Location = location
        };

    public static bool IsAreaSpecificationTable(DocxTable table) =>
        table.ColumnCount == 2 && table.Rows.Count > 1
        && ImportText.Canonical(table.Resolved(0, 0)) == "area"
        && ImportText.Canonical(table.Resolved(0, 1)).StartsWith("specification");

    /// <summary>AREA | SPECIFICATION rows → one proposal per area, the area as the group.</summary>
    public static IReadOnlyList<SpecificationCharacteristicProposal> FromAreaSpecificationTable(
        DocxBlock block, string testName, string sourceFieldKey) =>
        Enumerable.Range(1, block.Table.Rows.Count - 1)
            .Select(row => (row, area: block.Table.Resolved(row, 0), criteria: block.Table.Resolved(row, 1)))
            .Where(item => !ImportText.IsBlank(item.area) && !ImportText.IsBlank(item.criteria))
            .Select(item => Proposal(testName, item.criteria, sourceFieldKey, ImportProposalBuilder.At(block, item.row),
                IsLimit(item.criteria) ? ImportConfidence.High : ImportConfidence.Medium, groupName: item.area))
            .ToList();

    private static int Decimals(string number)
    {
        var dot = number.IndexOf('.');
        return dot < 0 ? 0 : number.Length - dot - 1;
    }
}

/// <summary>"Analysed By / Checked By / Date" (or DONE BY / CHECKED BY): dropped — review and approval cover it.</summary>
public static class SignOffTable
{
    public static bool IsSignOff(string text)
    {
        var canonical = ImportText.Canonical(text);
        return canonical.Contains("checkedby")
               && (canonical.Contains("analysedby") || canonical.Contains("analyzedby")
                   || canonical.Contains("doneby") || canonical.Contains("testedby"));
    }

    public static bool Is(DocxTable table) =>
        IsSignOff(string.Join(" ", Enumerable.Range(0, Math.Min(2, table.Rows.Count)).SelectMany(table.RowTexts)));
}

/// <summary>
/// Room/point grids (code + name columns) → SamplingPoint proposals. EM and water templates
/// hold one subject's fields only; the printed list never becomes template rows.
/// </summary>
public static class SamplingPointList
{
    public static bool Is(DocxTable table)
    {
        var grid = DataGrid.Read(table);
        return CodeColumn(grid) is not null && NameColumn(grid) is not null && grid.DataRows.Count > 0;
    }

    public static IReadOnlyList<SamplingPointProposal> Read(DocxBlock block, string area, SamplingPointType type)
    {
        var grid = DataGrid.Read(block.Table);
        var code = CodeColumn(grid);
        var name = NameColumn(grid);
        if (code is null || name is null)
            return [];

        return grid.DataRows
            .Select(row => new SamplingPointProposal
            {
                Code = block.Table.Resolved(row, code.SourceColumn),
                Name = block.Table.Resolved(row, name.SourceColumn),
                Area = area,
                Type = type,
                Location = ImportProposalBuilder.At(block, row)
            })
            .Where(point => !ImportText.IsBlank(point.Code))
            .ToList();
    }

    private static GridColumn CodeColumn(DataGridResult grid) =>
        grid.Columns.FirstOrDefault(column =>
        {
            var canonical = ImportText.Canonical(column.Label);
            return canonical.StartsWith("roomno") || canonical.Contains("idno") || canonical.Contains("pointno")
                   || canonical.Contains("pointcode") || canonical is "code" or "samplingpoint" or "spno";
        });

    private static GridColumn NameColumn(DataGridResult grid) =>
        grid.Columns.FirstOrDefault(column =>
        {
            var canonical = ImportText.Canonical(column.Label);
            return canonical.EndsWith("name") || canonical.Contains("location") || canonical is "description";
        });
}
