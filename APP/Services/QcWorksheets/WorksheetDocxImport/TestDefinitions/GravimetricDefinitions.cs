using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>
/// Crucible weighings W1 (empty), W2 (with sample, before), W3 (after). A loss is
/// (W2 − W3) / (W2 − W1) × 100; a residue is (W3 − W1) / (W2 − W1) × 100. The sheet's own printed
/// arrangement is compared with this and wins where it differs.
/// </summary>
internal static class GravimetricDefinitions
{
    public const string Loss = "(({w2} - {w3}) * 100) / ({w2} - {w1})";
    public const string Residue = "(({w3} - {w1}) * 100) / ({w2} - {w1})";

    private static RawMaterialTestDefinition Crucible(string key, string title, string formula, params string[] names) => new()
    {
        Key = key, Title = title, NamePatterns = names, PrintsVariableFormula = true,
        Inputs =
        [
            new("w1", "Weight of empty crucible (W1)", WorksheetFieldType.Number, "g") { Variable = "w1" },
            new("w2", "Weight of crucible + sample before drying (W2)", WorksheetFieldType.Number, "g") { Variable = "w2" },
            new("w3", "Weight of crucible + sample after drying (W3)", WorksheetFieldType.Number, "g") { Variable = "w3" }
        ],
        Calculations = [new("result", title, formula, "%", IsResult: true)],
        ResultKey = "result"
    };

    public static readonly RawMaterialTestDefinition[] All =
    [
        Crucible("loss_on_drying", "Loss on Drying", Loss, "^loss on drying$", @"^l ?o ?d$"),
        Crucible("loss_on_ignition", "Loss on Ignition", Loss, "^loss on ignition$", @"^l ?o ?i$"),
        Crucible("sulfated_ash", "Sulfated Ash", Residue, @"^sul(f|ph)ated ash$", "^residue on ignition$"),
        Crucible("total_ash", "Total Ash", Residue, "^total ash$")
    ];
}
