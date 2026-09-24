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
        var averages = PlateAverage.Apply(grid.Columns);
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

        var field = builder.AddTable(key, label, grid.Columns, location,
            grid.HasFixedRows ? $"Grid with {grid.DataRows.Count} fixed rows" : "Grid with open-ended rows");

        if (averages.Count > 0)
            builder.Flag(WorksheetImportFlagCodes.RowFormulaNotEvaluated,
                $"'{field.Label}': {string.Join(", ", averages.Select(column => column.Key))} carry a per-row average formula in "
                + "ColumnDefinitions. The current calculator evaluates scalar Calculated fields only, so these "
                + "cells are not computed or persisted until per-row column formulas are supported.",
                location);
    }

    private static bool IsAntibioticTable(DocxTable table) =>
        table.ColumnCount >= 3
        && ImportText.Canonical(table.Resolved(0, 0)).StartsWith("antimicrobial")
        && ImportText.Canonical(table.Resolved(0, 1)).Contains("standardzone");

    /// <summary>
    /// Antimicrobial | Standard Zone | Zone observed, one table per organism. The standard zone
    /// is a printed specification, so it becomes a Specification proposal per antimicrobial and
    /// is left out of the template; the antimicrobial is the fixed row header.
    /// </summary>
    private void ReadAntibioticTable(DocxBlock block, string caption)
    {
        var grid = DataGrid.Read(block.Table);
        var organism = caption ?? "organism";
        var location = ImportProposalBuilder.At(block);

        var standard = grid.Columns.First(column => column.SourceColumn == 1);
        var columns = grid.Columns.Where(column => column != standard).ToList();

        if (caption is null)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                "Antibiotic sensitivity table without an organism caption above it.", location);

        builder.Section("Antibiotic Sensitivity Test");
        var table = builder.AddTable("antibiotic_sensitivity_" + ImportText.SnakeKey(organism, 40),
            $"Antibiotic sensitivity – {organism}", columns, location,
            "Antimicrobial rows fixed; Standard Zone moved to specification proposals");

        foreach (var row in grid.DataRows)
        {
            var antimicrobial = block.Table.Resolved(row, 0);
            var zone = block.Table.Resolved(row, 1);
            if (ImportText.IsBlank(antimicrobial) || ImportText.IsBlank(zone))
                continue;

            builder.Proposal.SpecificationProposals.Add(PrintedSpecification.Proposal(
                $"Zone of inhibition – {antimicrobial}", zone, table.FieldKey, ImportProposalBuilder.At(block, row, 1),
                ImportConfidence.High, groupName: organism, analyte: antimicrobial));
        }
    }
}
