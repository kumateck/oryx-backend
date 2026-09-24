using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public sealed record EquipmentRow(int Row, string Name, string Code);

/// <summary>
/// "EQUIPMENT USED | EQUIPMENT CODE" → one Instrument field per row. An Instrument field is
/// an Entry field whose value is a <c>QcEquipment</c> id picked at run time (that is what the
/// calibration gate checks), so the matched equipment is returned as a suggestion in
/// <see cref="WorksheetImportProposal.EquipmentMatches"/>, never baked into the template.
/// </summary>
public static class EquipmentTable
{
    public static bool Is(DocxTable table)
    {
        if (table.ColumnCount < 2 || table.Rows.Count == 0)
            return false;
        var header = table.RowTexts(0).Select(ImportText.Canonical).ToList();
        return header.Any(text => text.Contains("equipment") || text.Contains("instrument"))
               && header.Any(text => text.Contains("code") || text.EndsWith("id") || text.EndsWith("no"));
    }

    public static IReadOnlyList<EquipmentRow> Read(DocxTable table)
    {
        var header = table.RowTexts(0).Select(ImportText.Canonical).ToList();
        var codeColumn = header.FindIndex(text => text.Contains("code") || text.EndsWith("id") || text.EndsWith("no"));
        var nameColumn = header.FindIndex(text => (text.Contains("equipment") || text.Contains("instrument")) && !text.Contains("code"));
        if (nameColumn < 0)
            nameColumn = codeColumn == 0 ? 1 : 0;

        var end = ReagentHeaderRow(table) ?? table.Rows.Count;
        return Enumerable.Range(1, end - 1)
            .Select(row => new EquipmentRow(row, table.Resolved(row, nameColumn), table.Resolved(row, codeColumn)))
            .Where(item => !ImportText.IsBlank(item.Name) || !ImportText.IsBlank(item.Code))
            .ToList();
    }

    /// <summary>
    /// Product sheets continue the equipment table with "REAGENTS USED | REAGENT CODE" rows.
    /// Those are reagents (the media, with their medium codes), not equipment.
    /// </summary>
    public static IReadOnlyList<EquipmentRow> ReagentRows(DocxTable table)
    {
        if (ReagentHeaderRow(table) is not { } header)
            return [];
        return Enumerable.Range(header + 1, table.Rows.Count - header - 1)
            .Select(row => new EquipmentRow(row, table.Resolved(row, 0), table.Resolved(row, 1)))
            .Where(item => !ImportText.IsBlank(item.Name))
            .ToList();
    }

    private static int? ReagentHeaderRow(DocxTable table) =>
        Enumerable.Range(1, Math.Max(0, table.Rows.Count - 1))
            .Where(row => ImportText.Canonical(table.Resolved(row, 0)).Contains("reagent"))
            .Select(row => (int?)row)
            .FirstOrDefault();

    /// <param name="capturedReagents">Normalized names already captured as Reagent fields (a product
    /// sheet's cited media), which the reagent list would otherwise repeat.</param>
    public static void Apply(DocxBlock block, ImportProposalBuilder builder, IReadOnlySet<string> capturedReagents = null)
    {
        builder.Section("Equipment");
        foreach (var item in Read(block.Table))
        {
            var location = ImportProposalBuilder.At(block, item.Row);
            var name = ImportText.IsBlank(item.Name) ? item.Code : item.Name;
            var match = builder.Catalog.FindEquipment(item.Code);

            var field = builder.AddField(new ProposedWorksheetField
            {
                FieldKey = "instrument_" + ImportText.SnakeKey(name, 40),
                Label = ImportText.IsBlank(item.Code) ? name : $"{name} ({item.Code})",
                Type = WorksheetFieldType.Instrument,
                Mode = WorksheetFieldMode.Entry
            }, location, match is null ? ImportConfidence.Medium : ImportConfidence.High,
                match is null ? "Equipment row; no equipment with this code in the register" : $"Equipment row; matched {match.Code}");

            builder.Proposal.EquipmentMatches.Add(new ImportEquipmentMatch
            {
                FieldKey = field.FieldKey, PrintedName = item.Name, PrintedCode = item.Code,
                EquipmentId = match?.Id, MatchedName = match?.Name
            });

            if (match is null)
                builder.Flag(WorksheetImportFlagCodes.UnmatchedEquipment,
                    $"'{name}' ({item.Code}) is not in the QC equipment register.", location);
        }

        var reagents = ReagentRows(block.Table)
            .Where(item => capturedReagents?.Contains(ImportText.Canonical(item.Name)) != true)
            .ToList();
        if (reagents.Count == 0)
            return;

        builder.Section("Reagents");
        foreach (var item in reagents)
            ReagentTable.AddReagent(builder, item.Name, "reagent_" + ImportText.SnakeKey(item.Name, 40),
                ImportText.IsBlank(item.Code) ? item.Name : $"{item.Name} ({item.Code})", ImportProposalBuilder.At(block, item.Row));
    }
}

/// <summary>
/// A reagent list → one Reagent field per row (the run-time entry carries reagent id, batch
/// and expiry). Reagents only have a Name, so they are matched by normalized name.
/// </summary>
public static class ReagentTable
{
    public static bool Is(DocxTable table) =>
        table.Rows.Count > 1 && table.RowTexts(0).Select(ImportText.Canonical)
            .Any(text => text.Contains("reagent") || text.Contains("chemical"));

    public static void Apply(DocxBlock block, ImportProposalBuilder builder)
    {
        var table = block.Table;
        var nameColumn = table.RowTexts(0).Select(ImportText.Canonical).ToList()
            .FindIndex(text => text.Contains("reagent") || text.Contains("chemical"));

        builder.Section("Reagents");
        for (var row = 1; row < table.Rows.Count; row++)
        {
            var name = table.Resolved(row, nameColumn);
            if (!ImportText.IsBlank(name))
                AddReagent(builder, name, "reagent_" + ImportText.SnakeKey(name, 40), name, ImportProposalBuilder.At(block, row));
        }
    }

    /// <summary>Adds one Reagent field and records its catalog match (or flags it).</summary>
    public static ProposedWorksheetField AddReagent(
        ImportProposalBuilder builder, string printedName, string key, string label, ImportSourceLocation location)
    {
        var match = builder.Catalog.FindReagent(printedName);
        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = key, Label = label, Type = WorksheetFieldType.Reagent, Mode = WorksheetFieldMode.Entry
        }, location, match is null ? ImportConfidence.Medium : ImportConfidence.High,
            match is null ? $"Reagent '{printedName}'; not found in the reagent catalog" : $"Reagent; matched '{match.Name}'");

        builder.Proposal.ReagentMatches.Add(new ImportReagentMatch
        {
            FieldKey = field.FieldKey, PrintedName = printedName, ReagentId = match?.Id, MatchedName = match?.Name
        });

        if (match is null)
            builder.Flag(WorksheetImportFlagCodes.UnmatchedReagent, $"'{printedName}' is not in the reagent catalog.", location);

        return field;
    }
}
