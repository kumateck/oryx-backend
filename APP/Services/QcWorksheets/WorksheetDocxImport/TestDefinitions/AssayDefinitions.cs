using DOMAIN.Entities.QcWorksheets;
using static APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions.DefinitionParts;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>
/// Assays (brief 12). The sheets print the formula as a stacked fraction; it is read term by term
/// against the definition's terms, so back titration, the drying correction and any extra factor
/// are exactly what the sheet prints. With nothing printed the definition's base formula is used.
/// </summary>
internal static class AssayDefinitions
{
    private const string Samples = @"^(wt|weight)\.? of (spl|sample)\b|^(spl|sample) (wt|weight)\b";
    private const string Standard = @"^(wt|weight)\.? of st(d|andard)\b|^st(d|andard) (wt|weight)\b";

    /// <summary>"% Assay(i) = x x x =": the blank worked sum of one determination.</summary>
    private const string OwnBlank = @"^(% ?)?(assay|content)\b";

    private static readonly DefinitionConstant Wavelength = new("wavelength", "Wavelength (λ)", @"λ\s*=\s*(?<value>\d+(?:\.\d+)?)\s*nm", "nm");

    private static readonly DefinitionInput Dilution =
        new("dilution", "Dilution", WorksheetFieldType.ShortText) { Required = false, Patterns = [@"^dilution( solution)?( [a-z])?$"] };

    private static readonly DefinitionInput[] Factors =
    [
        new("dilution_factor", "Dilution factor") { Required = false, Patterns = [@"^dil(ution)?\.? factor$"] },
        new("purity", "% Purity of standard", WorksheetFieldType.Number, "%") { Required = false, Patterns = [@"purity"] },
        new("filled_weight", "Filled weight") { Required = false, Patterns = [@"^filled (wt|weight)"] },
        new("claim", "Claim") { Required = false, Patterns = ["^claim$"] }
    ];

    private static readonly DefinitionTerm[] FactorTerms =
    [
        new(@"Dil(?:ution)?\.?\s+(?:factor|formula)", "{dilution_factor}", "dilution factor"),
        new(@"%\s*purity(?:\s+of\s+(?:the\s+)?(?:std|standard))?|purity\s+of\s+(?:std|standard)", "{purity}", "% purity"),
        new(@"Filled\s+w(?:eigh)?t", "{filled_weight}", "filled weight"),
        new("Claim", "{claim}", "claim")
    ];

    public static readonly RawMaterialTestDefinition Titration = new()
    {
        Key = "assay_titration", Title = "Assay by titration", Shape = DefinitionShape.Titration,
        NamePatterns = [@"^assay\b.*\btitr"],
        Inputs =
        [
            new("factor", "Factor of volumetric solution") { Patterns = [@"^factor\b"] },
            new("equivalence_value", "Equivalence (mg per mL)", WorksheetFieldType.Number, "mg") { Required = false, Patterns = ["^equivalence$"] },
            new("blank_titre", "Blank titre", WorksheetFieldType.Number, "mL") { Required = false, Patterns = [@"^blank titre\b"] },
            new("wt_per_ml", "Weight per mL", WorksheetFieldType.Number, "g/mL") { Required = false, Patterns = [@"^(wt|weight) ?(/|per) ?ml"] },
            Dilution, .. Factors
        ],
        Constants =
        [
            new("equivalence", "Equivalence",
                @"Equivalence\s*[:=]\s*(?<text>\S.*?(?:equivalent\s+to|≡)\s*(?<value>\d+(?:\.\d+)?)\s*(?<unit>mg|g)\b.*)$"),
            new("equivalence", "Equivalence",
                @"(?<text>(?:each\s+|1\s*)m[lL]\s+of\b.*?(?:equivalent\s+to|≡)\s*(?<value>\d+(?:\.\d+)?)\s*(?<unit>mg|g)\b.*)$"),
            new("equivalence", "Equivalence", @"Equivalence\s*[:=]\s*(?<text>\S*[A-Za-z]{2}.*)$")
        ],
        Table = new("titration", "Titration", "Determination", ["Sample 1", "Sample 2"],
        [
            new("wtTaken", "Wt. taken", WorksheetFieldType.Number, "mg"),
            new("finalVolume", "Final volume", WorksheetFieldType.Number, "mL"),
            new("initialVolume", "Initial volume", WorksheetFieldType.Number, "mL"),
            new("titreObtained", "Titre obtained", WorksheetFieldType.Number, "mL", "{finalVolume} - {initialVolume}")
        ]),
        Terms =
        [
            new(Difference(@"Sample\s+Titre", @"Blank\s+Titre"), "({row:titre} - {blank_titre})"),
            new(Difference(@"Blank\s+Titre", @"Sample\s+Titre"), "({blank_titre} - {row:titre})", "back titration"),
            new(@"Sample\s+Titre", "{row:titre}", "no blank"),
            .. FactorTerms,
            new("Factor", "{factor}"),
            new(@"Equiv(?:alence)?\.?", "{$equivalence}"),
            new(@"W(?:eigh)?t\s*/\s*ml", "{wt_per_ml}", "weight per mL"),
            new(@"Weight\s+of\s+sample", "{row:weight}"),
            .. Corrections,
            Number
        ],
        Calculations =
        [
            new("assay", "% Assay", "({row:titre} - {blank_titre}) * {factor} * {$equivalence} * 100 / {row:weight}", "%") { Patterns = [OwnBlank] },
            new("result", "Average % Assay", "AVG", "%", IsResult: true) { Patterns = [@"^average\b"] }
        ],
        ResultKey = "result"
    };

