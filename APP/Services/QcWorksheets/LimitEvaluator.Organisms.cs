namespace APP.Services.QcWorksheets;

/// <summary>Organism comparison for the absence/presence family of qualitative limits.</summary>
public static partial class LimitEvaluator
{
    /// <summary>
    /// Trailing words that only say "any species of this genus": "Salmonella spp.",
    /// "Salmonella sp." and "Salmonella species" all name the genus Salmonella, and are
    /// treated as the bare genus.
    /// </summary>
    private static readonly string[] GenusMarkers = ["spp", "sp", "species"];

    /// <summary>
    /// The organism an absence/presence phrase names, normalized — or empty when the text names
    /// none (a bare "Absent" / "Detected", or anything outside the family).
    /// <list type="number">
    ///   <item><description>The same normalization as <see cref="NormalizeQualitative"/>: lowercase, every run of non-alphanumeric characters to one space, trailing "in"/"per"/"from" clause dropped. So "E. coli", "E.coli" and "E coli" are all <c>e coli</c>.</description></item>
    ///   <item><description>The text after "absence of " / "presence of " is the organism.</description></item>
    ///   <item><description>Trailing genus markers ("spp", "sp", "species") are dropped, so "Salmonella spp." equals "Salmonella". A named species ("Salmonella typhi") stays distinct from the genus.</description></item>
    /// </list>
    /// No abbreviation is expanded: "Escherichia coli" and "E. coli" are different strings, and
    /// differing names are a breach rather than a guess.
    /// </summary>
    internal static string OrganismOf(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = StripContextClause(Collapse(text));

        foreach (var prefix in (string[])["absence of ", "presence of "])
        {
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var words = normalized[prefix.Length..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            while (words.Count > 1 && GenusMarkers.Contains(words[^1]))
                words.RemoveAt(words.Count - 1);

            return string.Join(' ', words);
        }

        return string.Empty;
    }
}
