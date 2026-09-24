using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>Field keys every imported media template carries, so product and water sheets can point at them.</summary>
public static class CultureMediaKeys
{
    /// <summary>The dehydrated medium as a Reagent field; its batch number is the resolution key.</summary>
    public const string Medium = "medium";

    /// <summary>The overall "Complies / Does not comply" remark: what a referencing sheet reads.</summary>
    public const string Remark = "remark";
}

/// <summary>
/// Product and EM sheets do not re-test their media: they cite the medium's qualification by
/// batch ("Medium Batch no" + "Remark", or "Refer to Culture Media Batch Data sheet serial
/// numbered …"). Each cited medium becomes a Reagent field (the resolution key — at run time
/// its batch number is matched against the media sheet's own <see cref="CultureMediaKeys.Medium"/>
/// batch) plus a ReferencedResult reading the media template's <see cref="CultureMediaKeys.Remark"/>.
/// </summary>
public static partial class MediaReference
{
    [GeneratedRegex(@"Refer\s+to\s+Culture\s+Media\s+Batch\s+Data\s+sheet", RegexOptions.IgnoreCase)]
    private static partial Regex ReferToSheetRegex();

    public static bool IsReferToSheet(string text) => ReferToSheetRegex().IsMatch(text ?? string.Empty);

    /// <summary>One cited medium: its printed name and the cell holding its batch number.</summary>
    public sealed record CitedMedium(string Name, int Row, int Column);

    public static bool IsMediaReferenceTable(DocxTable table) => Media(table).Count > 0;

    /// <summary>
    /// The media a reference table cites, in either printed layout:
    /// <list type="bullet">
    /// <item>one column per medium — a name row, then a "Medium Batch no:" row (most sheets);</item>
    /// <item>one row per medium — a "Medium Name | Medium Batch No | Remark" header row (Entrima).</item>
    /// </list>
    /// A spanning title row above either layout is ignored.
    /// </summary>
    public static IReadOnlyList<CitedMedium> Media(DocxTable table)
    {
        var header = Enumerable.Range(0, table.Rows.Count).FirstOrDefault(row =>
        {
            var cells = table.RowTexts(row).Select(ImportText.Canonical).ToList();
            return cells.Contains("mediumname") && cells.Any(cell => cell.StartsWith("mediumbatchno"));
        }, -1);

        if (header >= 0)
        {
            var texts = table.RowTexts(header).Select(ImportText.Canonical).ToList();
            var nameColumn = texts.IndexOf("mediumname");
            var batchColumn = texts.FindIndex(cell => cell.StartsWith("mediumbatchno"));
            return Enumerable.Range(header + 1, table.Rows.Count - header - 1)
                .Select(row => new CitedMedium(table.Resolved(row, nameColumn), row, batchColumn))
                .Where(item => !ImportText.IsBlank(item.Name))
                .ToList();
        }

        var batchRow = Enumerable.Range(0, table.Rows.Count)
            .FirstOrDefault(row => ImportText.Canonical(table.Resolved(row, 0)).StartsWith("mediumbatchno"), -1);
        if (batchRow < 1)
            return [];

        // "Medium Name | …" row when there is one, else the row above the batch row.
        var nameRow = Enumerable.Range(0, batchRow)
            .FirstOrDefault(row => ImportText.Canonical(table.Resolved(row, 0)) == "mediumname", batchRow - 1);

        return Enumerable.Range(1, table.ColumnCount - 1)
            .Where(column => table.Cell(nameRow, column) is { IsHorizontalSpan: false })
            .Select(column => new CitedMedium(table.Resolved(nameRow, column), batchRow, column))
            .Where(item => !ImportText.IsBlank(item.Name))
            .ToList();
    }

    /// <param name="mediumCodes">Printed medium codes by normalized name (a product sheet's reagent list), used to match the media template by code first.</param>
    public static void Apply(DocxBlock block, ImportProposalBuilder builder, IReadOnlyDictionary<string, string> mediumCodes = null)
    {
        foreach (var medium in Media(block.Table))
            AddMedium(builder, medium.Name, ImportProposalBuilder.At(block, medium.Row, medium.Column), mediumCodes);
    }

    /// <summary>
    /// One cited medium: a Reagent batch field (the resolution key) and a ReferencedResult on the
    /// media template's remark. Also used for the paragraph form of a citation (water sheets:
    /// caption, "Medium Batch no.:", then a serial number that is run data and not kept).
    /// </summary>
    public static void AddMedium(
        ImportProposalBuilder builder, string name, ImportSourceLocation location, IReadOnlyDictionary<string, string> mediumCodes = null)
    {
        builder.Section("Culture media");
        var slug = ImportText.SnakeKey(name, 40);
        var batchField = ReagentTable.AddReagent(builder, name, $"medium_batch_{slug}", $"{name} — medium batch no.", location);

        var code = mediumCodes?.GetValueOrDefault(ImportText.Canonical(name));
        var template = builder.Catalog.FindMediaTemplate(name, code);
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = $"media_qualification_{slug}",
            Label = $"{name} — media qualification remark",
            Type = WorksheetFieldType.ReferencedResult,
            Mode = WorksheetFieldMode.Entry,
            ReferencedResultSourceTemplateId = template?.Id,
            ReferencedResultSourceFieldKey = CultureMediaKeys.Remark,
            ReferencedResultResolutionFieldKey = batchField.FieldKey
        }, location, template is null ? ImportConfidence.Low : ImportConfidence.High,
            template is null
                ? "Referenced media qualification; no MediaQualification template for this medium yet"
                : $"Referenced media qualification from template {template.Code}, resolved by batch number");

        if (template is null)
            builder.Flag(WorksheetImportFlagCodes.MediaTemplateMissing,
                $"No MediaQualification template exists for '{name}'{(code is null ? string.Empty : $" ({code})")}. "
                + "Import the media sheets first, or repoint this field.", location);
    }
}
