using System.Globalization;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class PurifiedWaterWalker
{
    private static readonly HashSet<string> ColonHeadings = ["results", "tests", "conclusions"];

    private void ReadParagraph(DocxBlock block, DocxBlock next)
    {
        var text = block.Text;
        if (block.Kind == DocxBlockKind.Heading)
        {
            StartSection(text, organism: false);
            return;
        }

        if (_inReferences)
        {
            _references.Add((block, text));
            return;
        }

        if (SignOffTable.IsSignOff(text))
            return;

        var hasLabel = ImportText.TrySplitLabel(text, out var label, out var value);
        var canonicalLabel = hasLabel ? ImportText.Canonical(label) : string.Empty;

        // Media cited as paragraphs: a caption naming the medium, "Medium Batch no.: …", then
        // "Results: Refer to … serial numbered …" and the medium's own Remark. The batch is the
        // resolution key; the serial and the remark belong to the media sheet, not this one.
        if (canonicalLabel.StartsWith("mediumbatchno"))
        {
            var name = _previous?.Table is null ? ImportText.StripEnumerator(_previous?.Text ?? string.Empty) : string.Empty;
            if (ImportText.IsBlank(name))
                builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent, "A medium batch number with no medium named above it.", ImportProposalBuilder.At(block));
            else
                MediaReference.AddMedium(builder, name, ImportProposalBuilder.At(block), _mediumCodes);
            _citedMedia.Add(ImportText.Canonical(name));
            _inMediaCitations = true;
            return;
        }

        if (MediaReference.IsReferToSheet(text) || (_inMediaCitations && canonicalLabel == "remark"))
            return;

        if (TryReadSpecification(block, text))
            return;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (!hasLabel && words <= 8 && ImportText.Canonical(text).StartsWith("absenceof"))
        {
            StartSection(text, organism: true);
            var organism = text[(text.IndexOf("of", StringComparison.OrdinalIgnoreCase) + 2)..].Trim();
            _prefix = ImportText.SnakeKey(organism, 30);
            _organismPrefixes[OrganismKey(organism)] = _prefix;
            _organismSections[OrganismKey(organism)] = builder.CurrentSection.Name;
            return;
        }

        var choices = ChoicePhrases.Find(text);
        if (choices.Count > 0)
        {
            var remarkPrefix = _count is not null && _pathogenResults.Count == 0 ? "count" : _pathogenResults.Count > 0 ? "pathogens" : _prefix;
            if (_pathogenResults.Count > 0)
                builder.Section("Specified microorganisms – results");
            ChoiceFields.Add(builder, block, text, choices, remarkPrefix);
            return;
        }

        var isHeadingLike = !ImportText.HasLeader(text) && words <= 10
                            && (!hasLabel || (value.Length == 0 && ColonHeadings.Contains(canonicalLabel)));
        if (isHeadingLike)
        {
            var caption = ImportText.StripEnumerator(text).TrimEnd(':', ' ');
            _inMediaCitations = _inMediaCitations && next?.Table is null && !canonicalLabel.Equals("results");
            if (next?.Table is not null)
                _caption = caption;
            else if (!_inOrganism && ImportText.Canonical(caption) is not "results")
                StartSection(caption, organism: false);
            return;
        }

        if (!hasLabel || label.Length > 60)
        {
            ChoiceFields.AddInstructions(builder, block, hasLabel && label.Length <= 60 ? label : null, text,
                ImportConfidence.Medium, "Printed method text", _prefix);
            return;
        }

        builder.AddDecision(ParameterTable.Decide(label, value), ImportProposalBuilder.At(block), _prefix);
    }

    private void StartSection(string heading, bool organism)
    {
        var name = ImportText.StripEnumerator(heading).TrimEnd(':', '.', ' ');
        if (!name.Any(char.IsLower))
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());

        var canonical = ImportText.Canonical(name);
        _inReferences = canonical is "references" or "reference";
        _inOrganism = organism;
        _inMediaCitations = canonical.StartsWith("growthpromotion");
        if (!organism)
            _prefix = null;
        builder.Section(name);
    }

    /// <summary>"Escherichia coli" and "Salmonella spp." compared by genus and species only.</summary>
    private static string OrganismKey(string organism) =>
        ImportText.Canonical(organism.Replace("spp.", string.Empty, StringComparison.OrdinalIgnoreCase));
}
