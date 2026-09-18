using APP.Services.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// The limit grammar, exercised as a pure function. Limit interpretation is the single most
/// consequential piece of arithmetic in the module — it decides whether a batch is quarantined
/// — so it is tested exhaustively here without a database, rather than only through the
/// workflow that consumes it.
/// </summary>
public class LimitEvaluatorTests
{
    private static SpecificationCharacteristic Characteristic(
        string acceptanceCriteria = null, string alertLimit = null, string actionLimit = null) => new()
    {
        Id = Guid.NewGuid(),
        TestName = "Test",
        AcceptanceCriteria = acceptanceCriteria,
        AlertLimit = alertLimit,
        ActionLimit = actionLimit
    };

    private static LimitOutcome Outcome(
        string value, string acceptanceCriteria = null, string alertLimit = null, string actionLimit = null) =>
        LimitEvaluator
            .Evaluate(Characteristic(acceptanceCriteria, alertLimit, actionLimit), value)
            .Outcome;

    // -----------------------------------------------------------------------
    // NMT — Not More Than
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("50", LimitOutcome.Compliant)]
    [InlineData("100", LimitOutcome.Compliant)]     // inclusive: "not more than" allows equal
    [InlineData("100.0", LimitOutcome.Compliant)]
    [InlineData("100.1", LimitOutcome.ActionOos)]
    [InlineData("250", LimitOutcome.ActionOos)]
    public void Nmt_allows_up_to_and_including_the_limit(string value, LimitOutcome expected) =>
        Assert.Equal(expected, Outcome(value, actionLimit: "NMT 100 CFU/4Hrs"));

    /// <summary>The spelled-out and symbolic forms mean the same thing as the abbreviation.</summary>
    [Theory]
    [InlineData("NMT 10 ppm")]
    [InlineData("Not More Than 10 ppm")]
    [InlineData("not more than 10 ppm")]
    [InlineData("<= 10 ppm")]
    [InlineData("≤10 ppm")]
    [InlineData("Maximum 10 ppm")]
    public void Nmt_synonyms_are_all_recognised(string limit)
    {
        Assert.Equal(LimitOutcome.Compliant, Outcome("9.9", actionLimit: limit));
        Assert.Equal(LimitOutcome.ActionOos, Outcome("10.1", actionLimit: limit));
    }

    // -----------------------------------------------------------------------
    // NLT — Not Less Than
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("99", LimitOutcome.ActionOos)]
    [InlineData("100", LimitOutcome.Compliant)]     // inclusive
    [InlineData("101", LimitOutcome.Compliant)]
    public void Nlt_allows_from_the_limit_upward(string value, LimitOutcome expected) =>
        Assert.Equal(expected, Outcome(value, actionLimit: "NLT 100 mg"));

    [Theory]
    [InlineData("NLT 5 %")]
    [InlineData("Not Less Than 5 %")]
    [InlineData(">= 5 %")]
    [InlineData("≥5 %")]
    [InlineData("Minimum 5 %")]
    public void Nlt_synonyms_are_all_recognised(string limit)
    {
        Assert.Equal(LimitOutcome.Compliant, Outcome("5.1", actionLimit: limit));
        Assert.Equal(LimitOutcome.ActionOos, Outcome("4.9", actionLimit: limit));
    }

    // -----------------------------------------------------------------------
    // Range
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("94.9", LimitOutcome.ActionOos)]
    [InlineData("95.0", LimitOutcome.Compliant)]    // inclusive at both ends
    [InlineData("100.0", LimitOutcome.Compliant)]
    [InlineData("105.0", LimitOutcome.Compliant)]
    [InlineData("105.1", LimitOutcome.ActionOos)]
    public void Range_is_inclusive_at_both_ends(string value, LimitOutcome expected) =>
        Assert.Equal(expected, Outcome(value, actionLimit: "95.0-105.0%"));

    [Theory]
    [InlineData("95.0-105.0 %")]
    [InlineData("95.0 - 105.0 %")]
    [InlineData("95.0 to 105.0 %")]
    [InlineData("95.0–105.0 %")]
    public void Range_separators_are_interchangeable(string limit)
    {
        Assert.Equal(LimitOutcome.Compliant, Outcome("99", actionLimit: limit));
        Assert.Equal(LimitOutcome.ActionOos, Outcome("110", actionLimit: limit));
    }

    /// <summary>
    /// The unit travels with the entered figure on real worksheets, so it must not stop the
    /// reading being extracted.
    /// </summary>
    [Theory]
    [InlineData("98.5 %")]
    [InlineData("98.5%")]
    [InlineData("98.5 %w/w")]
    public void A_unit_on_the_entered_value_does_not_prevent_comparison(string value) =>
        Assert.Equal(LimitOutcome.Compliant, Outcome(value, actionLimit: "95.0-105.0%"));

    // -----------------------------------------------------------------------
    // Qualitative match
    // -----------------------------------------------------------------------

    /// <summary>
    /// The brief's worked example: the limit carries a context clause the entered result does
    /// not, and both must still reduce to the same kernel.
    /// </summary>
    [Theory]
    [InlineData("Absent")]
    [InlineData("absent")]
    [InlineData("Absence")]
    [InlineData("ABSENCE OF E. COLI IN 1G")]
    public void Absence_limits_are_satisfied_by_an_absent_result(string value) =>
        Assert.Equal(
            LimitOutcome.Compliant,
            Outcome(value, actionLimit: "Absence of E. coli in 1g"));

    [Theory]
    [InlineData("Present")]
    [InlineData("Detected")]
    [InlineData("Growth observed")]
    public void Absence_limits_are_breached_by_anything_else(string value) =>
        Assert.Equal(
            LimitOutcome.ActionOos,
            Outcome(value, actionLimit: "Absence of E. coli in 1g"));

    /// <summary>The conformance family is used interchangeably on the real worksheets.</summary>
    [Theory]
    [InlineData("Complies", "Complies")]
    [InlineData("Conforms", "Complies")]
    [InlineData("Passes", "Complies with the standard")]
    [InlineData("complies", "Conforms")]
    public void Conformance_wording_is_normalised(string value, string limit) =>
        Assert.Equal(LimitOutcome.Compliant, Outcome(value, actionLimit: limit));

    /// <summary>Punctuation and spacing differences are normalised away; meaning is not.</summary>
    [Theory]
    [InlineData("White  powder", "White powder", LimitOutcome.Compliant)]
    [InlineData("white-powder", "White powder", LimitOutcome.Compliant)]
    [InlineData("Yellow powder", "White powder", LimitOutcome.ActionOos)]
    public void Qualitative_matching_is_exact_after_normalisation(
        string value, string limit, LimitOutcome expected) =>
        Assert.Equal(expected, Outcome(value, actionLimit: limit));

    // -----------------------------------------------------------------------
    // Alert vs. Action — the locked governance distinction
    // -----------------------------------------------------------------------

    /// <summary>
    /// The real Environmental Monitoring pattern: general rooms at Alert 80 / Action 100. A
    /// value between the two flags for trend review and must <b>not</b> read as an OOS.
    /// </summary>
    [Theory]
    [InlineData("70", LimitOutcome.Compliant)]
    [InlineData("80", LimitOutcome.Compliant)]      // at the alert limit, not over it
    [InlineData("85", LimitOutcome.Alert)]
    [InlineData("100", LimitOutcome.Alert)]         // at the action limit, not over it
    [InlineData("101", LimitOutcome.ActionOos)]
    public void Alert_sits_between_compliant_and_action(string value, LimitOutcome expected) =>
        Assert.Equal(
            expected,
            Outcome(value, alertLimit: "NMT 80 CFU/4Hrs", actionLimit: "NMT 100 CFU/4Hrs"));

    /// <summary>The Dispensing Booth tier from the same document — a far tighter pair of limits.</summary>
    [Theory]
    [InlineData("2", LimitOutcome.Compliant)]
    [InlineData("4", LimitOutcome.Alert)]
    [InlineData("6", LimitOutcome.ActionOos)]
    public void Tighter_tiers_behave_identically(string value, LimitOutcome expected) =>
        Assert.Equal(
            expected,
            Outcome(value, alertLimit: "NMT 3 CFU/4Hrs", actionLimit: "NMT 5 CFU/4Hrs"));

    /// <summary>
    /// A non-tiered test has no separate Action limit, so its plain acceptance criteria is the
    /// hard limit — which is how most chemical tests work.
    /// </summary>
    [Fact]
    public void Acceptance_criteria_is_the_hard_limit_when_no_action_limit_exists()
    {
        Assert.Equal(LimitOutcome.Compliant, Outcome("99.0", acceptanceCriteria: "95.0-105.0%"));
        Assert.Equal(LimitOutcome.ActionOos, Outcome("92.0", acceptanceCriteria: "95.0-105.0%"));
    }

    /// <summary>An explicit Action limit takes precedence over the acceptance criteria text.</summary>
    [Fact]
    public void Action_limit_wins_over_acceptance_criteria_when_both_are_present() =>
        Assert.Equal(
            LimitOutcome.Compliant,
            Outcome("150", acceptanceCriteria: "NMT 100", actionLimit: "NMT 200"));

    // -----------------------------------------------------------------------
    // Fail-safe behaviour
    // -----------------------------------------------------------------------

    /// <summary>
    /// A limit that opens as a numeric comparison but states no usable number must never fall
    /// through to a string match — that would silently "pass" every value that is not literally
    /// the word "NMT".
    /// </summary>
    [Theory]
    [InlineData("NMT")]
    [InlineData("NLT ")]
    [InlineData("NMT TBD CFU")]
    public void An_unparseable_numeric_limit_is_referred_for_manual_review(string limit) =>
        Assert.Equal(LimitOutcome.ManualReview, Outcome("50", actionLimit: limit));

    /// <summary>A non-numeric result against a numeric limit cannot be judged, so it is not passed.</summary>
    [Theory]
    [InlineData("not done")]
    [InlineData("N/A")]
    [InlineData("see attached")]
    public void A_non_numeric_result_against_a_numeric_limit_is_referred(string value) =>
        Assert.Equal(LimitOutcome.ManualReview, Outcome(value, actionLimit: "NMT 100 CFU"));

    /// <summary>An empty result against a real limit is not a pass either.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_result_is_never_a_silent_pass(string value) =>
        Assert.Equal(LimitOutcome.ManualReview, Outcome(value, actionLimit: "NMT 100 CFU"));

    /// <summary>A reversed range is an authoring error, not something to quietly reinterpret.</summary>
    [Fact]
    public void A_reversed_range_is_referred_rather_than_reinterpreted() =>
        Assert.Equal(LimitOutcome.ManualReview, Outcome("100", actionLimit: "105.0-95.0 %"));

    /// <summary>
    /// A Characteristic stating no limit at all is compliant rather than referred: most
    /// Characteristics legitimately carry no Alert limit, and treating that as a data-quality
    /// failure would bury the real ones.
    /// </summary>
    [Fact]
    public void No_limit_stated_is_compliant_rather_than_referred() =>
        Assert.Equal(LimitOutcome.Compliant, Outcome("anything"));

    /// <summary>
    /// ManualReview outranks Alert: an unjudgeable hard limit is the bigger problem and must
    /// not be reported as the lesser of the two.
    /// </summary>
    [Fact]
    public void An_unjudgeable_action_limit_outranks_an_alert_breach() =>
        Assert.Equal(
            LimitOutcome.ManualReview,
            Outcome("90", alertLimit: "NMT 80 CFU", actionLimit: "NMT"));

    /// <summary>The evaluation carries the limit it decided against, which is what the case displays.</summary>
    [Fact]
    public void The_evaluation_reports_which_limit_it_judged_against()
    {
        var evaluation = LimitEvaluator.Evaluate(
            Characteristic(alertLimit: "NMT 80 CFU", actionLimit: "NMT 100 CFU"), "150");

        Assert.Equal(LimitOutcome.ActionOos, evaluation.Outcome);
        Assert.Equal("NMT 100 CFU", evaluation.LimitText);
        Assert.Contains("150", evaluation.Reason);
        Assert.Contains("100", evaluation.Reason);
    }
}
