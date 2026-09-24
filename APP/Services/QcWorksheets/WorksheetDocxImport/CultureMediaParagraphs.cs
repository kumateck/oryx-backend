using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class CultureMediaWalker
{
    /// <summary>Labels that end in a colon yet head a block rather than ask for a value.</summary>
    private static readonly HashSet<string> ColonHeadings = ["conclusion", "conclusions", "results", "tests", "observations"];

    private const int SentenceLength = 80;

    [GeneratedRegex(@"\bObserved\b", RegexOptions.IgnoreCase)]
    private static partial Regex ObservedRegex();

    private void ReadParagraph(DocxBlock block, DocxBlock next)
    {
        var text = block.Text;
        if (block.Kind == DocxBlockKind.Heading)
        {
            StartSection(text);
            return;
        }

        if (_inReferences)
        {
            _references.Add((block, text));
            return;
        }

        if (SignOffTable.IsSignOff(text) || ImportText.Canonical(text) == "ph")
            return;

        if (TryReadPh(block, text))
            return;

        var choices = ChoicePhrases.Find(text);
        if (choices.Count > 0)
        {
            AddChoices(block, text, choices);
            return;
        }

        var hasLabel = ImportText.TrySplitLabel(text, out var label, out var value);
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var isHeadingLike = !ImportText.HasLeader(text) && wordCount <= 8
                            && (!hasLabel || (value.Length == 0 && ColonHeadings.Contains(ImportText.Canonical(label))));

        if (isHeadingLike)
        {
            if (next?.Table is not null)
                _caption = text.TrimEnd(':', ' ');
            // "Escherichia coli (ATCC 25922)" captions a table; it does not open a section.
            if (!text.Contains('(') && !text.Any(char.IsDigit))
                StartSection(text);
            return;
        }

        if (!hasLabel && ImportText.HasLeader(text))
        {
            var entryLabel = ImportText.StripLeaders(text);
            builder.Flag(WorksheetImportFlagCodes.UnrecognizedContent,
                $"A blank with no 'Label:' before it ('{entryLabel}'); proposed as a text entry.", ImportProposalBuilder.At(block));
            builder.AddField(new ProposedWorksheetField
            {
                Label = entryLabel, Type = WorksheetFieldType.ShortText, Mode = WorksheetFieldMode.Entry
            }, ImportProposalBuilder.At(block), ImportConfidence.Low, "Blank without a label");
            return;
        }

        // A long label is usually a sentence ("All inocula were prepared as per SOP …; SOP no.:
        // QCD/SOP/096") and is kept as printed text — but never when it carries a blank: a
        // leader is always something to fill in.
        if (!hasLabel || (label.Length > 60 && !ImportText.HasLeader(value) && !ImportText.IsBlank(value)))
        {
            AddInstructions(block, label is { Length: > 0 } && hasLabel && label.Length <= 60 ? label : null, text,
                ImportConfidence.Medium, "Printed method text");
            return;
        }

        var decision = ParameterTable.Decide(label, value);
        if (_inPreviousBatch)
            builder.AddDecision(decision with { Label = $"Previously approved batch – {decision.Label}" },
                ImportProposalBuilder.At(block), "previously_approved");
        else
            builder.AddDecision(decision, ImportProposalBuilder.At(block));
    }

    /// <summary>
    /// "pH Range: 7.2 ± 0.2   Observed: ……" (older form: "pH: 7.00-7.40 Observed:"). The
    /// printed range is a Specification proposal; the observed pH is an Entry field.
    /// </summary>
    private bool TryReadPh(DocxBlock block, string text)
    {
        if (!ImportText.Canonical(text).StartsWith("ph") || !ObservedRegex().IsMatch(text))
            return false;

        var location = ImportProposalBuilder.At(block);
        var rangePart = ObservedRegex().Split(text)[0];
        var field = builder.AddField(new ProposedWorksheetField
        {
            FieldKey = _inPreviousBatch ? "previously_approved_ph_observed" : "ph_observed",
            Label = _inPreviousBatch ? "Previously approved batch – pH (observed)" : "pH (observed)",
            Type = WorksheetFieldType.Number,
            Mode = WorksheetFieldMode.Entry
        }, location, ImportConfidence.High, "'Observed' blank beside the printed pH range");

        // The previously approved batch is checked against the same range: one proposal only.
        if (_inPreviousBatch)
            return true;

        if (PrintedSpecification.TryParseRange(rangePart, out var criteria))
            builder.Proposal.SpecificationProposals.Add(PrintedSpecification.Proposal(
                "pH", criteria, field.FieldKey, location, ImportConfidence.High));
        else
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No printed pH range: the specification must be supplied by hand.", location);

        return true;
    }

    /// <summary>
    /// A sentence carrying dictionary choice phrases: one Select/GrowthObservation per phrase,
    /// and, for a long criterion sentence, the printed wording as Instructions so the analyst
    /// sees exactly what they are asserting.
    /// </summary>
    private void AddChoices(DocxBlock block, string text, IReadOnlyList<ChoiceMatch> choices)
    {
        string baseLabel = null;
        var sentence = text;
        if (ImportText.TrySplitLabel(text, out var label, out var value) && text.IndexOf(':') < choices[0].Index
            && label.Length <= 120)
        {
            baseLabel = label;
            sentence = value;
        }

        var location = ImportProposalBuilder.At(block);
        var sectionName = builder.CurrentSection?.Name ?? "Result";

        if (sentence.Length > SentenceLength)
            AddInstructions(block, baseLabel ?? sectionName, sentence, ImportConfidence.High, "Printed criterion wording for the choice below");

        foreach (var choice in choices)
        {
            var fieldLabel = baseLabel is null
                ? $"{sectionName} – {choice.Topic}"
                : choices.Count > 1 ? $"{baseLabel} – {choice.Topic}" : baseLabel;

            var key = ImportText.Canonical(baseLabel) == "remark" && !builder.IsKeyTaken(CultureMediaKeys.Remark)
                ? CultureMediaKeys.Remark
                : choices.Count > 1
                    ? $"{ImportText.SnakeKey(baseLabel ?? sectionName, 45)}_{ImportText.SnakeKey(choice.Topic, 20)}"
                    : ImportText.SnakeKey(fieldLabel);

            builder.AddField(new ProposedWorksheetField
            {
                FieldKey = key,
                Label = fieldLabel,
                Type = choice.Type,
                Mode = WorksheetFieldMode.Entry,
                Options = choice.Options.ToList()
            }, location, ImportConfidence.High, $"Choice phrase '{text.Substring(choice.Index, choice.Length)}' from the curated dictionary");
        }
    }

    private void AddInstructions(DocxBlock block, string label, string text, ImportConfidence confidence, string reason) =>
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = ImportText.SnakeKey(label ?? text, 40) + "_text",
            Label = label ?? (text.Length > 80 ? text[..80].TrimEnd() + "…" : text),
            Type = WorksheetFieldType.Instructions,
            Mode = WorksheetFieldMode.Constant,
            ConstantValue = text
        }, ImportProposalBuilder.At(block), confidence, reason);
}
