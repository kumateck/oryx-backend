using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The microbiology page of a finished-product ARD ("ANALYTICAL RAW DATA – MICROBIOLOGY").
/// <para>
/// Its metadata lives in the Word running header and is all run data or header context: the
/// product name names the template and gives the Specification proposals their context, and
/// nothing from the header becomes a Constant. The body is: media references, sample
/// preparation, the TAMC/TYMC enumeration (plates → calculated average → calculated result),
/// the specified-microorganism tests, equipment and reagents, and the conclusion.
/// </para>
/// </summary>
public sealed class ProductMicroRecognizer : IArdFamilyRecognizer
{
    public ArdFamily Family => ArdFamily.ProductMicro;

    public void Recognize(DocxDocument document, ImportProposalBuilder builder) =>
        new ProductMicroWalker(document, builder).Run();
}

internal sealed partial class ProductMicroWalker(DocxDocument document, ImportProposalBuilder builder)
{
    /// <summary>Enumeration tests by normalized name, with the key prefix each one's fields get.</summary>
    private static readonly Dictionary<string, string> EnumerationTests = new()
    {
        ["totalaerobicmicrobialcount"] = "tamc",
        ["totalcombinedyeastsandmouldscount"] = "tymc",
        ["totalyeastsandmouldscount"] = "tymc"
    };

    private string _productName;
    private string _specificationCode;
    private IReadOnlyDictionary<string, string> _mediumCodes = new Dictionary<string, string>();
    private HashSet<string> _citedMedia = [];

    // Walk state.
    private string _prefix;
    private bool _inOrganism;
    private string _caption;
    private string _judgedKey;
    private string _judgedTestName;
    private double? _dilutionFactor;
    private string _dilutionFactorKey;

    public void Run()
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.Microbial;
        template.Department = "Microbiology";

        ReadHeader();
        template.Name = _productName is null ? "Microbiology ARD" : $"Microbiology ARD – {_productName}";
        template.Code = _productName is null
            ? null
            : "MIC-" + ImportText.SnakeKey(_productName, 60).ToUpperInvariant().Replace('_', '-');

        // The reagent list sits at the end of the sheet, but the media references at the top need
        // its medium codes to find the media templates by code.
        _mediumCodes = document.Tables
            .Where(EquipmentTable.Is)
            .SelectMany(EquipmentTable.ReagentRows)
            .Where(row => !ImportText.IsBlank(row.Code))
            .GroupBy(row => ImportText.Canonical(row.Name))
            .ToDictionary(group => group.Key, group => group.First().Code);

        // The reagent list repeats the cited media; they are already Reagent fields (the batch
        // each media reference resolves by), so they are not added a second time.
        _citedMedia = document.Tables
            .Where(MediaReference.IsMediaReferenceTable)
            .SelectMany(MediaReference.Media)
            .Select(medium => ImportText.Canonical(medium.Name))
            .ToHashSet();

        for (var index = 0; index < document.Blocks.Count; index++)
        {
            var block = document.Blocks[index];
            var next = index + 1 < document.Blocks.Count ? document.Blocks[index + 1] : null;
            if (block.Table is not null)
                ReadTable(block);
            else
                ReadParagraph(block, next);
        }

        builder.Complete();
    }

    /// <summary>
    /// The running header: "Batch No.: … Product Name: … A.R. No.: … Spec. No.: …". Only the
    /// analysis dates become fields (the analyst records them); batch, A.R. no., Mfg/Exp,
    /// sampling and issuance belong to the test request and its sample, never the template.
    /// </summary>
    private void ReadHeader()
    {
        var header = document.HeaderText;
        var starts = RunDataLabels.LabelStartRegex().Matches(header).ToList();
        var location = new ImportSourceLocation { Header = true };
        var analysisDates = new List<ParameterDecision>();

        for (var index = 0; index < starts.Count; index++)
        {
            var end = index + 1 < starts.Count ? starts[index + 1].Index : header.Length;
            var label = ImportText.Normalize(starts[index].Value.TrimEnd(':', ' '));
            var value = ImportText.Normalize(header[(starts[index].Index + starts[index].Length)..end]);

            switch (ImportText.Canonical(label))
            {
                case "productname":
                    _productName ??= string.IsNullOrWhiteSpace(value) ? null : value;
                    break;
                case "specno":
                    _specificationCode ??= string.IsNullOrWhiteSpace(value) ? null : value;
                    break;
                case "analysisstartdate" or "analysisenddate":
                    var decision = ParameterTable.Decide(label, string.Empty);
                    if (analysisDates.All(existing => existing.Key != decision.Key))
                        analysisDates.Add(decision);
                    break;
            }
        }

        if (_productName is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No Product Name in the running header.", location);

        if (analysisDates.Count == 0)
            return;

        builder.Section("Analysis");
        foreach (var decision in analysisDates)
            builder.AddDecision(decision with { Reason = "Analysis date from the running header; entered per run" }, location);
    }

    private void AddSpecification(DocxBlock block, string criteria)
    {
        var location = ImportProposalBuilder.At(block);
        if (_judgedKey is null)
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                $"Specification '{criteria}' has no result field before it to constrain.", location);

        builder.Proposal.SpecificationProposals.Add(new SpecificationCharacteristicProposal
        {
            TestName = _judgedTestName ?? builder.CurrentSection?.Name ?? "Microbiology",
            AcceptanceCriteria = criteria,
            SourceFieldKey = _judgedKey,
            Stage = SpecificationStage.Finished,
            ProductName = _productName,
            SpecificationCode = _specificationCode,
            Confidence = _judgedKey is null ? ImportConfidence.Low
                : PrintedSpecification.IsLimit(criteria) ? ImportConfidence.High : ImportConfidence.Medium,
            Location = location
        });
    }

    private static string Join(params string[] parts) =>
        string.Join("_", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}
