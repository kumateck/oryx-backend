using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One "X / Y" choice found in a sentence.</summary>
public sealed record ChoiceMatch(string Topic, WorksheetFieldType Type, IReadOnlyList<string> Options, int Index, int Length);

/// <summary>
/// The curated dictionary of every choice phrase in the corpus. "/" is ambiguous — it also
/// separates codes (QCD/EQT), units (cfu/mL), drug names (Ampicillin/Sulbactam) and labels
/// (Room/Area) — so only these phrases become choices; anything else with a slash is text.
/// </summary>
public static class ChoicePhrases
{
    private sealed record Entry(string Topic, WorksheetFieldType Type, Regex Pattern, Func<Match, string[]> Options);

    private const RegexOptions Flags = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    private static readonly Entry[] Entries =
    [
        new("Compliance", WorksheetFieldType.Select,
            new Regex(@"\bcomplies\s*/\s*does\s+not\s+comply\b", Flags), _ => ["Complies", "Does not comply"]),
        new("Compliance", WorksheetFieldType.Select,
            new Regex(@"\bcomply\s*/\s*do\s+not\s+comply\b", Flags), _ => ["Comply", "Do not comply"]),
        new("Presence", WorksheetFieldType.GrowthObservation,
            new Regex(@"\babsent\s*/\s*detected\b", Flags), _ => ["Absent", "Detected"]),
        new("Presence", WorksheetFieldType.Select,
            new Regex(@"\bpresence\s+of\s+(?<o>[^/]+?)\s*/\s*absence\s+of\s+\k<o>", Flags),
            match => [$"Presence of {match.Groups["o"].Value.Trim()}", $"Absence of {match.Groups["o"].Value.Trim()}"]),
        new("Growth", WorksheetFieldType.GrowthObservation,
            new Regex(@"\bthere\s+was\s*/\s*was\s+no\b(\s+clearly\s+visible)?\s+growth", Flags), _ => ["Growth", "No Growth"]),
        new("Inhibition", WorksheetFieldType.Select,
            new Regex(@"\bdid\s*/\s*did\s+not\s+inhibit\b", Flags), _ => ["Did inhibit", "Did not inhibit"]),
        new("Factor 2", WorksheetFieldType.Select,
            new Regex(@"\bwas\s*/\s*was\s+not\s+more\s+than\s+(a\s+)?factor\s*2\b", Flags),
            _ => ["Was more than factor 2", "Was not more than factor 2"]),
        new("Green pigmentation", WorksheetFieldType.Select,
            new Regex(@"\bdid\s*/\s*did\s+not\s+produce\s+green\s+pigmentation\b", Flags),
            _ => ["Did produce green pigmentation", "Did not produce green pigmentation"])
    ];

    /// <summary>Every dictionary phrase in <paramref name="text"/>, in reading order, never overlapping.</summary>
    public static IReadOnlyList<ChoiceMatch> Find(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var found = new List<ChoiceMatch>();
        foreach (var entry in Entries)
        {
            foreach (Match match in entry.Pattern.Matches(text))
            {
                if (found.Any(other => match.Index < other.Index + other.Length && other.Index < match.Index + match.Length))
                    continue;
                found.Add(new ChoiceMatch(entry.Topic, entry.Type, entry.Options(match), match.Index, match.Length));
            }
        }

        return found.OrderBy(match => match.Index).ToList();
    }
}
