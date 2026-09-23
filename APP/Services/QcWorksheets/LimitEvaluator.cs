using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DOMAIN.Entities.QcWorksheets;

namespace APP.Services.QcWorksheets;

/// <summary>
/// How a submitted value sat against its Characteristic's limits.
/// </summary>
public enum LimitOutcome
{
    /// <summary>Within every limit that applies.</summary>
    Compliant = 0,

    /// <summary>
    /// Breached the Alert limit but not the Action limit. Flags for trend review and
    /// deliberately does <b>not</b> open an OosCase or block release
    /// (lifecycle-and-governance.md, "Alert vs. Action limits").
    /// </summary>
    Alert = 1,

    /// <summary>Breached the Action limit (or the acceptance criteria when no separate Action limit exists). Opens an OosCase and blocks release.</summary>
    ActionOos = 2,

    /// <summary>
    /// The limit text could not be parsed, or the submitted value could not be judged against
    /// it. Fails safe: flagged for a human rather than silently passing.
    /// </summary>
    ManualReview = 3
}

/// <summary>
/// One field's evaluation.
/// </summary>
/// <param name="Outcome">What the evaluation concluded.</param>
/// <param name="LimitText">The limit text the outcome was decided against, for display on the case.</param>
/// <param name="Reason">Why, in words — the case's "value vs. limit" line, or the data-quality complaint when <see cref="LimitOutcome.ManualReview"/>.</param>
public readonly record struct LimitEvaluation(LimitOutcome Outcome, string LimitText, string Reason)
{
    internal static LimitEvaluation Compliant() => new(LimitOutcome.Compliant, null, null);
}

