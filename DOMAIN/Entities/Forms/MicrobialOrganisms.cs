namespace DOMAIN.Entities.Forms;

/// <summary>The specified microorganisms QC tests for. Codes are stored; names are printed.</summary>
public static class MicrobialOrganisms
{
    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        ["escherichia-coli"] = "Escherichia coli",
        ["salmonella"] = "Salmonella",
        ["staphylococcus-aureus"] = "Staphylococcus aureus",
        ["pseudomonas-aeruginosa"] = "Pseudomonas aeruginosa",
        ["candida-albicans"] = "Candida albicans",
        ["bile-tolerant-gram-negative-bacteria"] = "Bile-tolerant gram-negative bacteria",
        ["clostridia"] = "Clostridia",
    };

    public static bool IsKnown(string code) => code is not null && Names.ContainsKey(code);
}

/// <summary>The units a CFU result is reported in.</summary>
public static class MicrobialUnits
{
    public static readonly IReadOnlyList<string> Cfu = ["cfu/g", "cfu/mL", "cfu/plate"];
    public static readonly IReadOnlyList<string> Quantity = ["g", "mL"];
}