    private static readonly DefinitionTerm[] ReadingTerms =
    [
        new(@"(?:avg\.?\s+)?spl\s+(?:peak|area|abs(?:orbance)?)", "{s:reading}"),
        new(@"(?:avg\.?\s+)?std\s+(?:peak|area|abs(?:orbance)?)", "{std_reading}"),
        new(@"conc\.?\s+std|std\s+conc\.?", "{std_conc}"),
        new(@"conc\.?\s+spl|spl\s+conc\.?", "{s:spl_conc}"),
        new(@"dil(?:ution)?\.?\s+factor", "{dilution_factor}"),
        FactorTerms[1] with { Variant = null }, .. FactorTerms.Skip(2),
        new(@"A\s*[¦|]|A\s*\(\s*1\s*%\s*,\s*1\s*cm\s*\)", "{$a11}", "specific absorbance A(1%, 1cm)"),
        new(@"(?:spl|sample)\s+w(?:eigh)?t(?:\s+taken)?|w(?:eigh)?t\s+of\s+(?:spl|sample)(?:\s+taken)?", "{s:spl_weight}"),
        new(@"(?:std|standard)\s+w(?:eigh)?t", "{std_weight}"),
        new("Factor", "{factor}", "factor"),
        .. Corrections,
        Number
    ];

    private static readonly DefinitionInput[] ReadingInputs =
    [
        new("std_conc", "Standard concentration") { Required = false, Patterns = [@"^(std|standard) conc", @"^conc\w* (of )?(std|standard)"] },
        new("spl_conc", "Sample concentration") { Required = false, Replicates = 2, Patterns = [@"^(spl|sample) conc", @"^conc\w* (of )?(spl|sample)", "^conc$"] },
        new("std_reading", "Standard reading") { Required = false },
        new("factor", "Factor") { Required = false, Patterns = ["^factor$"] },
        Dilution, .. Factors
    ];

    private static DefinitionTable ReadingsTable(string key, string label, string rowHeader, string[] rows, bool standard) =>
        new(key, label, rowHeader, rows,
        [
            .. standard ? new[] { new DefinitionColumn("standard", "Standard") } : [],
            new DefinitionColumn("sample1", "Sample 1"), new DefinitionColumn("sample2", "Sample 2")
        ]);

    public static readonly RawMaterialTestDefinition Hplc = new()
    {
        Key = "assay_hplc", Title = "Assay by HPLC", Shape = DefinitionShape.Readings, HasConditions = true,
        NamePatterns = [@"^assay\b.*\bhplc\b"],
        Inputs =
        [
            new("std_weight", "Weight of standard taken", WorksheetFieldType.Number, "g") { Patterns = [Standard] },
            new("spl_weight", "Weight of sample taken", WorksheetFieldType.Number, "g") { Replicates = 2, Patterns = [Samples] },
            Dilution with { Required = true },
            .. ReadingInputs.Select(input => input.Key == "purity" ? input with { Required = true } : input).Where(input => input.Key != "dilution")
        ],
        Constants = [Wavelength],
        Table = ReadingsTable("peak_areas", "Peak areas", "Injection", ["1", "2", "3", "4", "5"], standard: true),
        Terms = ReadingTerms,
        Calculations =
        [
            new("assay", "% Assay", "{s:reading} / {std_reading} * {std_conc} / {s:spl_conc} * {purity}", "%") { Patterns = [OwnBlank] },
            new("result", "Average % Assay", "AVG", "%", IsResult: true) { Patterns = [@"^average\b"] }
        ],
        ResultKey = "result"
    };

    public static readonly RawMaterialTestDefinition Uv = new()
    {
        Key = "assay_uv", Title = "Assay by UV", Shape = DefinitionShape.Readings, HasConditions = true,
        NamePatterns = [@"^assay\b.*\buv\b"],
        Inputs =
        [
            new("spl_weight", "Weight of sample taken", WorksheetFieldType.Number, "g") { Replicates = 2, Patterns = [Samples] },
            new("std_weight", "Weight of standard taken", WorksheetFieldType.Number, "g") { Required = false, Patterns = [Standard] },
            Dilution with { Required = true },
            .. ReadingInputs.Where(input => input.Key != "dilution")
        ],
        Constants =
        [
            new("a11", "A(1%, 1cm)", @"A\s*[¦|]\s*=\s*(?<value>\d+(?:\.\d+)?)"),
            Wavelength
        ],
        Table = ReadingsTable("absorbance", "Absorbance", "Reading", ["1", "2"], standard: false),
        Terms = ReadingTerms,
        Calculations =
        [
            // Specific absorbance when the sheet prints A(1%, 1cm); against a standard otherwise.
            new("assay", "% Assay", "{s:reading} * {dilution_factor} * 100 / ({$a11} * {s:spl_weight})", "%") { Patterns = [OwnBlank] },
            new("result", "Average % Assay", "AVG", "%", IsResult: true) { Patterns = [@"^average\b"] }
        ],
        Variants =
        [
            new("against a standard", @"\A(?![\s\S]*A\s*[¦|])")
            {
                Formulas = new Dictionary<string, string> { ["assay"] = "{s:reading} / {std_reading} * {std_conc} / {s:spl_conc} * {purity}" }
            }
        ],
        ResultKey = "result"
    };

    public static readonly RawMaterialTestDefinition[] All = [Titration, Hplc, Uv];
}
