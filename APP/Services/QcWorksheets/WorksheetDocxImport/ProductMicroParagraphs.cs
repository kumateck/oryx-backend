using System.Globalization;
using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class ProductMicroWalker
{
    private static readonly HashSet<string> ColonHeadings = ["conclusions", "results", "tests", "observations"];

    [GeneratedRegex(@"\s*\([^()]*\)\s*$")]
    private static partial Regex TrailingParenthesisRegex();

    private void ReadParagraph(DocxBlock block, DocxBlock next)
    {
        var text = block.Text;
        if (block.Kind == DocxBlockKind.Heading)
        {
            StartSection(text, organism: false);
            return;
        }

        if (SignOffTable.IsSignOff(text))
            return;

        // Water sheets cite a medium's qualification by the media sheet's serial number. The
        // serial is run data; the reference resolves by batch. None of the product sheets do
        // this, so it is flagged rather than guessed at.
        if (MediaReference.IsReferToSheet(text))
        {
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                "A media reference by batch-data serial number; the serial is run data and was not kept.",
                ImportProposalBuilder.At(block));
            return;
        }

        var plain = TrailingParenthesisRegex().Replace(text, string.Empty);
        if (EnumerationTests.TryGetValue(ImportText.Canonical(plain), out var prefix))
        {
            StartSection(plain, organism: false);
            (_prefix, _judgedTestName, _judgedKey, _judgedOptions) = (prefix, text, null, null);
            return;
        }

        var hasLabel = ImportText.TrySplitLabel(text, out var label, out var value);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        // "Absence of Escherichia coli": one section per specified organism.
        if (!hasLabel && words <= 8 && ImportText.Canonical(text).StartsWith("absenceof"))
        {
            StartSection(text, organism: true);
            var organism = text[text.IndexOf("of", StringComparison.OrdinalIgnoreCase)..][2..].Trim();
            (_prefix, _judgedTestName, _judgedKey, _judgedOptions) = (ImportText.SnakeKey(organism, 30), text, null, null);
            return;
        }

        if (PrintedSpecification.TryParseStatement(text, out var criteria))
        {
            AddSpecification(block, criteria);
            return;
        }

        var choices = ChoicePhrases.Find(text);
        if (choices.Count > 0)
        {
            if (hasLabel && ImportText.Canonical(label) is "conclusion" or "conclusions")
                StartSection("Conclusion", organism: false);

            var isResult = hasLabel && ImportText.Canonical(label).StartsWith("resultandinterpretation");
            var added = ChoiceFields.Add(builder, block, text, choices, _prefix,
                keyOverride: isResult ? Join(_prefix, "result") : null);
            if (isResult)
                (_judgedKey, _judgedOptions) = (added[0].FieldKey, added[0].Options);
            return;
        }

        var isHeadingLike = !ImportText.HasLeader(text) && words <= 8
                            && (!hasLabel || (value.Length == 0 && ColonHeadings.Contains(ImportText.Canonical(label))));
        if (isHeadingLike)
        {
            var caption = ImportText.StripEnumerator(text).TrimEnd(':', ' ');
            if (next?.Table is not null)
                _caption = caption;
            // Inside an organism's test, "Subculture" captions a table rather than opening a section;
            // "Tests:" above the test matrix opens none either, since the matrix opens one per test.
            if (!_inOrganism && !text.Contains('(') && !(next?.Table is { } upcoming && IsTestMatrix(upcoming)))
                StartSection(caption, organism: false);
            return;
        }

        if (!hasLabel || (label.Length > 60 && !ImportText.HasLeader(value)))
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

        _inOrganism = organism;
        if (!organism)
            (_prefix, _judgedKey, _judgedTestName, _judgedOptions) = (null, null, null, null);
        builder.Section(name);
    }
}
