using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>
/// Reads a printed formula term by term against a definition's <see cref="DefinitionTerm"/>s, so the
/// formula emitted is the arrangement the sheet prints. Anything the definition does not know makes
/// the whole formula unreadable: nothing is guessed around an unknown term.
/// </summary>
public static class PrintedTerms
{
    private const string Separators = " \t:xX×*.,=()[]";

    /// <summary>
    /// The formula template for a printed fraction, or null when a term is unknown.
    /// <paramref name="variants"/> lists the alternatives the printed terms select.
    /// </summary>
    public static string Assemble(PrintedFraction fraction, IReadOnlyList<DefinitionTerm> terms, out List<string> variants)
    {
        variants = [];
        var numerator = Scan(fraction.Numerator, terms, variants);
        var denominator = fraction.Denominator is null ? [] : Scan(fraction.Denominator, terms, variants);
        var trailing = string.IsNullOrWhiteSpace(fraction.Trailing) ? [] : Scan(fraction.Trailing, terms, variants);

        if (numerator is null || denominator is null || trailing is null || numerator.Count == 0
            || !numerator.Concat(denominator).Any(symbol => symbol.Contains('{')))
            return null;

        var formula = string.Join(" * ", numerator);
        if (denominator.Count > 0)
            formula = $"{formula} / ({string.Join(" * ", denominator)})";
        return trailing.Count == 0 ? formula : $"{formula} * {string.Join(" * ", trailing)}";
    }

    /// <summary>The template fragments of a product of terms, or null when some text is not a known term.</summary>
    public static List<string> Scan(string text, IReadOnlyList<DefinitionTerm> terms, List<string> variants)
    {
        var symbols = new List<string>();
        var position = 0;
        text ??= string.Empty;

        while (position < text.Length)
        {
            Match match = null;
            DefinitionTerm matched = null;
            foreach (var term in terms)
            {
                var candidate = new Regex(@"\G(?:" + term.Pattern + ")", RegexOptions.IgnoreCase).Match(text, position);
                if (!candidate.Success || candidate.Length == 0 || !EndsOnBoundary(text, candidate))
                    continue;
                match = candidate;
                matched = term;
                break;
            }

            if (match is null)
            {
                if (!Separators.Contains(text[position]))
                    return null;
                position++;
                continue;
            }

            symbols.Add(matched.Symbol.Replace("$0", match.Value.Trim()));
            if (matched.Variant is not null && !variants.Contains(matched.Variant))
                variants.Add(matched.Variant);
            position = match.Index + match.Length;
        }

        return symbols;
    }

    /// <summary>A term may not stop in the middle of a word or number ("Factor" inside "Factory", "1" inside "100").</summary>
    private static bool EndsOnBoundary(string text, Match match)
    {
        var end = match.Index + match.Length;
        if (end >= text.Length)
            return true;
        var last = text[end - 1];
        var next = text[end];
        return !(char.IsLetterOrDigit(last) && char.IsLetterOrDigit(next)) && !(char.IsDigit(last) && next == '.' && end + 1 < text.Length && char.IsDigit(text[end + 1]));
    }
}
