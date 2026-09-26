using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class EnvironmentalMonitoringWalker
{
    /// <summary>
    /// The (stitched) room list: every row is a sampling point proposal; the airborne-viables
    /// column becomes this subject's one count. A ColonyCount entry rather than a Result: the Test
    /// Room renders a Result entry as a Pass/Fail choice, a ColonyCount as a number.
    /// </summary>
    private void ReadRooms(DocxBlock block)
    {
        var points = SamplingPointList.Read(block, area, SamplingPointType.Environmental);
        var known = builder.Proposal.SamplingPointProposals.GroupBy(point => ImportText.Canonical(point.Code))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var point in points)
        {
            if (!IsMissingCode(point.Code) && known.TryGetValue(ImportText.Canonical(point.Code), out var first))
            {
                // The same room printed twice is harmless; one code for two different rooms is not.
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                    ImportText.Canonical(first.Name) == ImportText.Canonical(point.Name)
                        ? $"Room '{point.Code}' ({point.Name}) is listed twice; proposed once."
                        : $"Room code '{point.Code}' is printed for two different rooms ('{first.Name}' and '{point.Name}'); "
                          + "only the first is proposed — give the other its own code.",
                    point.Location);
                continue;
            }

            if (!IsMissingCode(point.Code))
                known[ImportText.Canonical(point.Code)] = point;

            if (IsMissingCode(point.Code))
                builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                    $"'{point.Name}' has no room code (printed '{point.Code}'); give it a unique code before saving.", point.Location);
            builder.Proposal.SamplingPointProposals.Add(point);
        }

        if (_result is not null)
            return;

        var unit = DataGrid.Read(block.Table).Columns.FirstOrDefault(column => column.Unit?.Contains("CFU", StringComparison.OrdinalIgnoreCase) == true)?.Unit
                   ?? "CFU/4Hrs";
        builder.Section("Airborne Viables");
        _result = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = EnvironmentalMonitoringRecognizer.ResultKey,
            Label = $"Airborne viables ({unit})",
            Type = WorksheetFieldType.ColonyCount,
            Mode = WorksheetFieldMode.Entry,
            Unit = unit
        }, ImportProposalBuilder.At(block), ImportConfidence.High, "The per-room result column, for this subject's one room or point");
    }

    private static bool IsMissingCode(string code) =>
        ImportText.Canonical(code) is "" or "na" or "nil" or "none";

    /// <summary>
    /// AREA | SPECIFICATION: one tier per row ("Rooms NMT 100", "Dispensing Booth NMT 5"). A point
    /// joins the tier its name or code names (booths, LAF benches); every other point joins
    /// "Rooms". Groups are named after the printed tier, so the same tier across areas can share
    /// one SamplingPointGroup. The worksheet prints one limit — the Action limit.
    /// </summary>
    private void ReadTiers(DocxBlock block)
    {
        var table = block.Table;
        var tiers = Enumerable.Range(1, table.Rows.Count - 1)
            .Select(row => (Row: row, Name: table.Resolved(row, 0), Criteria: table.Resolved(row, 1)))
            .Where(tier => !ImportText.IsBlank(tier.Name) && !ImportText.IsBlank(tier.Criteria))
            .ToList();
        var defaultTier = tiers.FirstOrDefault(tier => ImportText.Canonical(tier.Name) is "rooms" or "room");
        var members = tiers.ToDictionary(tier => tier.Name, _ => new List<SamplingPointProposal>());

        foreach (var point in builder.Proposal.SamplingPointProposals)
        {
            var tier = tiers.FirstOrDefault(candidate => candidate != defaultTier && Matches(candidate.Name, point));
            if (tier == default)
                tier = defaultTier;
            if (tier == default)
                continue;
            point.GroupName = tier.Name;
            members[tier.Name].Add(point);
        }

        foreach (var tier in tiers)
        {
            var location = ImportProposalBuilder.At(block, tier.Row);
            builder.Proposal.SamplingPointGroupProposals.Add(new SamplingPointGroupProposal
            {
                Name = tier.Name, AcceptanceCriteria = tier.Criteria, PrintedPoints = tier.Name,
                PointCodes = members[tier.Name].Select(point => point.Code).ToList(), Location = location
            });

            builder.Proposal.SpecificationProposals.Add(new SpecificationCharacteristicProposal
            {
                TestName = _result?.Label ?? "Airborne viables",
                AcceptanceCriteria = tier.Criteria,
                ActionLimit = tier.Criteria,
                PrintedCriteria = $"{tier.Name}: {tier.Criteria}",
                SourceFieldKey = _result?.FieldKey,
                GroupName = tier.Name,
                Confidence = _result is null ? ImportConfidence.Low : ImportConfidence.High,
                Location = location
            });

            if (members[tier.Name].Count == 0)
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                    $"The tier '{tier.Name}' ({tier.Criteria}) matches none of this sheet's rooms.", location);
        }
    }

    /// <summary>"Dispensing Booth" names its points; "LAF Bench" / "Laminar Airflow Bench" match LAF units by name or code.</summary>
    private static bool Matches(string tierName, SamplingPointProposal point)
    {
        var tier = ImportText.Canonical(tierName);
        var name = ImportText.Canonical(point.Name);
        var code = ImportText.Canonical(point.Code);

        if (tier.Contains("laf") || tier.Contains("laminar"))
            return name.Contains("laf") || name.Contains("laminarair") || code.Contains("eqtlaf");

        return name.Contains(tier);
    }
}
