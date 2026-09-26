using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class CultureMediaWalker
{
    private void ReadTable(DocxBlock block)
    {
        var table = block.Table;
        var caption = _caption;
        _caption = null;

        if (SignOffTable.Is(table))
            return;

        if (EquipmentTable.Is(table))
        {
            EquipmentTable.Apply(block, builder);
            return;
        }

        if (ReagentTable.Is(table))
        {
            ReagentTable.Apply(block, builder);
            return;
        }

        if (IsAntibioticTable(table))
        {
            ReadAntibioticTable(block, caption);
            return;
        }

        if (ParameterTable.IsTwoColumnParameterTable(table))
        {
            foreach (var (row, decision) in ParameterTable.ReadTwoColumn(table))
                builder.AddDecision(decision, ImportProposalBuilder.At(block, row));
            return;
        }

        ReadGrid(block, caption);
    }

    /// <summary>
    /// The strain grid and the cultural-response grid. Organism, strain code and any cell
    /// printed in every row (e.g. incubation period "18 hours") become fixed columns; the
    /// New / Previously approved batch groups become grouped Entry columns, with Av. calculated
    /// from the plates.
    /// </summary>
    private void ReadGrid(DocxBlock block, string caption)
    {
        var grid = DataGrid.Read(block.Table);
        PlateAverage.Apply(grid.Columns);
        var location = ImportProposalBuilder.At(block);

        var labels = grid.Columns.Select(column => ImportText.Canonical(column.Label)).ToList();
        var isStrainList = labels.Any(label => label.Contains("strain")) && grid.Columns.All(column => column.Group is null);
        var isCulturalResponse = labels.Any(label => label.Contains("strain")) && grid.Columns.Any(column => column.Group is not null);

        var (key, label) = isCulturalResponse
            ? ("cultural_response", "Cultural response")
            : isStrainList
                ? ("test_strains", "Test strains")
                : (ImportText.SnakeKey(caption ?? builder.CurrentSection?.Name ?? "table", 40), caption ?? builder.CurrentSection?.Name ?? "Table");

        if (!grid.HasFixedRows && grid.DataRows.Count > 0)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                $"'{label}' has {grid.DataRows.Count} printed rows but no column filled in every row; proposed as an open-ended table.",
                location);

        // The Av. columns are computed per row at submit ("mode": "Calculated" with a formula).
        builder.AddTable(key, label, grid.Columns, location,
            grid.HasFixedRows ? $"Grid with {grid.DataRows.Count} fixed rows" : "Grid with open-ended rows");
    }

    private static bool IsAntibioticTable(DocxTable table) =>
        table.ColumnCount >= 3
        && ImportText.Canonical(table.Resolved(0, 0)).StartsWith("antimicrobial")
        && ImportText.Canonical(table.Resolved(0, 1)).Contains("standardzone");

    /// <summary>
    /// Antimicrobial | Standard Zone | Zone observed, one table per organism. Media limits stay
    /// on the sheet (media qualification never binds to a Specification), so the printed
    /// standard zone is a read-only fixed column beside the analyst's reading; the antimicrobial
    /// is the fixed row header.
    /// </summary>
    private void ReadAntibioticTable(DocxBlock block, string caption)
    {
        var grid = DataGrid.Read(block.Table);
        var organism = caption ?? "organism";
        var location = ImportProposalBuilder.At(block);

        var standard = grid.Columns.First(column => column.SourceColumn == 1);
        if (standard.FixedValues is not null)
            standard.Reason = "Printed standard zone (acceptance range per antimicrobial): read-only fixed values";

        if (caption is null)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                "Antibiotic sensitivity table without an organism caption above it.", location);

        builder.Section("Antibiotic Sensitivity Test");
        builder.AddTable("antibiotic_sensitivity_" + ImportText.SnakeKey(organism, 40),
            $"Antibiotic sensitivity – {organism}", grid.Columns, location,
            "Antimicrobial and standard zone fixed per row; zone observed entered");
    }
}
