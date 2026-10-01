using DOMAIN.Entities.QcWorksheets;
using static APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions.DefinitionParts;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>Physical constants (brief 12): pycnometer weights, replicate readings and their mean, titre values.</summary>
internal static class PhysicalConstantDefinitions
{
    /// <summary>Two readings and their mean.</summary>
    private static RawMaterialTestDefinition Readings(string key, string title, string unit, string[] names, params string[] readingLabels) => new()
    {
        Key = key, Title = title, IdentityTest = true, NamePatterns = names,
        Inputs = [Weight(), Volume(), Preparation(required: false), Reading(title, unit, readingLabels), Observation(required: false)],
        Calculations = [new("result", "Mean", "{mean:reading}", unit, IsResult: true) { Patterns = [Mean] }],
        ResultKey = "result"
    };

    /// <summary>Titre × factor / weight, the numeric factor read from the sheet.</summary>
    private static RawMaterialTestDefinition TitreValue(string key, string title, params string[] names) => new()
    {
        Key = key, Title = title, IdentityTest = true, NamePatterns = names,
        Inputs =
        [
            new("weight", "Weight of sample", WorksheetFieldType.Number, "g") { Patterns = [@"^(wt|weight)\b"] },
            new("titre", "Titre obtained", WorksheetFieldType.Number, "mL") { Patterns = [@"^titre\b"] },
            new("blank_titre", "Blank titre (n2)", WorksheetFieldType.Number, "mL") { Required = false, Patterns = [@"^blank titre\b"] },
            new("factor", "Factor", WorksheetFieldType.Number) { Required = false, Patterns = ["^factor$"] },
            Preparation(required: false), Observation(required: false)
        ],
        Terms =
        [
            new(Difference("n2", "n1"), "({blank_titre} - {titre})", "blank titre (n2 − n1)"),
            new("Titre", "{titre}"),
            new(@"Weight\s+of\s+sample", "{weight}"),
            Number
        ],
        Calculations = [new("result", title, "{titre} * {$factor} / {weight}", null, IsResult: true)],
        ResultKey = "result"
    };

    public static readonly RawMaterialTestDefinition[] All =
    [
        new()
        {
            Key = "relative_density", Title = "Relative Density", IdentityTest = true, PrintsVariableFormula = true,
            NamePatterns = [@"\b(relative density|specific gravity)\b", @"\b(weight|wt) per ml\b"],
            Inputs =
            [
                new("w1", "wt of empty pycnometer (w1)", WorksheetFieldType.Number, "g") { Variable = "w1" },
                new("w2", "wt of pycnometer + water (w2)", WorksheetFieldType.Number, "g") { Variable = "w2" },
                new("w3", "wt of pycnometer + sample (w3)", WorksheetFieldType.Number, "g") { Variable = "w3" },
                new("temperature", "Temp", WorksheetFieldType.Number, "°C") { Required = false, Patterns = ["^temp"] },
                Volume(), Preparation(required: false), Observation(required: false)
            ],
            Calculations =
            [
                new("water_weight", "wt of water (w2 – w1)", "{w2} - {w1}", "g") { Optional = true, Patterns = [@"^(wt|weight) of water\b"] },
                new("sample_weight", "wt of sample (w3 – w1)", "{w3} - {w1}", "g") { Optional = true, Patterns = [@"^(wt|weight) of sample \(w3"] },
                new("result", "Relative density", "({w3} - {w1}) / ({w2} - {w1})", null, IsResult: true) { Patterns = [@"^wt ?/ ?ml\b"] }
            ],
            ResultKey = "result"
        },
        Readings("ph", "pH", null, ["^ph$"], @"^ph( reading| obtained| value)?( ?\(?\d\)?)?$"),
        Readings("refractive_index", "Refractive Index", null, [@"\brefractive index\b"], "^refractive index$"),
        Readings("melting_point", "Melting Point", "°C", [@"\bmelting point\b"], "^melting point$"),
        Readings("water", "Water", "%", [@"^water( content)?( by)?( kf| karl fischer.*)?$"],
            @"^water content\b.*$", @"^observations?$"),
        Readings("conductivity", "Conductivity", null, ["^conductivity$"], "^conductivity$") with
        {
            Inputs =
            [
                Weight(), Volume(), Preparation(required: false),
                Reading("Conductivity", null, "^conductivity$"),
                new("c1", "Conductivity of the solution (C1)") { Required = false, Variable = "c1" },
                new("c2", "Conductivity of the water used (C2)") { Required = false, Variable = "c2" },
                Observation(required: false)
            ],
            Variants =
            [
                // The factor is whatever the sheet prints (0.35, 0.992 …), never the definition's.
                new("water-corrected (C1 − k × C2)", @"C1\s*[–—-]\s*(?<k>\d+(?:\.\d+)?)\s*[x×*]?\s*C2")
                {
                    Formulas = new Dictionary<string, string> { ["result"] = "{c1} - {$k} * {c2}" }
                }
            ]
        },
        new()
        {
            Key = "optical_rotation", Title = "Specific Optical Rotation", IdentityTest = true,
            NamePatterns = [@"\boptical rotation\b"],
            Inputs =
            [
                Weight(), Preparation(required: false),
                new("rotation", "Observed rotation (α)", WorksheetFieldType.Number, "°")
                {
                    Replicates = 2, Required = false,
                    Patterns = [@"^(measurement|determination|reading)s?( ?\d)?$", @"^(angle of rotation|observed rotation)( obtained| measured)?$", @"^optical rotation \( ?\)$"]
                },
                new("path_length", "Path length (l)", WorksheetFieldType.Number, "dm") { Required = false, Patterns = ["^path ?length"] },
                new("concentration", "Concentration (c)", WorksheetFieldType.Number, "g/100 mL") { Required = false, Patterns = ["^conc"] },
                Observation(required: false)
            ],
            Terms =
            [
                new("[αꭤ]", "{mean:rotation}"),
                new(@"conc(?:entration)?\.?", "{concentration}"),
                new(@"[lL]\b", "{path_length}"),
                .. Corrections,
                Number
            ],
            Calculations =
            [
                new("mean", "Mean rotation", "{mean:rotation}", "°") { Optional = true, Patterns = [Mean] },
                new("result", "Specific optical rotation", "{mean:rotation} * 100 / ({path_length} * {concentration})", "°", IsResult: true)
                {
                    Patterns = [@"^(specific )?optical rotation$"]
                }
            ],
            Variants =
            [
                // "Optical Rotation" with readings only: the observed angle is the result.
                new("observed angle only", @"\A(?![\s\S]*specific)(?![\s\S]*calculation)(?![\s\S]*optical rotation\s*=)")
                {
                    Formulas = new Dictionary<string, string> { ["mean"] = null, ["result"] = "{mean:rotation}" }
                },
                new("dried basis (100 − LOD)", @"100\s*[–—-]\s*L\.?\s*O\.?\s*D")
                {
                    Formulas = new Dictionary<string, string>
                    {
                        ["result"] = "{mean:rotation} * 100 * 100 / ({path_length} * {concentration} * (100 - {@lod}))"
                    }
                }
            ],
            ResultKey = "result"
        },
        TitreValue("acid_value", "Acid Value", @"^acid value$"),
        TitreValue("saponification_value", "Saponification Value", @"^saponification value$"),
        TitreValue("iodine_value", "Iodine Value", @"^iodine value$")
    ];
}
