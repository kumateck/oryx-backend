using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>Inputs and printed-formula terms that several definitions share.</summary>
internal static class DefinitionParts
{
    public const string Mean = @"^(mean|average|avg)\b";
    private const string Dash = @"\s*[–—-]\s*";

    public static readonly string[] Compliance = ["Complies", "Does not comply"];

    public static DefinitionInput Weight(bool required = false) =>
        new("weight", "Weight of sample taken", WorksheetFieldType.Number, "g")
        {
            Required = required, Patterns = [@"^(wt|weight)\b(?!.*\b(kbr|std|standard)\b)"]
        };

    public static DefinitionInput Volume() =>
        new("volume", "Volume taken", WorksheetFieldType.Number, "mL") { Required = false, Patterns = [@"^vol(ume)?\b"] };

    public static DefinitionInput Preparation(bool required = true) =>
        new("preparation", "Preparation", WorksheetFieldType.LongText)
        {
            Required = required, Patterns = [@"^(solution s\d? |sample |solution )?prep[ae]r[ae]tions?$"]
        };

    public static DefinitionInput Observation(string key = "observation", bool required = true, bool fromEmptyCell = false) =>
        new(key, "Observation", WorksheetFieldType.LongText)
        {
            Required = required, FromEmptyCell = fromEmptyCell, Patterns = [@"^ob\w*ervations?(\(s\))?$"]
        };

    public static DefinitionInput Inference(bool required = false, bool choice = false) =>
        new("inference", "Inference", choice ? WorksheetFieldType.Select : WorksheetFieldType.LongText)
        {
            Required = required, Options = choice ? Compliance : null, Patterns = ["^inference$"]
        };

    /// <summary>Two readings, found under any of the labels the sheets use for them.</summary>
    public static DefinitionInput Reading(string label, string unit, params string[] ownPatterns) =>
        new("reading", label, WorksheetFieldType.Number, unit)
        {
            Replicates = 2, Required = false,
            Patterns = [@"^(measurement|determination|result|reading)s?( ?\d)?$", .. ownPatterns]
        };

    /// <summary>"(100 – LOD)", "(100 – Water)", "(100 – L.O.I)": the correction refers to this worksheet's own test.</summary>
    public static readonly DefinitionTerm[] Corrections =
    [
        new(@"\(\s*100" + Dash + @"L\.?\s*o\.?\s*D\.?\s*\)", "(100 - {@lod})", "dried basis (100 − LOD)"),
        new(@"\(\s*100" + Dash + @"Water\s*\)", "(100 - {@water})", "anhydrous basis (100 − Water)"),
        new(@"\(\s*100" + Dash + @"L\.?\s*O\.?\s*I\.?\s*\)", "(100 - {@loi})", "ignited basis (100 − LOI)")
    ];

    public static readonly DefinitionTerm Number = new(@"\d+(?:\.\d+)?", "$0");

    public static string Difference(string left, string right) => @"\(\s*" + left + Dash + right + @"\s*\)";
}
