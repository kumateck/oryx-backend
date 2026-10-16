using DOMAIN.Entities.QcWorksheets;
using static APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions.DefinitionParts;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>Observation and limit tests (brief 12): no calculation, the analyst records what was seen.</summary>
internal static class ObservationDefinitions
{
    private static RawMaterialTestDefinition Observed(string key, string title, params string[] names) => new()
    {
        Key = key, Title = title, NamePatterns = names,
        Inputs = [Observation("result", fromEmptyCell: true)],
        ResultKey = "result"
    };

    /// <summary>Weight, preparation, observation.</summary>
    private static RawMaterialTestDefinition Prepared(string key, string title, params string[] names) => new()
    {
        Key = key, Title = title, NamePatterns = names,
        Inputs = [Weight(), Volume(), Preparation(), Observation(), Inference()],
        ResultKey = "observation"
    };

    /// <summary>Test solution, reference solution, observation, inference (Complies / Does not comply).</summary>
    private static RawMaterialTestDefinition Limit(string key, string title, params string[] names) => new()
    {
        Key = key, Title = title, NamePatterns = names,
        Inputs =
        [
            Weight(), Volume(),
            new("test_solution", "Test solution", WorksheetFieldType.LongText)
            {
                Patterns = [@"^test solution( [a-z])?$", @"^(sample |solution )?prep[ae]r[ae]tions?$"]
            },
            new("reference_solution", "Reference solution", WorksheetFieldType.LongText)
            {
                Patterns = [@"^reference solution( used)?( [a-z])?$"]
            },
            Observation(),
            Inference(required: true, choice: true)
        ],
        ResultKey = "inference"
    };

    public static readonly RawMaterialTestDefinition[] All =
    [
        Observed("description", "Description", @"^description( appearance)?$", "^appearance$"),
        Observed("odour", "Odour", @"^odou?r( taste)?$"),
        Observed("taste", "Taste", "^taste$"),
        new()
        {
            Key = "solubility", Title = "Solubility", NamePatterns = ["^solubility$"],
            Inputs =
            [
                new("solvent", "Solvent", WorksheetFieldType.ShortText) { Required = false, KeyFromLabel = true, Patterns = [".+"] }
            ]
        },
        Prepared("appearance_of_solution", "Appearance of Solution", @"^(appearance|colou?r|clarity) of solution\b"),
        Prepared("acidity_alkalinity", "Acidity or Alkalinity", @"^acidity( or)?( alkalinity)?$", "^alkalinity$"),
        Prepared("reducing_sugars", "Reducing Sugars", @"reducing sugars$"),
        Limit("heavy_metals", "Heavy Metals", "^heavy metals$"),
        Limit("chlorides", "Chlorides", "^chlorides?$"),
        Limit("sulfates", "Sulfates", @"^sul(f|ph)ates?$"),
        Limit("iron", "Iron", "^iron$"),
        new()
        {
            Key = "identity_ir", Title = "Identity by IR", IdentityTest = true, NamePatterns = [@"\b(ft)?ir\b"],
            Inputs =
            [
                new("balance", "Balance ID", WorksheetFieldType.Instrument) { Patterns = ["balance"] },
                new("kbr_weight", "Weight of KBr", WorksheetFieldType.Number, "g") { Patterns = [@"^(wt|weight)\b.*\bkbr\b"] },
                Weight(required: true) with { Key = "sample_weight", Label = "Weight of sample" },
                new("spectrum", "Attach Print Out", WorksheetFieldType.FileUpload) { Patterns = [@"^attach\b"] },
                Preparation(required: false),
                Observation(),
                Inference()
            ],
            ResultKey = "observation"
        },
        new()
        {
            // Identity by colour or reaction: the catch-all for an "Identity Test" with no named technique.
            Key = "identity_reaction", Title = "Identity by colour or reaction", IdentityTest = true,
            NamePatterns = ["^$", "^[a-f]$", @"\b(colou?r|reactions?|tests?|precipitate)\b"],
            Inputs = [Weight(), Volume(), Preparation(), Observation(), Inference()],
            ResultKey = "observation"
        }
    ];
}
