using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>Text clean-up shared by the reader and every recognizer.</summary>
public static partial class ImportText
{
    /// <summary>
    /// Words the corpus hyphenates across lines ("Pseudo-monas", "Incu- bation"). Only these are
    /// rejoined: a generic rule would also destroy real hyphens ("Soyabean-Casein", "free-flowing").
    /// </summary>
    private static readonly HashSet<string> KnownWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pseudomonas", "aeruginosa", "Escherichia", "Staphylococcus", "Salmonella", "enterica",
        "Typhimurium", "Candida", "albicans", "Aspergillus", "brasiliensis", "Enterococcus",
        "faecalis", "Bacillus", "subtilis", "spizizenii", "Clostridium", "sporogenes",
        "Burkholderia", "cepacia", "Penicillium", "chrysogenum", "Incubation", "Inoculum", "Pigmentation", "Colonies",
        "temperature", "microorganisms", "Microbiology", "Specification", "Environmental",
        "Monitoring", "Enumeration", "Sterility", "Dehydrated"
    };

    [GeneratedRegex(@"[\s\u00A0\u2007\u202F]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(\p{L}+)-\s*(\p{L}+)")]
    private static partial Regex HyphenSplitRegex();

    [GeneratedRegex(@"(…|\.{3,}|_{3,}|-{4,})+")]
    private static partial Regex LeaderRegex();

    // "QCD/ STR/P1-01" and "CFU/ 0.1mL": a code or unit split over two paragraphs of one cell.
    [GeneratedRegex(@"(?<=\b[A-Z]{2,}/)\s+(?=[A-Z0-9])")]
    private static partial Regex SplitCodeRegex();

    [GeneratedRegex(@"Page\s*\d+\s*of\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex PageOfRegex();

    [GeneratedRegex(@"^[\s\p{P}\p{S}]*$")]
    private static partial Regex PunctuationOnlyRegex();

    public static string Normalize(string text) =>
        string.IsNullOrEmpty(text) ? string.Empty : WhitespaceRegex().Replace(text, " ").Trim();

    /// <summary>Rejoins a word split by a hyphen only when the joined word is a known one.</summary>
    public static string Dehyphenate(string text) =>
        string.IsNullOrEmpty(text)
            ? string.Empty
            : HyphenSplitRegex().Replace(text, match =>
            {
                var joined = match.Groups[1].Value + match.Groups[2].Value;
                return KnownWords.Contains(joined) ? joined : match.Value;
            });

    public static string Clean(string text) => SplitCodeRegex().Replace(Normalize(Dehyphenate(Normalize(text))), string.Empty);

    public static string StripPageNumbers(string text) => Normalize(PageOfRegex().Replace(text ?? string.Empty, " "));

    /// <summary>A paragraph that is only brackets or punctuation ("[", "[[", "`"), i.e. noise.</summary>
    public static bool IsNoise(string text) => PunctuationOnlyRegex().IsMatch(text ?? string.Empty);

    /// <summary>True when the text contains a dotted/underscored fill-in leader.</summary>
    public static bool HasLeader(string text) => !string.IsNullOrEmpty(text) && LeaderRegex().IsMatch(text);

    public static string StripLeaders(string text) => Normalize(LeaderRegex().Replace(text ?? string.Empty, " "));

    /// <summary>Empty, or nothing but leaders and punctuation — a blank to be filled in.</summary>
    public static bool IsBlank(string text) => IsNoise(StripLeaders(text));

    /// <summary>
    /// Splits "Label: value" at the first colon. Returns false when there is no colon, or when
    /// the colon sits inside a time ("10:30") or a code rather than after a label.
    /// </summary>
    public static bool TrySplitLabel(string text, out string label, out string value)
    {
        label = value = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var index = text.IndexOf(':');
        if (index <= 0 || (index + 1 < text.Length && char.IsDigit(text[index + 1]) && char.IsDigit(text[index - 1])))
            return false;

        label = Normalize(text[..index]).TrimEnd('.', ' ');
        value = Normalize(text[(index + 1)..]);
        return label.Length > 0 && label.Length <= 120;
    }

    /// <summary>Lower-case letters and digits only, for dictionary comparisons.</summary>
    public static string Canonical(string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in (text ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(ch);
        }

        return builder.ToString();
    }

    /// <summary>snake_case key from free text, at most <paramref name="maxLength"/> characters.</summary>
    public static string SnakeKey(string text, int maxLength = 60)
    {
        var words = Words(text);
        var key = string.Join("_", words).ToLowerInvariant();
        key = key.Length > maxLength ? key[..maxLength].TrimEnd('_') : key;
        return key.Length == 0 ? "field" : char.IsDigit(key[0]) ? "f_" + key : key;
    }

    /// <summary>camelCase key from free text ("New Batch" → newBatch, "Plate 1" → plate1).</summary>
    public static string CamelKey(string text, int maxLength = 40)
    {
        var words = Words(text);
        if (words.Count == 0)
            return "column";

        var builder = new StringBuilder(words[0].ToLowerInvariant());
        foreach (var word in words.Skip(1))
            builder.Append(char.ToUpperInvariant(word[0])).Append(word[1..].ToLowerInvariant());

        var key = builder.ToString();
        key = key.Length > maxLength ? key[..maxLength] : key;
        return char.IsDigit(key[0]) ? "c" + key : key;
    }

    private static List<string> Words(string text)
    {
        var decomposed = (text ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            builder.Append(char.IsLetterOrDigit(ch) && ch < 128 ? ch : ' ');
        }

        return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
