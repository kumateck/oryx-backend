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
    CompletedCertificate = 5
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
}
