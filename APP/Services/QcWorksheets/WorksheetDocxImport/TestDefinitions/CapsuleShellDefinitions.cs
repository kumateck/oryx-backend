using DOMAIN.Entities.QcWorksheets;
using static APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions.DefinitionParts;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>Empty capsule shells (brief 12): colours and printing observed, disintegration timed, 20 shells weighed.</summary>
internal static class CapsuleShellDefinitions
{
    private static RawMaterialTestDefinition Observed(string key, string title, params string[] names) => new()
    {
        Key = key, Title = title, NamePatterns = names,
        Inputs = [Observation("result", fromEmptyCell: true)],
        ResultKey = "result"
    };

    public static readonly RawMaterialTestDefinition[] All =
    [
        Observed("cap_colour", "Cap Colour", @"^cap colou?r$"),
        Observed("body_colour", "Body Colour", @"^body colou?r$"),
        Observed("printing_details", "Printing Details", @"^printing details?$") with
        {
            Inputs =
            [
                new("cap", "Cap", WorksheetFieldType.ShortText) { Required = false, Patterns = ["^cap$"] },
                new("body", "Body", WorksheetFieldType.ShortText) { Required = false, Patterns = ["^body$"] },
                Observation("result", fromEmptyCell: true)
            ]
        },
        new()
        {
            Key = "disintegration_time", Title = "Disintegration Time", NamePatterns = [@"^disintegration( time)?$"],
            Inputs =
            [
                new("result", "Disintegration time", WorksheetFieldType.Number, "min")
                {
                    FromEmptyCell = true, Patterns = [@"^(disintegration )?time\b", @"^ob\w*ervations?$"]
                }
            ],
            ResultKey = "result"
        },
        new()
        {
            Key = "average_weight", Title = "Average Weight", Shape = DefinitionShape.Weighings, NamePatterns = ["^average weight$"],
            Table = new("individual_weights", "Individual weights", "Shell",
                Enumerable.Range(1, 20).Select(number => number.ToString("00")).ToList(),
                [new("weight", "Weight", WorksheetFieldType.Number, "mg")]),
            Calculations =
            [
                new("total", "Weight of 20 Shells", "SUM", "mg") { Patterns = [@"^(total )?weight of \d* ?shells?\b"] },
                new("result", "Average Weight of Shell", "AVG", "mg", IsResult: true) { Patterns = [@"^average weight\b"] }
            ],
            ResultKey = "result"
        }
    ];
}
