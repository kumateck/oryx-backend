using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Environmental monitoring worksheets ("ENVIRONMENTAL MONITORING RAW DATA").
/// <para>
/// Locked decision 1: the template is for <b>one</b> room or point — each is its own
/// test-request subject — so it holds one airborne-viables count, never a room table. The
/// printed room list becomes SamplingPoint proposals, and the AREA | SPECIFICATION tiers become
/// SamplingPointGroup and Specification proposals. The worksheet body never names its area
/// (header and body are identical across areas), so the area is read from the file name, e.g.
/// "… WORKSHEET (Tablet).docx", and flagged for review.
/// </para>
/// </summary>
public sealed partial class EnvironmentalMonitoringRecognizer : IArdFamilyRecognizer
{
    public const string ResultKey = "airborne_viables";

    /// <summary>The one EM template every area sheet proposes, and its stable identity.</summary>
    public const string TemplateCode = "EM-AIRBORNE-VIABLES";

    public const string TemplateName = "Environmental Monitoring – Airborne Viables";

    public ArdFamily Family => ArdFamily.EnvironmentalMonitoring;

    [GeneratedRegex(@"\((?<area>[^()]+)\)[^()]*$")]
    private static partial Regex AreaInFileNameRegex();

    public static string AreaFromFileName(string fileName)
    {
        var match = AreaInFileNameRegex().Match(Path.GetFileNameWithoutExtension(fileName ?? string.Empty));
        return match.Success ? ImportText.Normalize(match.Groups["area"].Value) : null;
    }

    public void Recognize(DocxDocument document, ImportProposalBuilder builder) =>
        new EnvironmentalMonitoringWalker(document, builder, AreaFromFileName(builder.Proposal.FileName)).Run();
}

internal sealed partial class EnvironmentalMonitoringWalker(DocxDocument document, ImportProposalBuilder builder, string area)
{
    private string _caption;
    private ProposedWorksheetField _result;
    private HashSet<string> _citedMedia = [];

    public void Run()
    {
        var template = builder.Template;
        template.Category = WorksheetCategory.Microbial;
        template.Department = "Microbiology";
        // One template for every area: the area sheets are identical but for rooms, limits and
        // equipment, so the area lives on the sampling points, not in the template.
        template.Name = EnvironmentalMonitoringRecognizer.TemplateName;
        template.Code = EnvironmentalMonitoringRecognizer.TemplateCode;
        builder.Proposal.SharedTemplate = new SharedTemplateReference
        {
            Key = EnvironmentalMonitoringRecognizer.TemplateCode, CarriedBy = builder.Proposal.FileName
        };

        if (area is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                "The worksheet does not name its area and the file name has none in brackets; set the area on the sampling points.");
        else
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                $"The worksheet body does not name its area; '{area}' was taken from the file name. Confirm it.");

        _citedMedia = document.Tables.Where(MediaReference.IsMediaReferenceTable).SelectMany(MediaReference.Media)
            .Select(medium => ImportText.Canonical(medium.Name)).ToHashSet();
        var mediumCodes = document.Tables.Where(EquipmentTable.Is).SelectMany(EquipmentTable.ReagentRows)
            .Where(row => !ImportText.IsBlank(row.Code))
            .GroupBy(row => ImportText.Canonical(row.Name))
            .ToDictionary(group => group.Key, group => group.First().Code);

        for (var index = 0; index < document.Blocks.Count; index++)
        {
            var block = document.Blocks[index];
            var next = index + 1 < document.Blocks.Count ? document.Blocks[index + 1] : null;
            if (block.Table is not null)
                ReadTable(block, mediumCodes);
            else
                ReadParagraph(block, next);
        }

        if (_result is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No room results table was found: the airborne-viables count must be added by hand.");
        foreach (var point in builder.Proposal.SamplingPointProposals.Where(point => point.GroupName is null))
            builder.Flag(WorksheetImportFlagCodes.SamplingPointWithoutLimit,
                $"'{point.Code}' ({point.Name}) matches no printed specification tier.", point.Location);

        builder.Complete();
    }

    private void ReadParagraph(DocxBlock block, DocxBlock next)
    {
        var text = block.Text;
        if (SignOffTable.IsSignOff(text))
            return;

        var choices = ChoicePhrases.Find(text);
        if (choices.Count > 0)
        {
            builder.Section("Conclusion");
            ChoiceFields.Add(builder, block, text, choices);
            return;
        }

        var hasLabel = ImportText.TrySplitLabel(text, out var label, out var value);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (!ImportText.HasLeader(text) && words <= 8 && (!hasLabel || value.Length == 0))
        {
            var caption = ImportText.StripEnumerator(text).TrimEnd(':', ' ');
            if (next?.Table is not null)
                _caption = caption;
            else if (block.Kind == DocxBlockKind.Heading || ImportText.Canonical(caption) is not ("test" or "results"))
                builder.Section(block.Kind == DocxBlockKind.Heading
                    ? System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(caption.ToLowerInvariant())
                    : caption);
            return;
        }

        if (!hasLabel)
        {
            ChoiceFields.AddInstructions(builder, block, null, text, ImportConfidence.Medium, "Printed method text");
            return;
        }

        builder.AddDecision(ParameterTable.Decide(label, value), ImportProposalBuilder.At(block));
    }

    private void ReadTable(DocxBlock block, IReadOnlyDictionary<string, string> mediumCodes)
    {
        var table = block.Table;
        var caption = _caption;
        _caption = null;

        if (SignOffTable.Is(table))
            return;

        if (MediaReference.IsMediaReferenceTable(table))
        {
            MediaReference.Apply(block, builder, mediumCodes);
            return;
        }

        if (EquipmentTable.Is(table))
        {
            // Keyed by equipment code, so the same instrument on several area sheets is one field.
            EquipmentTable.Apply(block, builder, _citedMedia, keyByCode: true);
            return;
        }

        if (PrintedSpecification.IsAreaSpecificationTable(table))
        {
            ReadTiers(block);
            return;
        }

        if (SamplingPointList.Is(table))
        {
            ReadRooms(block);
            return;
        }

        // "Analysis Start Date: | Analysis End Date:" — labelled cells, not a label | value table.
        if (ParameterTable.ReadLabelledCells(table) is { Count: > 0 } labelled
            && labelled.All(item => item.Decision.Key is "analysis_start_date" or "analysis_end_date"))
        {
            builder.Section("Analysis");
            foreach (var (cell, decision) in labelled)
                builder.AddDecision(decision, ImportProposalBuilder.At(block, cell.Row, cell.Column));
            return;
        }

        if (ParameterTable.IsTwoColumnParameterTable(table))
        {
            builder.Section(caption ?? "Method");
            foreach (var (row, decision) in ParameterTable.ReadTwoColumn(table))
                builder.AddDecision(decision, ImportProposalBuilder.At(block, row));
            return;
        }

        var grid = DataGrid.Read(table);
        builder.AddTable(ImportText.SnakeKey(caption ?? "table", 30), caption ?? "Table", grid.Columns,
            ImportProposalBuilder.At(block), "Unrecognized grid, proposed as a table");
        builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
            "A table that matches no EM-sheet layout; proposed as a generic table.", ImportProposalBuilder.At(block));
    }
}