/// <summary>
/// Parses the limit grammar the real Specification documents actually use, and judges a
/// submitted value against it.
/// <para>
/// The grammar is deliberately closed to the four forms below — every form observed across the
/// real ARD/COA/Specification documents reviewed, and nothing invented beyond them. A limit
/// this cannot parse is <b>never</b> treated as a pass: it comes back as
/// <see cref="LimitOutcome.ManualReview"/> so the ambiguity surfaces as a data-quality signal
/// against whoever authored that Specification.
/// </para>
/// <list type="bullet">
///   <item><description><c>NMT &lt;number&gt; &lt;unit&gt;</c> — Not More Than; value must be &lt;= number.</description></item>
///   <item><description><c>NLT &lt;number&gt; &lt;unit&gt;</c> — Not Less Than; value must be &gt;= number.</description></item>
///   <item><description><c>&lt;number&gt;-&lt;number&gt; &lt;unit&gt;</c> or <c>&lt;number&gt; to &lt;number&gt;</c> — inclusive range.</description></item>
///   <item><description>Anything else — an exact qualitative match, case-insensitive, after the normalization defined in <see cref="NormalizeQualitative"/>.</description></item>
/// </list>
/// <para>
/// Stateless and dependency-free on purpose: limit interpretation is the single most
/// consequential piece of arithmetic in the module, so it is a pure function that can be
/// exhaustively tested without a database.
/// </para>
/// </summary>
public static class LimitEvaluator
{
    /// <summary>
    /// Not More Than. Accepts the abbreviation and the spelled-out form, plus the symbols the
    /// documents occasionally use in place of either.
    /// </summary>
    private static readonly Regex NotMoreThan = new(
        @"^\s*(?:nmt|not\s+more\s+than|max(?:imum)?|<=|≤|<)\s*(?<number>[-+]?\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex NotLessThan = new(
        @"^\s*(?:nlt|not\s+less\s+than|min(?:imum)?|>=|≥|>)\s*(?<number>[-+]?\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// A range. The separator is a hyphen/dash or the word "to"; a leading sign on the low
    /// bound is allowed, which is why the high bound's separator is matched explicitly rather
    /// than by splitting on "-".
    /// </summary>
    private static readonly Regex Range = new(
        @"^\s*(?<low>[-+]?\d+(?:\.\d+)?)\s*(?:-|–|—|to)\s*(?<high>[-+]?\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// A limit that opened with a comparison keyword but whose number did not parse — the one
    /// case that is genuinely ambiguous rather than merely qualitative, and therefore the one
    /// that must not fall through to a string match.
    /// </summary>
    private static readonly Regex ComparisonKeyword = new(
        @"^\s*(?:nmt|nlt|not\s+more\s+than|not\s+less\s+than|max(?:imum)?|min(?:imum)?|<=|>=|≤|≥|<|>)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>A number anywhere in the submitted value, so "98.5 %" and "98.5%w/w" both read as 98.5.</summary>
    private static readonly Regex LeadingNumber = new(
        @"[-+]?\d+(?:\.\d+)?",
        RegexOptions.Compiled);

    /// <summary>
    /// Judges a submitted value against one Characteristic.
    /// <para>
    /// The Action limit decides whether this is an OOS, and the Alert limit only matters when
    /// the Action limit was not breached — an Alert breach flags for trend review and blocks
    /// nothing. When a Characteristic carries no separate Action limit, its plain
    /// <see cref="SpecificationCharacteristic.AcceptanceCriteria"/> <i>is</i> the hard limit,
    /// which is how non-tiered chemical tests work.
    /// </para>
    /// <para>
    /// Precedence is ActionOos &gt; ManualReview &gt; Alert &gt; Compliant. ManualReview
    /// outranks Alert deliberately: an unjudgeable hard limit is a bigger problem than a known
    /// soft breach, and must not be reported as the lesser of the two.
    /// </para>
    /// </summary>
    public static LimitEvaluation Evaluate(SpecificationCharacteristic characteristic, string submittedValue)
    {
        ArgumentNullException.ThrowIfNull(characteristic);

        // The hard limit: an explicit Action limit if the Characteristic has one, otherwise the
        // acceptance criteria itself.
        var hardLimit = FirstNonBlank(characteristic.ActionLimit, characteristic.AcceptanceCriteria);

        var action = Judge(hardLimit, submittedValue);
        if (action.Outcome == JudgementOutcome.Breached)
            return new LimitEvaluation(LimitOutcome.ActionOos, hardLimit, action.Reason);

        if (action.Outcome == JudgementOutcome.Indeterminate)
            return new LimitEvaluation(LimitOutcome.ManualReview, hardLimit, action.Reason);

        var alert = Judge(characteristic.AlertLimit, submittedValue);
        if (alert.Outcome == JudgementOutcome.Breached)
            return new LimitEvaluation(LimitOutcome.Alert, characteristic.AlertLimit, alert.Reason);

        // An unparseable Alert limit is still a data-quality problem worth surfacing, but it
        // cannot make the result an OOS — the hard limit already passed.
        if (alert.Outcome == JudgementOutcome.Indeterminate)
            return new LimitEvaluation(LimitOutcome.ManualReview, characteristic.AlertLimit, alert.Reason);

        return LimitEvaluation.Compliant();
    }

    /// <summary>The three things judging one limit text can conclude.</summary>
    private enum JudgementOutcome { Satisfied, Breached, Indeterminate }

    private readonly record struct Judgement(JudgementOutcome Outcome, string Reason)
    {
        internal static Judgement Satisfied() => new(JudgementOutcome.Satisfied, null);
        internal static Judgement Breached(string reason) => new(JudgementOutcome.Breached, reason);
        internal static Judgement Indeterminate(string reason) => new(JudgementOutcome.Indeterminate, reason);
    }

    /// <summary>
    /// Judges one value against one limit text. A blank limit is "no limit stated", which is
    /// satisfied rather than indeterminate — a Characteristic with no Alert limit is the norm,
    /// not a data-quality failure.
    /// </summary>
    private static Judgement Judge(string limitText, string submittedValue)
    {
        if (string.IsNullOrWhiteSpace(limitText))
            return Judgement.Satisfied();

        var limit = limitText.Trim();

        // A limit exists but nothing was entered against it. Never a silent pass.
        if (string.IsNullOrWhiteSpace(submittedValue))
            return Judgement.Indeterminate(
                $"No value was submitted to judge against the limit '{limit}'.");

        var value = submittedValue.Trim();

        var notMoreThan = NotMoreThan.Match(limit);
        if (notMoreThan.Success && TryNumber(notMoreThan.Groups["number"].Value, out var maximum))
            return JudgeNumeric(
                value, limit,
                number => number <= maximum,
                number => $"{number.ToString(CultureInfo.InvariantCulture)} exceeds the limit of "
                    + $"{maximum.ToString(CultureInfo.InvariantCulture)} ('{limit}').");

        var notLessThan = NotLessThan.Match(limit);
        if (notLessThan.Success && TryNumber(notLessThan.Groups["number"].Value, out var minimum))
            return JudgeNumeric(
                value, limit,
                number => number >= minimum,
                number => $"{number.ToString(CultureInfo.InvariantCulture)} falls below the limit of "
                    + $"{minimum.ToString(CultureInfo.InvariantCulture)} ('{limit}').");

        var range = Range.Match(limit);
        if (range.Success
            && TryNumber(range.Groups["low"].Value, out var low)
            && TryNumber(range.Groups["high"].Value, out var high))
        {
            // A reversed range is an authoring error, not something to quietly reinterpret.
            if (low > high)
                return Judgement.Indeterminate(
                    $"The limit '{limit}' states a range whose lower bound is above its upper bound.");

            return JudgeNumeric(
                value, limit,
                number => number >= low && number <= high,
                number => $"{number.ToString(CultureInfo.InvariantCulture)} falls outside the range "
                    + $"{low.ToString(CultureInfo.InvariantCulture)}-"
                    + $"{high.ToString(CultureInfo.InvariantCulture)} ('{limit}').");
        }

        // Opened with a comparison keyword but carried no usable number. This is the ambiguous
        // case the brief calls out: it must not fall through to a string comparison, because a
        // malformed "NMT" limit would then "pass" for any value that is not literally "NMT".
        if (ComparisonKeyword.IsMatch(limit))
            return Judgement.Indeterminate(
                $"The limit '{limit}' reads as a numeric comparison but states no usable number. "
                + "The Specification characteristic needs correcting.");

        return JudgeQualitative(value, limit);
    }

    private static Judgement JudgeNumeric(
        string submittedValue, string limit, Func<decimal, bool> satisfies, Func<decimal, string> breachReason)
    {
        if (!TryExtractNumber(submittedValue, out var number))
            return Judgement.Indeterminate(
                $"'{submittedValue}' is not a number, and the limit '{limit}' is a numeric "
                + "comparison. The result cannot be judged automatically.");

        return satisfies(number) ? Judgement.Satisfied() : Judgement.Breached(breachReason(number));
    }

    /// <summary>
    /// The exact qualitative match. Both sides go through
    /// <see cref="NormalizeQualitative"/> and must come out identical.
    /// </summary>
    private static Judgement JudgeQualitative(string submittedValue, string limit)
    {
        var expected = NormalizeQualitative(limit);
        var actual = NormalizeQualitative(submittedValue);

        if (expected.Length == 0)
            return Judgement.Indeterminate($"The limit '{limit}' normalizes to nothing comparable.");

        return expected == actual
            ? Judgement.Satisfied()
            : Judgement.Breached(
                $"'{submittedValue}' does not match the required '{limit}'.");
    }

    /// <summary>
    /// The qualitative normalization rule, stated once and applied to both sides so the
    /// comparison is symmetric.
    /// <list type="number">
    ///   <item><description>Lowercase, and collapse every run of non-alphanumeric characters to one space.</description></item>
    ///   <item><description>Drop a trailing context clause introduced by "in", "per" or "from" — this is what lets the limit "Absence of E. coli in 1g" be satisfied by the entered result "Absent".</description></item>
    ///   <item><description>Reduce the absence/presence family to a single kernel: anything of the form "absence of X", "absence" or "absent" becomes <c>absent</c>, and the presence family becomes <c>present</c>.</description></item>
    ///   <item><description>Reduce the conformance family — "complies", "complies with", "conforms", "conform", "passes", "pass", "satisfactory" — to <c>complies</c>, since the real worksheets use these interchangeably for the same finding.</description></item>
    /// </list>
    /// <para>
    /// Nothing here guesses at a partial match: two strings that reduce to different kernels are
    /// a breach, not a maybe. Fuzzy matching on a pharmaceutical acceptance criterion would be
    /// worse than useless.
    /// </para>
    /// </summary>
    internal static string NormalizeQualitative(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var character in text.ToLowerInvariant())
            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');

        var normalized = string.Join(' ', builder.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        normalized = StripContextClause(normalized);

        if (normalized.StartsWith("absence of ", StringComparison.Ordinal)
            || normalized is "absence" or "absent")
            return "absent";

        if (normalized.StartsWith("presence of ", StringComparison.Ordinal)
            || normalized is "presence" or "present")
            return "present";

        if (normalized is "complies" or "conforms" or "conform" or "conforming"
            or "pass" or "passes" or "satisfactory")
            return "complies";

        // "Complies with the standard" / "Conforms to USP" state the same finding as a bare
        // "Complies": the trailing phrase names the reference being met, it does not narrow
        // the result. Matched as a prefix rather than listed exhaustively, since the reference
        // named varies per document.
        foreach (var prefix in (string[])["complies with", "complies to", "conforms to", "conforms with"])
        {
            if (normalized == prefix || normalized.StartsWith($"{prefix} ", StringComparison.Ordinal))
                return "complies";
        }

        return normalized;
    }

    /// <summary>
    /// Removes a trailing " in ... " / " per ... " / " from ... " clause. Only the <b>last</b>
    /// such clause is dropped, so "absence of e coli in 1g" loses "in 1g" and keeps the organism.
    /// </summary>
    private static string StripContextClause(string normalized)
    {
        foreach (var marker in (string[])[" in ", " per ", " from "])
        {
            var index = normalized.LastIndexOf(marker, StringComparison.Ordinal);
            if (index > 0)
                normalized = normalized[..index];
        }

        return normalized.Trim();
    }

    private static bool TryNumber(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// Pulls the number out of an entered result. The unit is entered alongside the figure on
    /// real worksheets ("98.5 %", "&lt;1 CFU"), so the first number in the string is the reading.
    /// </summary>
    private static bool TryExtractNumber(string submittedValue, out decimal value)
    {
        value = 0;
        var match = LeadingNumber.Match(submittedValue);
        return match.Success && TryNumber(match.Value, out value);
    }

    private static string FirstNonBlank(params string[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));
}
