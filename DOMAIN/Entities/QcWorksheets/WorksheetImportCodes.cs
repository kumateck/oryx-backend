using SHARED;

namespace DOMAIN.Entities.QcWorksheets;

// Enums, flag codes and errors of the worksheet DOCX import (build brief 07, Phase B).

/// <summary>The document family an ARD worksheet belongs to.</summary>
public enum ArdFamily
{
    Unknown = 0,
    ProductMicro = 1,
    CultureMedia = 2,
    EnvironmentalMonitoring = 3,
    PurifiedWater = 4,
    CompletedCertificate = 5,

    /// <summary>"RAW MATERIAL ANALYTICAL WORKSHEET" (brief 09): a chemical template, code RM-NNN.</summary>
    RawMaterialChemical = 6,

    /// <summary>A raw-material Specification document ("SPC No.: QCD/SPC/RM/NNN"): proposals only, no template.</summary>
    RawMaterialSpecification = 7
}

/// <summary>Where a raw-material Specification document found the worksheet its characteristics bind to.</summary>
public enum RawMaterialPairingStatus
{
    /// <summary>No worksheet NNN in this upload and no saved RM-NNN template: import the worksheet first.</summary>
    Missing = 0,

    /// <summary>Bound to the worksheet proposal in the same upload; post after that worksheet is saved.</summary>
    InUpload = 1,

    /// <summary>Bound to an already-saved RM-NNN template, which is not modified.</summary>
    ExistingTemplate = 2
}

public enum ImportConfidence
{
    High = 0,
    Medium = 1,
    Low = 2
}

public static class WorksheetImportErrors
{
    public static readonly Error NoFiles =
        Error.Validation("QcWorksheetTemplate.ImportNoFiles", "At least one .docx file is required");

    /// <summary>
    /// Build brief 10: the AI extractor's response did not validate against the expected
    /// schema, or proposed a field whose <c>sourceQuote</c> is not verbatim in the redacted
    /// document. Refused outright — never partially accepted.
    /// </summary>
    public static readonly Error AiResponseUngrounded = Error.Validation(
        "QcWorksheetTemplate.AiResponseUngrounded",
        "The AI extractor's response could not be grounded in the document and was refused.");

    /// <summary>
    /// Build brief 10: <c>CanUseAiWorksheetExtraction</c> is held but no key is configured for
    /// the active provider. Build brief 11 also returns this from
    /// <c>IAiExtractionSettingsService.SetActiveProviderAsync</c> when the provider being
    /// activated has no key saved yet — you can't activate a provider you haven't configured.
    /// </summary>
    public static readonly Error AiExtractionUnavailable = Error.Validation(
        "QcWorksheetTemplate.AiExtractionUnavailable",
        "AI extraction is not available: no API key is configured for the active provider.");

    public static readonly Error AiKeyUnreadable = Error.Failure(
        "QcWorksheetTemplate.AiKeyUnreadable",
        "The saved AI provider key cannot be decrypted. Ask an administrator to save it again.");

    public static readonly Error AiProviderRejectedRequest = Error.Failure(
        "QcWorksheetTemplate.AiProviderRejectedRequest",
        "The AI provider rejected the extraction request. Check the configured key, model, and provider logs.");

    public static readonly Error AiProviderRateLimited = Error.Failure(
        "QcWorksheetTemplate.AiProviderRateLimited",
        "The AI provider rate limited the extraction request. Try again later.");

    public static readonly Error AiProviderUnreachable = Error.Failure(
        "QcWorksheetTemplate.AiProviderUnreachable",
        "The AI provider could not be reached or is unavailable. Try again later.");

    /// <summary>Build brief 11: <c>SaveProviderKeyAsync</c> rejects an empty/whitespace key.</summary>
    public static readonly Error AiExtractionKeyRequired = Error.Validation(
        "QcWorksheetTemplate.AiExtractionKeyRequired",
        "An API key is required.");
}

/// <summary>Stable flag codes a reviewer (and the Phase C screen) can key on.</summary>
public static class WorksheetImportFlagCodes
{
    public const string InvalidFile = "InvalidFile";
    public const string CompletedOutputNotTemplate = "CompletedOutputNotTemplate";
    public const string UnknownFamily = "UnknownFamily";
    public const string RecognizerPending = "RecognizerPending";
    /// <summary>Older media form with no newer twin: importable, with a warning (e.g. EEBM).</summary>
    public const string SupersededFormat = "SupersededFormat";

    /// <summary>
    /// Older media form whose newer twin is in the same upload or already saved as a
    /// non-superseded MediaQualification template. The review screen refuses to save it.
    /// </summary>
    public const string SupersededFormatBlocked = "SupersededFormatBlocked";
    public const string UnmatchedEquipment = "UnmatchedEquipment";
    public const string UnmatchedReagent = "UnmatchedReagent";
    public const string MediaTemplateMissing = "MediaTemplateMissing";
    public const string MixedColumn = "MixedColumn";
    public const string IncompleteValue = "IncompleteValue";
    public const string SuspectedRunData = "SuspectedRunData";
    public const string UnrecognizedContent = "UnrecognizedContent";
    public const string MissingMetadata = "MissingMetadata";

    /// <summary>This file's shared template is proposed on another file of the same upload.</summary>
    public const string SharedTemplateInBatch = "SharedTemplateInBatch";

    /// <summary>This file's shared template is already saved; only its points and specifications are proposed.</summary>
    public const string SharedTemplateExists = "SharedTemplateExists";

    /// <summary>A sampling point that no printed limit tier covers (or that a tier lists twice).</summary>
    public const string SamplingPointWithoutLimit = "SamplingPointWithoutLimit";

    /// <summary>
    /// Brief 09: a Specification test with no worksheet section got an added section (Result +
    /// "Attach print out") in the worksheet proposal of the same upload. The reviewer may remove it.
    /// </summary>
    public const string AddedForSpecification = "AddedForSpecification";

    /// <summary>A calculated field whose formula was read from the printed formula; the reviewer confirms it.</summary>
    public const string FormulaFromPrint = "FormulaFromPrint";

    /// <summary>A calculated field left with an empty formula (titration/HPLC assay, "Calculation:" blank); never guessed.</summary>
    public const string FormulaNeedsReview = "FormulaNeedsReview";

    /// <summary>A Specification characteristic that matches no field of the already-saved RM-NNN template.</summary>
    public const string FieldNotOnTemplate = "FieldNotOnTemplate";

    /// <summary>A Specification document with no worksheet NNN in the upload and no saved RM-NNN template.</summary>
    public const string WorksheetNotFound = "WorksheetNotFound";

    /// <summary>
    /// Build brief 10: any field of this proposal came from the AI fallback extractor, not a
    /// deterministic recognizer. A distinct, non-dismissable banner — never folded into the
    /// ordinary confidence styling.
    /// </summary>
    public const string AiExtracted = "AiExtracted";

    /// <summary>
    /// Build brief 10: the AI extractor recognized a run-data label or choice phrase not in the
    /// curated dictionaries and is confident it is one. The suggestion is attached to this
    /// flag's own text, informational only — never applied automatically.
    /// </summary>
    public const string AiDictionarySuggestion = "AiDictionarySuggestion";

    /// <summary>
    /// A run-data label was found but its value could not be confidently redacted before the AI
    /// path would have sent it. The document is refused rather than sent unredacted.
    /// </summary>
    public const string RedactionRefused = "RedactionRefused";
}
