using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Turns a sentence carrying dictionary choice phrases into fields, for every recognizer: one
/// Select / GrowthObservation per phrase, and — for a long criterion sentence — the printed
/// wording as Instructions, so the analyst sees exactly what they are asserting.
/// </summary>
public static class ChoiceFields
{
    private const int SentenceLength = 80;

    /// <param name="keyPrefix">Prepended to generated keys ("tamc" → "tamc_remark").</param>
    /// <param name="reservedRemarkKey">A fixed key for the first "Remark:" choice (media: "remark").</param>
    /// <param name="keyOverride">The key for a single-phrase sentence, when the caller needs a known key.</param>
    public static IReadOnlyList<ProposedWorksheetField> Add(
        ImportProposalBuilder builder, DocxBlock block, string text, IReadOnlyList<ChoiceMatch> choices,
        string keyPrefix = null, string reservedRemarkKey = null, string keyOverride = null)
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
            AddInstructions(builder, block, baseLabel ?? sectionName, sentence, ImportConfidence.High,
                "Printed criterion wording for the choice below", keyPrefix);

        var added = new List<ProposedWorksheetField>();
        foreach (var choice in choices)
        {
            var fieldLabel = baseLabel is null
                ? $"{sectionName} – {choice.Topic}"
                : choices.Count > 1 ? $"{baseLabel} – {choice.Topic}" : baseLabel;

            string key;
            if (reservedRemarkKey is not null && ImportText.Canonical(baseLabel) == "remark" && !builder.IsKeyTaken(reservedRemarkKey))
                key = reservedRemarkKey;
            else if (keyOverride is not null && choices.Count == 1)
                key = keyOverride;
            else
                key = Prefixed(keyPrefix, choices.Count > 1
                    ? $"{ImportText.SnakeKey(baseLabel ?? sectionName, 45)}_{ImportText.SnakeKey(choice.Topic, 20)}"
                    : ImportText.SnakeKey(fieldLabel));

            added.Add(builder.AddField(new ProposedWorksheetField
            {
                FieldKey = key,
                Label = fieldLabel,
                Type = choice.Type,
                Mode = WorksheetFieldMode.Entry,
                Options = choice.Options.ToList()
            }, location, ImportConfidence.High, $"Choice phrase '{text.Substring(choice.Index, choice.Length)}' from the curated dictionary"));
        }

        return added;
    }

    public static ProposedWorksheetField AddInstructions(
        ImportProposalBuilder builder, DocxBlock block, string label, string text, ImportConfidence confidence,
        string reason, string keyPrefix = null) =>
        builder.AddField(new ProposedWorksheetField
        {
            FieldKey = Prefixed(keyPrefix, ImportText.SnakeKey(label ?? text, 40) + "_text"),
            Label = label ?? (text.Length > 80 ? text[..80].TrimEnd() + "…" : text),
            Type = WorksheetFieldType.Instructions,
            Mode = WorksheetFieldMode.Constant,
            ConstantValue = text
        }, ImportProposalBuilder.At(block), confidence, reason);

    private static string Prefixed(string prefix, string key) =>
        string.IsNullOrWhiteSpace(prefix) ? key : $"{prefix}_{key}";
}
