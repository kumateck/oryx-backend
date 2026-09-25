using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

public enum RunDataDisposition
{
    /// <summary>The analyst records it per run: an Entry field.</summary>
    Entry,

    /// <summary>Issuance data the system itself now owns (issue no., issuer, AR no.): dropped.</summary>
    HeaderData
}

public sealed record RunDataLabel(string Key, string Label, RunDataDisposition Disposition, WorksheetFieldType Type);

/// <summary>
/// The files are issued copies that carry run data. A label in this dictionary never produces
/// a Constant: its printed value is always discarded (brief 07, "Corpus facts").
/// </summary>
public static partial class RunDataLabels
{
    private static readonly Dictionary<string, RunDataLabel> ByCanonical = Build();

    /// <summary>
    /// Metadata labels that start a segment inside a "Label: value" cell. Cells hold several
    /// (e.g. "Sampled by: Date Sampled:"), and a value can run into the next label, so segments
    /// are split on known labels rather than on every colon.
    /// </summary>
    [GeneratedRegex(
        @"(?<![A-Za-z])(Batch\s*No\.?|Lot\s*No\.?|Culture\s+Medium\s+Name|Medium\s+Code|Issue\s*No\.?|Issue\s+Date|"
        + @"Issued\s+By|Format\s+No\.?|Serial\s+No\.?|Revision\s+No\.?|Analysis\s+Start\s+Date|Analysis\s+End\s+Date|"
        + @"Date\s+of\s+Mfg\.?|Date\s+of\s+Expiry|Date\s+Received|Date\s+Opened|Sampled\s+By|Sampled\s+On|Date\s+Sampled|"
        + @"A\.?\s*R\.?\s*No\.?|Mfg\.?\s*Date|Exp\.?\s*Date|Product\s+Name|Spec\.?\s*No\.?|SOP\s*No\.?)\s*:",
        RegexOptions.IgnoreCase)]
    public static partial Regex LabelStartRegex();

    // Dates (05/05/2026, 08/2021), batch-like codes (QCD/26/002/0000493532) and issue numbers
    // (26/0730). A value shaped like this under an unknown label is still treated as run data.
    [GeneratedRegex(@"\b\d{1,2}[/.-]\d{1,2}[/.-]\d{2,4}\b|\b\d{2}/\d{4}\b|\b[A-Z]{1,5}/\d{2}/\d{2,4}/\d{3,}\b|^\d{7,}$")]
    private static partial Regex RunDataValueRegex();

    public static bool TryMatch(string label, out RunDataLabel match) =>
        ByCanonical.TryGetValue(ImportText.Canonical(label), out match);

    public static bool LooksLikeRunData(string value) =>
        !string.IsNullOrWhiteSpace(value) && RunDataValueRegex().IsMatch(value.Trim());

    private static Dictionary<string, RunDataLabel> Build()
    {
        var entries = new (string[] Variants, string Key, string Label, RunDataDisposition Disposition, WorksheetFieldType Type)[]
        {
            (["Batch No", "Batch Number"], "batch_no", "Batch No.", RunDataDisposition.Entry, WorksheetFieldType.ShortText),
            (["Lot No", "Lot Number"], "lot_no", "Lot No.", RunDataDisposition.Entry, WorksheetFieldType.ShortText),
            (["Medium Batch no", "Medium Batch Number"], "medium_batch_no", "Medium Batch No.", RunDataDisposition.Entry, WorksheetFieldType.ShortText),
            (["Previously approved batch", "Previously approved lot", "Previously approved batch no", "Previously approved lot no"],
                "previously_approved_batch_no", "Previously approved batch/lot", RunDataDisposition.Entry, WorksheetFieldType.ShortText),
            (["Mfg Date", "Date of Mfg", "Mfg"], "mfg_date", "Mfg. Date", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Exp Date", "Date of Expiry", "Expiry Date"], "exp_date", "Exp. Date", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Date Received"], "date_received", "Date Received", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Date Opened"], "date_opened", "Date Opened", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Analysis Start Date"], "analysis_start_date", "Analysis Start Date", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Analysis End Date"], "analysis_end_date", "Analysis End Date", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Sampled On", "Date Sampled"], "sampled_on", "Sampled On", RunDataDisposition.Entry, WorksheetFieldType.Date),
            (["Sampled by"], "sampled_by", "Sampled By", RunDataDisposition.Entry, WorksheetFieldType.ShortText),
            (["A.R. No", "AR No", "AR Number", "A.R. Number"], "ar_no", "A.R. No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["Issue No", "Issue Number"], "issue_no", "Issue No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["Issue Date"], "issue_date", "Issue Date", RunDataDisposition.HeaderData, WorksheetFieldType.Date),
            (["Issued by"], "issued_by", "Issued By", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["Serial No", "Serial Number"], "serial_no", "Serial No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["Revision No"], "revision_no", "Revision No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            // Format No. is the controlled form's number, but two media sheets print a serial
            // ("QCD/MIC/BDCA/26/001") under it, so its value is never trusted as a Constant.
            (["Spec No", "Specification No"], "spec_no", "Spec. No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["SOP No"], "sop_no", "SOP No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText),
            (["Format No", "Format Number"], "format_no", "Format No.", RunDataDisposition.HeaderData, WorksheetFieldType.ShortText)
        };

        var map = new Dictionary<string, RunDataLabel>();
        foreach (var entry in entries)
        {
            foreach (var variant in entry.Variants)
                map[ImportText.Canonical(variant)] = new RunDataLabel(entry.Key, entry.Label, entry.Disposition, entry.Type);
        }

        return map;
    }
}
