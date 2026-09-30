using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>
/// Matches a Specification test to a worksheet section by name (brief 09): case-, spacing- and
/// punctuation-insensitive, over words, with a small synonym list (Sulphated/Sulfated,
/// Identity/Identification, LOD/Loss on drying, Residue on Ignition/Sulfated Ash) and plurals
/// folded. A sub-test matches on its own name or with its parent ("Identification Tests" + "IR"
/// ↔ "Identity Test: IR"); a section named more broadly than the test also matches
/// ("Description" ↔ "Description / Appearance"), ranked below an exact match.
/// </summary>
public static partial class RawMaterialTestNames
{
    private static readonly (string From, string To)[] Synonyms =
    [
        ("sulphated", "sulfated"), ("identity", "identification"), ("color", "colour"),
        ("residue on ignition", "sulfated ash"), ("l o d", "loss on drying"), ("lod", "loss on drying")
    ];

    private static readonly HashSet<string> StopWords = ["test", "tests", "of", "and", "the", "by", "in", "on"];

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SeparatorRegex();

    /// <summary>The index of the best-matching section, or -1. Among equals, the first not yet bound wins.</summary>
    public static int Match(string testName, string analyte, IReadOnlyList<string> sectionNames, ISet<int> bound)
    {
        var parent = Words(testName);
        var own = analyte is null ? null : Words(analyte);
        var combined = own is null ? parent : Words($"{testName} {analyte}");

        var best = -1;
        var bestScore = 0;
        for (var index = 0; index < sectionNames.Count; index++)
        {
            var section = Words(sectionNames[index]);
            var score = Score(section, combined, own);
            if (score > bestScore || (score == bestScore && score > 0 && bound.Contains(best) && !bound.Contains(index)))
            {
                best = index;
                bestScore = score;
            }
        }

        return best;
    }

    private static int Score(HashSet<string> section, HashSet<string> combined, HashSet<string> own)
    {
        if (section.Count == 0)
            return 0;
        if (section.SetEquals(combined) || (own is not null && section.SetEquals(own)))
            return 3;
        if (combined.IsSubsetOf(section) || (own is not null && own.Any(word => word.Length >= 2) && own.IsSubsetOf(section)))
            return 2;
        if (section.Count >= 2 && section.IsSubsetOf(combined))
            return 1;
        return 0;
    }

    public static HashSet<string> Words(string text)
    {
        var spaced = " " + SeparatorRegex().Replace((text ?? string.Empty).ToLowerInvariant(), " ").Trim() + " ";
        foreach (var (from, to) in Synonyms)
            spaced = spaced.Replace($" {from} ", $" {to} ");

        return spaced.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !StopWords.Contains(word))
            .Select(word => word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss") ? word[..^1] : word)
            .ToHashSet();
    }
}
