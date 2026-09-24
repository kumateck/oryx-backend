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

    public static bool IsMediaReferenceTable(DocxTable table) =>
        BatchRow(table) >= 0 && MediumNames(table).Count > 0;

    public static IReadOnlyList<(int Column, string Name)> MediumNames(DocxTable table)
    {
        var batchRow = BatchRow(table);
        if (batchRow < 0)
            return [];

        // "Medium Name | …" row when there is one, else the row above the batch row.
        var nameRow = Enumerable.Range(0, table.Rows.Count)
            .FirstOrDefault(row => ImportText.Canonical(table.Resolved(row, 0)) == "mediumname", batchRow - 1);
        if (nameRow < 0)
            return [];

        return Enumerable.Range(1, table.ColumnCount - 1)
            .Where(column => table.Cell(nameRow, column) is { IsHorizontalSpan: false })
            .Select(column => (column, table.Resolved(nameRow, column)))
            .Where(item => !ImportText.IsBlank(item.Item2))
            .ToList();
    }

    public static void Apply(DocxBlock block, ImportProposalBuilder builder)
    {
        var batchRow = BatchRow(block.Table);
        builder.Section("Culture media");

        foreach (var (column, name) in MediumNames(block.Table))
        {
            var location = ImportProposalBuilder.At(block, batchRow, column);
            var slug = ImportText.SnakeKey(name, 40);
            var batchField = ReagentTable.AddReagent(builder, name, $"medium_batch_{slug}", $"{name} — medium batch no.", location);

            var template = builder.Catalog.FindMediaTemplate(name);
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
                    $"No MediaQualification template exists for '{name}'. Import the media sheets first.", location);
        }
    }

    private static int BatchRow(DocxTable table) =>
        Enumerable.Range(0, table.Rows.Count)
            .FirstOrDefault(row => ImportText.Canonical(table.Resolved(row, 0)).StartsWith("mediumbatchno"), -1);
}
