using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

internal sealed partial class CultureMediaWalker
{
    /// <summary>Labels that end in a colon yet head a block rather than ask for a value.</summary>
    private static readonly HashSet<string> ColonHeadings = ["conclusion", "conclusions", "results", "tests", "observations"];

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
    /// "pH Range: 7.2 ± 0.2   Observed: ……" (older form: "pH: 7.00-7.40 Observed:").
    /// <para>
    /// Media qualification never binds to a Specification (field-catalog refinement 1), so the
    /// printed range stays on the sheet as a read-only, Result-typed Constant holding the
    /// inline acceptance criteria. It cannot be one field with the observed value: a Constant
    /// field accepts no entry, so the observed pH is its own Number entry beside it.
    /// </para>
    /// </summary>
    private bool TryReadPh(DocxBlock block, string text)
    {
        if (!ImportText.Canonical(text).StartsWith("ph") || !ObservedRegex().IsMatch(text))
            return false;

        var location = ImportProposalBuilder.At(block);
        var prefix = _inPreviousBatch ? "previously_approved_" : string.Empty;
        var labelPrefix = _inPreviousBatch ? "Previously approved batch – " : string.Empty;

        if (PrintedSpecification.TryParseRange(ObservedRegex().Split(text)[0], out var criteria))
            builder.AddField(new ProposedWorksheetField
            {
                FieldKey = prefix + "ph_range",
                Label = labelPrefix + "pH range (acceptance)",
                Type = WorksheetFieldType.Result,
                Mode = WorksheetFieldMode.Constant,
                ConstantValue = criteria
            }, location, ImportConfidence.High, "Printed pH range: inline acceptance criteria, read-only");
        else if (!_inPreviousBatch)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No printed pH range: add the acceptance range by hand.", location);

        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = prefix + "ph_observed",
            Label = labelPrefix + "pH (observed)",
            Type = WorksheetFieldType.Number,
            Mode = WorksheetFieldMode.Entry
        }, location, ImportConfidence.High, "'Observed' blank beside the printed pH range");

        return true;
    }

    private void AddChoices(DocxBlock block, string text, IReadOnlyList<ChoiceMatch> choices) =>
        ChoiceFields.Add(builder, block, text, choices, reservedRemarkKey: CultureMediaKeys.Remark);

    private void AddInstructions(DocxBlock block, string label, string text, ImportConfidence confidence, string reason) =>
        ChoiceFields.AddInstructions(builder, block, label, text, confidence, reason);
}
