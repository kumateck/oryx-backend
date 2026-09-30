using SHARED;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One block of a redacted document: mirrors <see cref="DocxBlock"/>/<see cref="ImportSourceBlock"/>, text only.</summary>
public sealed record RedactedBlock(int Index, string Kind, string Text, int? Table, List<List<string>> Rows);

/// <summary>
/// A document with every run-data value (brief 07's <see cref="RunDataLabels"/>) replaced by a
/// placeholder, produced by <see cref="DocumentRedactor"/>. This is the only shape the AI
/// extractor is allowed to see or send over the wire (build brief 10).
/// </summary>
public sealed class RedactedDocument
{
    public string HeaderText { get; init; } = string.Empty;
    public List<RedactedBlock> Blocks { get; init; } = [];

    /// <summary>Header + every block/cell, newline-joined — what is actually sent to the model, and what a proposed <c>sourceQuote</c> is checked against.</summary>
    public string FullText { get; init; } = string.Empty;
}

public static class RedactionErrors
{
    public static readonly Error UnrecognizedValueShape = Error.Validation(
        "QcWorksheetTemplate.RedactionUnrecognizedValueShape",
        "A run-data label was found but its value could not be confidently redacted, so the document was not sent for AI extraction.");
}

/// <summary>
/// Strips run data (batch/AR numbers, staff names, dates …) from a document's text before any
/// of it is allowed to leave the building for the AI extractor (build brief 10). Reuses
/// <see cref="RunDataLabels"/> exactly as the deterministic recognizers do: a label matched
/// here but whose value cannot be confidently identified is a hard failure, never an
/// unredacted send.
/// </summary>
public static class DocumentRedactor
{
    public static Result<RedactedDocument> Redact(DocxDocument document)
    {
        var header = RedactText(document.HeaderText);
        if (header.IsFailure) return Result.Failure<RedactedDocument>(header.Error);

        var blocks = new List<RedactedBlock>();
        var textParts = new List<string> { header.Value };

        foreach (var block in document.Blocks)
        {
            if (block.Table is null)
            {
                var text = RedactText(block.Text);
                if (text.IsFailure) return Result.Failure<RedactedDocument>(text.Error);
                blocks.Add(new RedactedBlock(block.Index, block.Kind.ToString(), text.Value, null, null));
                if (!string.IsNullOrWhiteSpace(text.Value)) textParts.Add(text.Value);
                continue;
            }

            var rows = new List<List<string>>();
            for (var row = 0; row < block.Table.Rows.Count; row++)
            {
                var cells = new List<string>();
                foreach (var cellText in block.Table.RowTexts(row))
                {
                    var redactedCell = RedactText(cellText);
                    if (redactedCell.IsFailure) return Result.Failure<RedactedDocument>(redactedCell.Error);
                    cells.Add(redactedCell.Value);
                    if (!string.IsNullOrWhiteSpace(redactedCell.Value)) textParts.Add(redactedCell.Value);
                }
                rows.Add(cells);
            }
            blocks.Add(new RedactedBlock(block.Index, block.Kind.ToString(), null, block.Table.Ordinal, rows));
        }

        return Result.Success(new RedactedDocument
        {
            HeaderText = header.Value,
            Blocks = blocks,
            FullText = string.Join("\n", textParts)
        });
    }

    /// <summary>
    /// Splits on every metadata label (<see cref="RunDataLabels.LabelStartRegex"/>) and, for a
    /// label that is a known run-data label, replaces the value that follows it with a
    /// placeholder. A matched label whose following value is empty (nothing to confidently
    /// redact) fails hard rather than being sent as-is.
    /// </summary>
    private static Result<string> RedactText(string text)
    {
        if (string.IsNullOrEmpty(text)) return Result.Success(text ?? string.Empty);

        var matches = RunDataLabels.LabelStartRegex().Matches(text);
        if (matches.Count == 0) return Result.Success(text);

        var builder = new System.Text.StringBuilder();
        var cursor = 0;
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var label = match.Groups[1].Value.Trim();
            var valueStart = match.Index + match.Length;
            var valueEnd = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var rawValue = text[valueStart..valueEnd];
            var trimmedValue = rawValue.Trim();

            builder.Append(text, cursor, match.Index - cursor);
            builder.Append(match.Value);

            if (RunDataLabels.TryMatch(label, out var runDataLabel))
            {
                if (trimmedValue.Length == 0)
                    return Result.Failure<string>(RedactionErrors.UnrecognizedValueShape);

                builder.Append(' ').Append(PlaceholderFor(runDataLabel.Key));
            }
            else
            {
                builder.Append(rawValue);
            }

            cursor = valueEnd;
        }

        builder.Append(text, cursor, text.Length - cursor);
        return Result.Success(builder.ToString());
    }

    private static string PlaceholderFor(string runDataKey) => runDataKey switch
    {
        "batch_no" or "lot_no" or "medium_batch_no" or "previously_approved_batch_no" => "[BATCH]",
        "mfg_date" or "exp_date" or "date_received" or "date_opened"
            or "analysis_start_date" or "analysis_end_date" or "sampled_on" or "issue_date" => "[DATE]",
        "sampled_by" or "issued_by" => "[NAME]",
        "ar_no" => "[AR_NUMBER]",
        _ => "[REDACTED]"
    };
}
