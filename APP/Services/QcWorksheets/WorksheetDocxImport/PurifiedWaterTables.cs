using System.Globalization;
using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class PurifiedWaterWalker
{
    private double? _filteredVolume;

    [GeneratedRegex(@"(?<volume>\d+(?:\.\d+)?)\s*mL", RegexOptions.IgnoreCase)]
    private static partial Regex VolumeRegex();

    private void ReadTable(DocxBlock block)
    {
        var table = block.Table;
        var caption = _caption;
        _caption = null;

        if (SignOffTable.Is(table))
            return;

        if (EquipmentTable.Is(table))
        {
            (_prefix, _inOrganism) = (null, false);
            EquipmentTable.Apply(block, builder, _citedMedia);
            return;
        }

        if (SamplingPointList.Is(table))
        {
            ReadPointTable(block);
            return;
        }

        if (ParameterTable.IsTwoColumnParameterTable(table))
        {
            ReadParameters(block, ParameterTable.Title(table) ?? caption);
            return;
        }

        var grid = DataGrid.Read(table);
        builder.AddTable(ImportText.SnakeKey(caption ?? "table", 30), caption ?? "Table", grid.Columns,
            ImportProposalBuilder.At(block), "Unrecognized grid, proposed as a table");
        builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
            "A table that matches no water-sheet layout; proposed as a generic table.", ImportProposalBuilder.At(block));
    }

    private void ReadParameters(DocxBlock block, string title)
    {
        var group = title is null || string.Equals(title, builder.CurrentSection?.Name, StringComparison.OrdinalIgnoreCase) ? null : title;
        // Outside an organism the step is the microbial count itself ("count_method"); inside one
        // it is a step of that organism's test ("escherichia_coli_selection_medium").
        var keyPrefix = _inOrganism
            ? string.Join("_", new[] { _prefix, group is null ? null : ImportText.StepKey(group) }.Where(part => !string.IsNullOrWhiteSpace(part)))
            : "count";

        if (!_inOrganism && group is not null)
            builder.Section(group);

        foreach (var (row, decision) in ParameterTable.ReadTwoColumn(block.Table))
        {
            var labelled = group is null || !_inOrganism ? decision : decision with { Label = $"{group} – {decision.Label}" };
            builder.AddDecision(labelled, ImportProposalBuilder.At(block, row), keyPrefix.Length == 0 ? null : keyPrefix);

            if (ImportText.Canonical(decision.Label).StartsWith("volumeofwatersamplefiltered")
                && VolumeRegex().Match(decision.ConstantValue ?? string.Empty) is { Success: true } volume)
                _filteredVolume = double.Parse(volume.Groups["volume"].Value, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A point list with results columns. The rows are sampling points (proposals, never template
    /// rows); the columns are one subject's results: CFU/100 mL and CFU/mL, or the four pathogens.
    /// The AR number column is run data.
    /// </summary>
    private void ReadPointTable(DocxBlock block)
    {
        var known = builder.Proposal.SamplingPointProposals.Select(point => WaterSpecificationTiers.PointKey(point.Code)).ToHashSet();
        builder.Proposal.SamplingPointProposals.AddRange(
            SamplingPointList.Read(block, PurifiedWaterRecognizer.Area, SamplingPointType.Water)
                .Where(point => known.Add(WaterSpecificationTiers.PointKey(point.Code))));

        var columns = DataGrid.Read(block.Table).Columns;
        var perHundred = columns.FirstOrDefault(column => Regex.IsMatch(column.Label, @"cfu\s*/\s*\d+\s*ml", RegexOptions.IgnoreCase));
        var pathogens = columns.Where(column => _organismPrefixes.ContainsKey(OrganismKey(column.Label))).ToList();

        if (perHundred is not null)
            AddCountResults(block, perHundred);
        else if (pathogens.Count > 0)
            AddPathogenResults(block, pathogens);
        else
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                "A sampling-point table with no CFU or pathogen result columns.", ImportProposalBuilder.At(block));
    }

    /// <summary>
    /// The membrane filters a known volume (100 mL), so the plate count is CFU per that volume
    /// and CFU/mL is it divided by the volume — a Calculated field, and the one the printed
    /// "NMT … cfu/mL" limits are judged against (Result-typed, like the product results).
    /// </summary>
    private void AddCountResults(DocxBlock block, GridColumn perHundred)
    {
        var location = ImportProposalBuilder.At(block);
        var printedVolume = VolumeRegex().Match(perHundred.Label);
        var volume = printedVolume.Success ? double.Parse(printedVolume.Groups["volume"].Value, CultureInfo.InvariantCulture) : 100;

        if (_filteredVolume is { } filtered && Math.Abs(filtered - volume) > 0.0001)
            builder.Flag(WorksheetImportFlagCodes.IncompleteValue,
                $"The result column is per {volume} mL but {filtered} mL is filtered; check the CFU/mL formula.", location);

        _countTestName = "Microbial count (CFU/mL)";
        builder.Section("Microbial Count");
        var count = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = "cfu_per_100ml", Label = $"Count (CFU/{QcWorksheetCalculator.Format(volume)} mL)",
            Type = WorksheetFieldType.ColonyCount, Mode = WorksheetFieldMode.Entry, Unit = $"CFU/{QcWorksheetCalculator.Format(volume)}mL"
        }, ImportProposalBuilder.At(block, null, perHundred.SourceColumn), ImportConfidence.High,
            "The per-point result column, for this subject's one sampling point");

        _count = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = "cfu_per_ml", Label = "Result (CFU/mL)", Type = WorksheetFieldType.Result, Mode = WorksheetFieldMode.Calculated,
            Unit = "CFU/mL", FormulaExpression = $"{{{count.FieldKey}}} / {QcWorksheetCalculator.Format(volume)}"
        }, location, ImportConfidence.Medium, $"CFU/mL = CFU per {volume} mL filtered ÷ {volume}; reviewer confirms");
    }

    /// <summary>One Absent / Detected result per pathogen, in that pathogen's own section.</summary>
    private void AddPathogenResults(DocxBlock block, IReadOnlyList<GridColumn> pathogens)
    {
        foreach (var column in pathogens)
        {
            var organism = column.Label;
            var prefix = _organismPrefixes[OrganismKey(organism)];
            var firstCell = Enumerable.Range(0, block.Table.Rows.Count).Select(row => block.Table.Resolved(row, column.SourceColumn))
                .FirstOrDefault(text => ChoicePhrases.Find(text).Count > 0);
            var options = firstCell is null ? ["Absent", "Detected"] : ChoicePhrases.Find(firstCell)[0].Options.ToList();

            builder.Section(_organismSections.GetValueOrDefault(OrganismKey(organism)) ?? $"Absence of {organism}");
            var field = builder.AddField(new ProposedWorksheetField
            {
                FieldKey = $"{prefix}_result", Label = $"{organism} – result", Type = WorksheetFieldType.Select,
                Mode = WorksheetFieldMode.Entry, Options = options
            }, ImportProposalBuilder.At(block, null, column.SourceColumn),
                firstCell is null ? ImportConfidence.Medium : ImportConfidence.High,
                firstCell is null ? "Pathogen column without printed choices; Absent / Detected assumed" : $"Printed '{firstCell}' in the pathogen column");
            _pathogenResults.Add((field, organism));
        }
    }
}
