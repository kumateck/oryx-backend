using System.Text.RegularExpressions;

namespace APP.Services.QcWorksheets.WorksheetDocxImport;

/// <summary>One "Label: ____ g" blank of a line, with the unit printed after its leader.</summary>
public sealed record LineBlank(string Label, string Unit);

/// <summary>
/// Line-level patterns of the raw-material worksheets, which stack "Label: ____" blanks in one
/// cell (brief 09). Pure text functions; <see cref="RawMaterialSectionBody"/> decides the fields.
/// </summary>
public static partial class RawMaterialLines
{
    // "Instrument ID:", "Balance ID.:", "Equipment ID" and an optional printed code after it.
    [GeneratedRegex(@"(?<label>(?:Instrument|Balance|Equipment)\s*ID)\.?\s*:?\s*(?<code>[A-Z]{2,}(?:/[A-Z0-9-]+)+)?", RegexOptions.IgnoreCase)]
    public static partial Regex InstrumentRegex();

    [GeneratedRegex(@"\((?:i{1,3}|iv)\)")]
    private static partial Regex ReplicateRegex();

    // A leader and/or a unit at the start of the text after a label: "__________ g Weight of sample".
    [GeneratedRegex(@"^(?<value>(?:_{2,}|…+|\.{3,}|\s)*(?:(?:g/ml|g/mL|mg|ml|mL|g|%|[˚°]\s*C)(?![A-Za-z]))?)\s*(?<next>.*)$")]
    private static partial Regex LeaderUnitRegex();

    [GeneratedRegex(@"^\d+\s*nm$", RegexOptions.IgnoreCase)]
    public static partial Regex WavelengthRegex();

    // Printed calculation skeletons and blank sums: "= ____ - ____ x 100% =", "-", "( – ) x x x".
    [GeneratedRegex(@"^[\s=\-–—xX×*%().\d/]*$")]
    private static partial Regex SkeletonRegex();

    // Method conditions printed with their value (Constants, never entries).
    private static readonly string[] ConditionLabels =
    [
        "detection", "detectionλ", "detector", "column", "columntemperature", "flowrate", "mobilephase",
        "mobilephasecomposition", "buffer", "diluent", "injectionvolume", "injectionsize", "mode", "wavelength"
    ];

    private static readonly string[] Captions =
        ["chromatographicconditions", "chromatographicsystem", "observedabsorbance", "systemsuitability"];

    public static bool IsCondition(string label) => ConditionLabels.Contains(ImportText.Canonical(label));

    public static bool IsCaption(string label) => Captions.Contains(ImportText.Canonical(label));

    public static bool IsSkeleton(string line) => SkeletonRegex().IsMatch(ImportText.StripLeaders(line));

    public static bool IsCalculationLabel(string line)
    {
        var canonical = ImportText.Canonical(line);
        return canonical is "calculation" or "calculations" or "contentcalculations"
               || (canonical.StartsWith("average") && canonical.Contains("assay"));
    }

    /// <summary>
    /// "Determination (i): (ii) mean:", "wt of Std taken: wt of Spl taken (i) (ii)": the labels
    /// before the replicate base, the base, its markers and the averaging label after them.
    /// </summary>
    public static bool TryReplicates(string line, out List<string> leading, out string baseLabel, out List<string> markers, out string average)
    {
        leading = [];
        baseLabel = average = null;
        markers = ReplicateRegex().Matches(line).Select(match => match.Value).ToList();
        if (markers.Count == 0)
            return false;

        var first = ReplicateRegex().Match(line);
        var parts = line[..first.Index].Split(':').Select(part => part.Trim()).ToList();
        leading = parts.Take(parts.Count - 1).Where(part => part.Length > 0).ToList();
        baseLabel = parts[^1].Length > 0 ? parts[^1] : leading.LastOrDefault();
        if (parts[^1].Length == 0 && leading.Count > 0)
            leading.RemoveAt(leading.Count - 1);

        var last = ReplicateRegex().Matches(line)[^1];
        var tail = line[(last.Index + last.Length)..].Trim(' ', ':');
        if (Regex.IsMatch(tail, @"^(mean|average)\b", RegexOptions.IgnoreCase))
            average = tail;
        return baseLabel is not null;
    }

    /// <summary>
    /// The blanks of a line: "Weight of KBr: ____ g Weight of sample: ____ g" → two blanks, unit g.
    /// Returns false when a label carries a printed value instead (a method condition or a
    /// constant), which <paramref name="filledLabel"/> / <paramref name="filledValue"/> then hold.
    /// </summary>
    public static bool TryBlanks(string line, out List<LineBlank> blanks, out string filledLabel, out string filledValue)
    {
        blanks = [];
        filledLabel = filledValue = null;
        var parts = Regex.Split(line, @"\s*[:=]\s*").ToList();
        if (parts.Count < 2)
            return false;

        var label = parts[0].Trim();
        for (var index = 1; index < parts.Count; index++)
        {
            var match = LeaderUnitRegex().Match(parts[index]);
            var value = ImportText.Normalize(match.Groups["value"].Value.Replace("_", " ").Replace("…", " ").Trim('.', ' '));
            var next = match.Groups["next"].Value.Trim();
            var printedLeader = match.Groups["value"].Value.Trim().Length > 0;

            // Text straight after a label, with no leader or unit, is that label's printed value —
            // unless it is itself a label ("Water: Ethanol (96%):", "230nm: 250nm:").
            var isValue = !printedLeader && next.Length > 0
                          && (index == parts.Count - 1 || char.IsDigit(next[0]) && !WavelengthRegex().IsMatch(next));
            if (isValue)
            {
                filledLabel = label;
                filledValue = ImportText.Normalize(string.Join(": ", parts.Skip(index)));
                return false;
            }

            if (label.Length > 0)
                blanks.Add(new LineBlank(label, NormalizeUnit(value)));
            label = next;
        }

        if (label.Length > 0)
            blanks.Add(new LineBlank(label, null));
        return true;
    }

    private static string NormalizeUnit(string unit) =>
        string.IsNullOrWhiteSpace(unit) ? null : unit.Replace("˚", "°").Replace(" ", string.Empty) switch
        {
            "ml" => "mL",
            "g/ml" => "g/mL",
            var other => other
        };
}
