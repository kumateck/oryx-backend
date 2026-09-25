using System.Globalization;
using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One printed limit tier: the points it covers and their limit.</summary>
public sealed record WaterTier(string PrintedPoints, IReadOnlyList<string> PointKeys, string Criteria, IReadOnlyList<string> Duplicates);

/// <summary>
/// Water sheets print a limit per group of sampling points:
/// "SP1- SP3 – NMT 500cfu/mL", "SP14, SP15, NSP1, NSP2, NSP6, NSP6 … – NMT 80 cfu/mL",
/// "SP10- SP15, NSP1-NSP15– NMT 80 cfu/mL". Codes are compared by <see cref="PointKey"/>, so
/// "SP 1" in the point list matches "SP1" in a tier.
/// </summary>
public static partial class WaterSpecificationTiers
{
    [GeneratedRegex(@"\b(NMT|NLT|not\s+more\s+than|not\s+less\s+than)\b", RegexOptions.IgnoreCase)]
    private static partial Regex LimitStartRegex();

    [GeneratedRegex(@"^(?<prefix>[A-Za-z]+)\s*(?<from>\d+)\s*(?:[-–—]\s*(?:(?<prefix2>[A-Za-z]+)\s*)?(?<to>\d+))?$")]
    private static partial Regex ItemRegex();

    /// <summary>"SP 1" → "sp1".</summary>
    public static string PointKey(string code) => ImportText.Canonical(code);

    /// <summary>
    /// Reads "&lt;points&gt; – &lt;limit&gt;". False when the text is not a tier: no limit, or
    /// anything before the limit that is not a list of point codes and ranges.
    /// </summary>
    public static bool TryParse(string text, out WaterTier tier)
    {
        tier = null;
        var limit = LimitStartRegex().Match(text ?? string.Empty);
        if (!limit.Success || limit.Index == 0)
            return false;

        var printedPoints = text[..limit.Index].Trim().TrimEnd('-', '–', '—', ':', ' ');
        var keys = new List<string>();
        var duplicates = new List<string>();

        foreach (var item in printedPoints.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = ItemRegex().Match(item);
            if (!match.Success)
                return false;

            var prefix = match.Groups["prefix"].Value;
            var second = match.Groups["prefix2"].Value;
            if (second.Length > 0 && !string.Equals(prefix, second, StringComparison.OrdinalIgnoreCase))
                return false;

            var from = int.Parse(match.Groups["from"].Value, CultureInfo.InvariantCulture);
            var to = match.Groups["to"].Success ? int.Parse(match.Groups["to"].Value, CultureInfo.InvariantCulture) : from;
            if (to < from || to - from > 200)
                return false;

            for (var number = from; number <= to; number++)
            {
                var key = PointKey($"{prefix}{number}");
                if (keys.Contains(key))
                    duplicates.Add($"{prefix}{number}");
                else
                    keys.Add(key);
            }
        }

        if (keys.Count == 0)
            return false;

        tier = new WaterTier(printedPoints, keys, ImportText.Normalize(text[limit.Index..]), duplicates);
        return true;
    }
}
