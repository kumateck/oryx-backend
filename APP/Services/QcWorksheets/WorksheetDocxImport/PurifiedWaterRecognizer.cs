using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Purified-water sheets ("MICROBIOLOGY ANALYTICAL WORKSHEET … WATER").
/// <para>
/// Locked decision 1: the template is for <b>one</b> sampling point — each point is its own
/// test-request subject with its own worksheet — so it holds one subject's fields: CFU/100 mL,
/// CFU/mL (calculated from it) and the four specified-pathogen results. The printed point list
/// becomes SamplingPoint proposals, and the per-point limit tiers become SamplingPointGroup
/// proposals with a Specification proposal each. A water sheet listing 11 points and one listing
/// every point therefore propose the same template.
/// </para>
/// </summary>
public sealed class PurifiedWaterRecognizer : IArdFamilyRecognizer
{
    public const string Area = "Purified Water";

    public ArdFamily Family => ArdFamily.PurifiedWater;

    public void Recognize(DocxDocument document, ImportProposalBuilder builder) =>
        new PurifiedWaterWalker(document, builder).Run();
}

internal sealed partial class PurifiedWaterWalker(DocxDocument document, ImportProposalBuilder builder)
{
    private readonly Dictionary<string, string> _organismPrefixes = new();
    private readonly Dictionary<string, string> _organismSections = new();
    private readonly List<(ProposedWorksheetField Field, string Organism)> _pathogenResults = [];
    private readonly List<(DocxBlock Block, string Text)> _references = [];
    private IReadOnlyDictionary<string, string> _mediumCodes = new Dictionary<string, string>();
    private HashSet<string> _citedMedia = [];

    // Walk state.
    private string _prefix;
    private bool _inOrganism;
    private bool _inMediaCitations;
    private bool _inReferences;
    private string _caption;
    private DocxBlock _previous;
    private ProposedWorksheetField _count;
    private string _countTestName;

    public void Run()
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.Microbial;
        template.Department = "Microbiology";
        template.Code = "MIC-PURIFIED-WATER";
        template.Name = "Microbiology – Purified Water (per sampling point)";

        var metadata = document.Blocks.FirstOrDefault(block => block.Table is not null);
        if (metadata is not null)
            ReadMetadata(metadata);

        _mediumCodes = document.Tables.Where(EquipmentTable.Is).SelectMany(EquipmentTable.ReagentRows)
            .Where(row => !ImportText.IsBlank(row.Code))
            .GroupBy(row => ImportText.Canonical(row.Name))
            .ToDictionary(group => group.Key, group => group.First().Code);

        for (var index = 0; index < document.Blocks.Count; index++)
        {
            var block = document.Blocks[index];
            if (block == metadata)
                continue;

            var next = index + 1 < document.Blocks.Count ? document.Blocks[index + 1] : null;
            if (block.Table is not null)
                ReadTable(block);
            else
                ReadParagraph(block, next);
            _previous = block;
        }

        FlagUngroupedPoints();
        if (_count is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No CFU results table was found: the count fields must be added by hand.");
        if (_pathogenResults.Count == 0)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No specified-pathogen results table was found.");

        if (_references.Count > 0)
        {
            builder.Section("References");
            ChoiceFields.AddInstructions(builder, _references[0].Block, "References",
                string.Join("; ", _references.Select(item => item.Text)), ImportConfidence.High, "Printed reference list");
        }

        builder.Complete();
    }

    /// <summary>
    /// "Issue no | WATER | Sampled by / Date Sampled | Issue date | Analysis Start/End Date | AR No |
    /// Issued by": run data for the round. Only the analysis dates become (Entry) fields.
    /// </summary>
    private void ReadMetadata(DocxBlock block)
    {
        builder.Section("Analysis");
        foreach (var (cell, decision) in ParameterTable.ReadLabelledCells(block.Table))
        {
            if (decision.Key is "analysis_start_date" or "analysis_end_date")
                builder.AddDecision(decision, ImportProposalBuilder.At(block, cell.Row, cell.Column));
        }
    }

    /// <summary>"Specifications: SP1 – NMT 500cfu/mL" and its continuation lines, or the pathogen statement.</summary>
    private bool TryReadSpecification(DocxBlock block, string text)
    {
        var statement = PrintedSpecification.TryParseStatement(text, out var criteria) ? criteria : null;
        var location = ImportProposalBuilder.At(block);

        if (WaterSpecificationTiers.TryParse(statement ?? text, out var tier))
        {
            AddTier(tier, location);
            return true;
        }

        if (statement is null)
            return false;

        if (_pathogenResults.Count > 0 && ImportText.Canonical(statement).StartsWith("absence"))
        {
            foreach (var (field, organism) in _pathogenResults)
                builder.Proposal.SpecificationProposals.Add(new SpecificationCharacteristicProposal
                {
                    TestName = $"Absence of {organism}",
                    AcceptanceCriteria = PrintedSpecification.CompliantOption(statement, field.Options) ?? statement,
                    PrintedCriteria = statement,
                    SourceFieldKey = field.FieldKey,
                    Confidence = ImportConfidence.High,
                    Location = location
                });
            return true;
        }

        builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, $"Specification '{statement}' matches no result.", location);
        return true;
    }

    private void AddTier(WaterTier tier, ImportSourceLocation location)
    {
        var points = builder.Proposal.SamplingPointProposals;
        var members = points.Where(point => tier.PointKeys.Contains(WaterSpecificationTiers.PointKey(point.Code))).ToList();
        var name = $"Purified water – {tier.Criteria}";

        foreach (var point in members)
            point.GroupName ??= name;

        builder.Proposal.SamplingPointGroupProposals.Add(new SamplingPointGroupProposal
        {
            Name = name, AcceptanceCriteria = tier.Criteria, PrintedPoints = tier.PrintedPoints,
            PointCodes = members.Select(point => point.Code).ToList(), Location = location
        });

        builder.Proposal.SpecificationProposals.Add(new SpecificationCharacteristicProposal
        {
            TestName = _countTestName ?? "Microbial count",
            AcceptanceCriteria = tier.Criteria,
            PrintedCriteria = $"{tier.PrintedPoints} – {tier.Criteria}",
            SourceFieldKey = _count?.FieldKey,
            GroupName = name,
            Confidence = _count is null ? ImportConfidence.Low : ImportConfidence.High,
            Location = location
        });

        foreach (var duplicate in tier.Duplicates)
            builder.Flag(WorksheetImportFlagCodes.SamplingPointWithoutLimit,
                $"'{duplicate}' is listed twice in the tier '{tier.PrintedPoints}'.", location);
    }

    /// <summary>Points no tier covers, flagged once every tier has been read.</summary>
    private void FlagUngroupedPoints()
    {
        if (builder.Proposal.SamplingPointGroupProposals.Count == 0)
            return;
        foreach (var point in builder.Proposal.SamplingPointProposals.Where(point => point.GroupName is null))
            builder.Flag(WorksheetImportFlagCodes.SamplingPointWithoutLimit,
                $"Sampling point '{point.Code}' ({point.Name}) is in no printed limit tier.", point.Location);
    }
}
