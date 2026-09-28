using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// The running headers of the raw-material pair (brief 09). Both carry the three-digit material
/// number NNN that pairs a worksheet ("NNN - Material.docx", "Spec. No.: NQC/RM/SPC/NNN") with its
/// Specification document ("NNN Material spec.docx", "SPC No.: QCD/SPC/RM/NNN").
/// <para>
/// Everything else in the worksheet header is run data — batch, quantities, dates, sampler,
/// issuer, GRN, supplier — and is dropped: only the material name is kept, to name the template.
/// </para>
/// </summary>
public static partial class RawMaterialHeader
{
    [GeneratedRegex(@"^\s*(\d{3})(?!\d)")]
    private static partial Regex FilePrefixRegex();

    // The value runs up to the next header label; the labels after it vary by sheet.
    [GeneratedRegex(@"Raw\s+Material\s+Name\s*:\s*(?<name>.*?)\s+(?:Quantity\s+Received|A\.?\s*R\.?\s*No|Batch\s*No|Mfg)", RegexOptions.IgnoreCase)]
    private static partial Regex MaterialNameRegex();

    [GeneratedRegex(@"Spec\.?\s*No\.?\s*:\s*(?<code>\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex WorksheetSpecNumberRegex();

    // "SPC No.: QCD/SPC/RM/012 CIPROFLOXACIN HYDROCHLORIDE Revision No.: 05 Effective Date: …"
    [GeneratedRegex(@"\b(?:SPC|Specification)\s*No\.?\s*:\s*(?<code>\S+)\s+(?<name>.*?)\s*Revision\s*No\.?\s*:\s*(?<revision>[^\s:]+)?", RegexOptions.IgnoreCase)]
    private static partial Regex SpecificationTitleRegex();

    [GeneratedRegex(@"(\d{3})(?!.*\d)")]
    private static partial Regex LastNumberRegex();

    public const string CodePrefix = "RM-";

    public static string TemplateCode(string pairingKey) => pairingKey is null ? null : CodePrefix + pairingKey;

    /// <summary>The worksheet's material name, pairing key and flags; nothing becomes a field.</summary>
    public static RawMaterialDocumentInfo ReadWorksheet(string fileName, DocxDocument document, ImportProposalBuilder builder)
    {
        var header = document.HeaderText;
        var location = new ImportSourceLocation { Header = true };
        var name = MaterialNameRegex().Match(header) is { Success: true } match ? ImportText.Normalize(match.Groups["name"].Value) : null;
        var specKey = WorksheetSpecNumberRegex().Match(header) is { Success: true } spec ? Key(spec.Groups["code"].Value) : null;

        if (name is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No Raw Material Name in the running header.", location);

        var key = PairingKey(fileName, specKey, builder, location);
        return new RawMaterialDocumentInfo { PairingKey = key, TemplateCode = TemplateCode(key), MaterialName = name };
    }

    /// <summary>The Specification document's SPC number, title and revision.</summary>
    public static RawMaterialDocumentInfo ReadSpecification(string fileName, DocxDocument document, ImportProposalBuilder builder)
    {
        var location = new ImportSourceLocation { Header = true };
        var match = SpecificationTitleRegex().Match(document.HeaderText);
        var code = match.Success ? match.Groups["code"].Value.Trim() : null;
        var name = match.Success ? ImportText.Normalize(match.Groups["name"].Value) : null;
        var revision = match.Success && match.Groups["revision"].Success ? match.Groups["revision"].Value : null;

        if (string.IsNullOrWhiteSpace(name))
        {
            name = null;
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata, "No material name between the SPC number and 'Revision No.'.", location);
        }

        var key = PairingKey(fileName, Key(code), builder, location);
        return new RawMaterialDocumentInfo
        {
            PairingKey = key, TemplateCode = TemplateCode(key), MaterialName = name, SpecificationCode = code, Revision = revision,
            Pairing = RawMaterialPairingStatus.Missing
        };
    }

    /// <summary>The file name prefix wins (it is how the lab files the pair); the printed number backs it up.</summary>
    private static string PairingKey(string fileName, string printedKey, ImportProposalBuilder builder, ImportSourceLocation location)
    {
        var fileKey = FilePrefixRegex().Match(Path.GetFileName(fileName ?? string.Empty)) is { Success: true } prefix
            ? prefix.Groups[1].Value
            : null;

        if (fileKey is not null && printedKey is not null && fileKey != printedKey)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                $"The file name says material {fileKey} but the printed number ends in {printedKey}; confirm the RM code.", location);

        var key = fileKey ?? printedKey;
        if (key is null)
            builder.Flag(WorksheetImportFlagCodes.MissingMetadata,
                "No three-digit material number in the file name or the printed Spec./SPC number; set the RM code by hand.", location);
        return key;
    }

    private static string Key(string printed) =>
        printed is not null && LastNumberRegex().Match(printed) is { Success: true } match ? match.Groups[1].Value : null;
}
