using APP.Services.QcWorksheets;
using DOMAIN.Entities.QcWorksheets;
using Xunit;

namespace APP.Tests.QcWorksheets;

/// <summary>
/// Organism comparison in the absence/presence family: when both the value and the criterion
/// name an organism they must name the same one; when either names none, the keyword-only match
/// applies exactly as before.
/// </summary>
public class LimitEvaluatorOrganismTests
{
    private static LimitOutcome Outcome(string value, string criterion) =>
        LimitEvaluator.Evaluate(
            new SpecificationCharacteristic { Id = Guid.NewGuid(), TestName = "Test", AcceptanceCriteria = criterion },
            value).Outcome;

    [Theory]
    [InlineData("Absence of Salmonella", "Absence of E. coli")]
    [InlineData("Absence of Salmonella typhi", "Absence of Salmonella spp.")]
    [InlineData("Absence of Escherichia coli", "Absence of E. coli")]
    [InlineData("Presence of E. coli", "Absence of E. coli")]
    [InlineData("Presence of Salmonella", "Presence of E. coli")]
    public void A_different_organism_or_kernel_is_a_breach(string value, string criterion) =>
        Assert.Equal(LimitOutcome.ActionOos, Outcome(value, criterion));

    [Theory]
    [InlineData("Absence of E.coli", "Absence of E. coli in 1g of sample")]
    [InlineData("absence of E coli", "Absence of E. coli")]
    [InlineData("Absence of Salmonella spp.", "Absence of Salmonella")]
    [InlineData("Absence of Salmonella species", "Absence of Salmonella spp")]
    [InlineData("Absence of Salmonella sp.", "Absence of Salmonella spp. in 10g")]
    public void The_same_organism_written_differently_complies(string value, string criterion) =>
        Assert.Equal(LimitOutcome.Compliant, Outcome(value, criterion));

    /// <summary>Either side naming no organism keeps the keyword-only match exactly as before.</summary>
    [Theory]
    [InlineData("Absent", "Absence of E. coli", LimitOutcome.Compliant)]
    [InlineData("Absence of E. coli", "Absent", LimitOutcome.Compliant)]
    [InlineData("Absent", "Absent", LimitOutcome.Compliant)]
    [InlineData("Detected", "Absent", LimitOutcome.ActionOos)]
    [InlineData("Present", "Absence of E. coli", LimitOutcome.ActionOos)]
    public void Without_an_organism_on_both_sides_the_keyword_match_is_unchanged(
        string value, string criterion, LimitOutcome expected) =>
        Assert.Equal(expected, Outcome(value, criterion));

    [Theory]
    [InlineData("Absence of E. coli", "e coli")]
    [InlineData("Absence of E.coli in 1g", "e coli")]
    [InlineData("Presence of Salmonella spp.", "salmonella")]
    [InlineData("Absent", "")]
    [InlineData("Complies", "")]
    public void The_organism_is_read_after_absence_or_presence_of(string text, string organism) =>
        Assert.Equal(organism, LimitEvaluator.OrganismOf(text));
}
