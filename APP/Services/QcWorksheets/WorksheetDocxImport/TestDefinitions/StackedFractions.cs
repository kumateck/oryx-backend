using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport.TestDefinitions;

/// <summary>A formula as the sheet prints it, rejoined from its stacked lines.</summary>
public sealed record PrintedFraction(string Label, string Numerator, string Denominator, string Trailing)
{
    /// <summary>One line: "% Assay = [(Sample Titre – Blank Titre) x Factor] / [Weight of sample] x 100".</summary>
    public string Text
    {
        get
        {
            var body = Denominator is null ? Numerator : $"[{Numerator}] / [{Denominator}]";
            var text = string.IsNullOrWhiteSpace(Label) ? body : $"{Label} = {body}";
            return string.IsNullOrWhiteSpace(Trailing) ? text : $"{text} {Trailing}";
        }
    }
}

public sealed class StackedFractionResult
{
    /// <summary>The worded formulas, in sheet order. The blank worked sums under them are not listed.</summary>
    public List<PrintedFraction> Fractions { get; } = [];

    /// <summary>Indexes of every line that belongs to a printed calculation: worded, rule or blank worked sum.</summary>
    public HashSet<int> Consumed { get; } = [];
}

/// <summary>
/// The raw-material sheets print a formula as a fraction stacked over three lines (brief 12):
/// <code>
///            (Sample Titre – Blank Titre) x Factor x Equiv. x 100
/// % Assay = ------------------------------------------------------ x 100
///                 Weight of sample x (100 – LOD)
/// </code>
/// followed by the same shape with blanks ("( – ) x x x") for the analyst's own figures. This joins
/// numerator, rule and denominator back into one formula. The HPLC and UV sheets draw the rule as
/// a border instead, so there a line after "Calculations:" is the numerator and the next one the
/// denominator.
/// </summary>
public static partial class StackedFractions
{
    [GeneratedRegex(@"-{5,}|-{2,}\s*[xX]\s*-{2,}")]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^\s*(?:Content\s+)?Calculations?\s*:+\s*", RegexOptions.IgnoreCase)]
    private static partial Regex CalculationLabelRegex();

    [GeneratedRegex(@"^\s*(?<label>%?\s*(?:Assay|Content)[^=:]*?)\s*=\s*(?<expr>.*[A-Za-z]{3}.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex InlineRegex();

    // Any letter that is not the "x" of a blank product: "(n2 – n1)", "α x 100", "Titre x 5.61".
    [GeneratedRegex(@"[\p{L}-[xX]]")]
    private static partial Regex WordRegex();

    public static bool IsRule(string line) => RuleRegex().IsMatch(line ?? string.Empty);

    /// <summary>True when the line carries words, not just the "( – ) x x x" of a blank worked sum.</summary>
    public static bool IsWorded(string line) => WordRegex().IsMatch(ImportText.StripLeaders(line ?? string.Empty));

    public static StackedFractionResult Read(IReadOnlyList<string> lines)
    {
        var result = new StackedFractionResult();

        for (var index = 0; index < lines.Count; index++)
        {
            if (!IsRule(lines[index]))
                continue;

            result.Consumed.Add(index);
            var rules = RuleRegex().Matches(lines[index]);
            var label = lines[index][..rules[0].Index].Trim().TrimEnd('=', ':', ' ');
            var trailing = lines[index][(rules[^1].Index + rules[^1].Length)..].Trim().TrimEnd('=', ' ');

            var above = index > 0 && !result.Consumed.Contains(index - 1) && IsOperand(lines[index - 1]) ? index - 1 : -1;
            if (above < 0)
                continue;

            result.Consumed.Add(above);
            var below = index + 1 < lines.Count && IsOperand(lines[index + 1])
                        && IsWorded(lines[above]) == IsWorded(lines[index + 1]) ? index + 1 : -1;

            // A blank worked sum: its denominator is consumed only when another rule does not claim it as a numerator.
            if (!IsWorded(lines[above]))
            {
                if (below >= 0 && !(below + 1 < lines.Count && IsRule(lines[below + 1])))
                    result.Consumed.Add(below);
                continue;
            }

            if (below >= 0)
                result.Consumed.Add(below);
            result.Fractions.Add(new PrintedFraction(label, Clean(lines[above]), below < 0 ? null : Clean(lines[below]), trailing));
        }

        for (var index = 0; index < lines.Count; index++)
        {
            if (result.Consumed.Contains(index))
                continue;

            // "Calculations:" then ": spl peak x conc std x % Purity" over "std peak conc spl (100 – Water)".
            var calculation = CalculationLabelRegex().Match(lines[index]);
            var expression = calculation.Success ? lines[index][calculation.Length..] : null;
            var start = index;
            if (calculation.Success && ImportText.IsBlank(expression) && index + 1 < lines.Count
                && !result.Consumed.Contains(index + 1) && IsOperand(lines[index + 1]) && IsWorded(lines[index + 1]))
            {
                expression = lines[index + 1];
                start = index + 1;
            }

            string inlineLabel = null;
            if (expression is null && InlineRegex().Match(lines[index]) is { Success: true } inline)
            {
                inlineLabel = inline.Groups["label"].Value.Trim();
                expression = inline.Groups["expr"].Value;
            }
            else if (expression is null && lines[index].TrimStart().StartsWith(':') && IsWorded(lines[index]))
                expression = lines[index];

            if (expression is null || !IsWorded(expression))
                continue;

            for (var consumed = index; consumed <= start; consumed++)
                result.Consumed.Add(consumed);

            var next = start + 1;
            var hasDenominator = next < lines.Count && !result.Consumed.Contains(next) && IsOperand(lines[next]) && IsWorded(lines[next]);
            if (hasDenominator)
                result.Consumed.Add(next);
            result.Fractions.Add(new PrintedFraction(inlineLabel, Clean(expression), hasDenominator ? Clean(lines[next]) : null, null));
        }

        return result;
    }

    /// <summary>A numerator or denominator: not a rule, a label ("Average %Assay =", "Preparation:") or a blank to fill.</summary>
    private static bool IsOperand(string line)
    {
        if (IsRule(line) || ImportText.IsBlank(line))
            return false;
        var text = line.Trim();
        if (CalculationLabelRegex().IsMatch(text) && ImportText.IsBlank(CalculationLabelRegex().Replace(text, string.Empty)))
            return false;
        return !IsWorded(text) || (!text.Contains('=') && !text.EndsWith(':') && !ImportText.HasLeader(text));
    }

    /// <summary>The line without the ":" that leads a numerator or the stray " ." that ends one.</summary>
    private static string Clean(string line) => Regex.Replace(ImportText.Normalize(line).TrimStart(':', ' '), @"\s+\.$", string.Empty).Trim();
}
